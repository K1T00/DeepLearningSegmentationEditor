using AiOps.AiUtils;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;


namespace AiOps.AiModels.UNet
{

	/// <summary>
	/// UNet is a convolutional neural network architecture for image segmentation.
	/// It consists of an encoder-decoder structure with skip connections.
	/// The encoder captures context and reduces spatial dimensions, while the decoder reconstructs the output with high-resolution features.
	/// The skip connections allow the model to retain spatial information lost during down sampling, improving segmentation accuracy.
	///
	/// Always use odd-sized and relatively small kernelSize  -> 3x3; 5x5, 7x7
	/// Input images tensor: [batch size x channels x width x height]
	/// Output masks tensor: [batch size x features x width x height]
	///
	/// Conv2d Output = (W-F+2P)/S+1
	/// W := input volume
	/// F := neuron size
	/// S := stride
	/// P := zero-padding
	/// K := Number of filters
	/// Total weights (with parameter sharing) = F * F * K
	/// </summary>
	public sealed class UNetModel : Module<Tensor, Tensor>
	{
		private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly Module<Tensor, Tensor> bottleneckLayer;
		private readonly ModuleList<Module<Tensor, Tensor>> upConvolutionLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> decoderLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly Module<Tensor, Tensor> outLayer;

		private readonly bool useInterpolationUp;
		private readonly bool useInterpolationDown;

		public UNetModel(IHyperParameter uNetPara) : base(nameof(UNetModel))
		{
			this.useInterpolationUp = uNetPara.UseInterpolationUp;
			this.useInterpolationDown = uNetPara.UseInterpolationDown;

			var filterSizes = new List<int>();
			var inChannels = uNetPara.TrainImagesAsGreyscale ? 1 : 3;
			var outChannels = uNetPara.Features;
			var device = new Device(uNetPara.TrainOnDevice);
			var trainPrecision = uNetPara.TrainPrecision;

			for (var i = 0; i <= uNetPara.Depth; i++)
			{
				filterSizes.Add(uNetPara.FirstFilterSize << i);
			}

			// Encoder
			var inC = inChannels;
			for (var nLayer = 0; nLayer < uNetPara.Depth; nLayer++)
			{
				encoderLayers.Add(
					new DoubleConvolutionBuilder(
						$"EncoderLayer{nLayer}", inC, filterSizes[nLayer], uNetPara.UseInstanceNorm, uNetPara.UseDropout, uNetPara.UseChannelAttention, device, trainPrecision).Build());

				downSamplingLayers.Add(
					DownSamplingBuilder
						.Build($"DownSamplingLayer{nLayer}", filterSizes[nLayer], uNetPara.UsePooling, uNetPara.UseStridedConv, useInterpolationDown, device, trainPrecision));

				inC = filterSizes[nLayer];
			}

			// Middle bottleneck layer
			bottleneckLayer = new DoubleConvolutionBuilder(
					"Bottleneck", inC, filterSizes[uNetPara.Depth], uNetPara.UseInstanceNorm, uNetPara.UseDropout, uNetPara.UseChannelAttention, device, trainPrecision).Build();
			inC = filterSizes[uNetPara.Depth];

			// Decoder
			for (var nLayer = uNetPara.Depth - 1; nLayer >= 0; nLayer--)
			{
				upConvolutionLayers.Add(
					UpConvolutionBuilder
						.Build($"UpConvolutionLayer{nLayer}", inC, filterSizes[nLayer], useInterpolationUp, device, trainPrecision));

				decoderLayers.Add(
					new DoubleConvolutionBuilder(
					$"DecoderLayer{nLayer}", 2 * filterSizes[nLayer], filterSizes[nLayer], uNetPara.UseInstanceNorm, uNetPara.UseDropout, uNetPara.UseChannelAttention, device, trainPrecision).Build());

				inC = filterSizes[nLayer];
			}

			// Output layer
			outLayer = Conv2d(filterSizes[0], uNetPara.Features, 1, device: device, dtype: trainPrecision);

			RegisterComponents();
			this.to(device, true);
		}

		public override Tensor forward(Tensor input)
		{
			using (var _ = NewDisposeScope())
			{
				var skips = new List<Tensor>();
				var x = input;

				for (var i = 0; i < encoderLayers.Count; i++)
				{
					x = encoderLayers[i].call(x);
					skips.Add(x);

					x = useInterpolationDown 
						? interpolate(x, scale_factor: new double[] { 0.5, 0.5 }, mode: InterpolationMode.Bilinear, align_corners: false) 
						: downSamplingLayers[i].call(x);
				}

				x = bottleneckLayer.call(x);

				for (var i = 0; i < decoderLayers.Count; i++)
				{
					var skip = skips[skips.Count - 1 - i];
					
					var upSampled = useInterpolationUp ?
						interpolate(x, scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false) :
						upConvolutionLayers[i].call(x);

					if (upSampled.shape[2] != skip.shape[2] || upSampled.shape[3] != skip.shape[3])
					{
						var interpolatedSkip = 
							interpolate(skip, new long[] { upSampled.shape[2], upSampled.shape[3] }, mode: InterpolationMode.Bilinear, align_corners: false);

						var concatenated = cat(new List<Tensor>() { upSampled, interpolatedSkip }, 1);
						x = decoderLayers[i].call(concatenated);
					}
					else
					{
						var concatenated = cat(new List<Tensor>() { upSampled, skip }, 1);
						x = decoderLayers[i].call(concatenated);
					}
				}
				return outLayer.call(x).MoveToOuterDisposeScope();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var l in encoderLayers) l.Dispose();
				foreach (var l in downSamplingLayers) l.Dispose();
				foreach (var l in upConvolutionLayers) l.Dispose();
				foreach (var l in decoderLayers) l.Dispose();

				bottleneckLayer.Dispose();
				outLayer.Dispose();

				ClearModules();
			}
			base.Dispose(disposing);
		}
	}

}
