using AiOps.AiUtils;
using System;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;

namespace AiOps.AiModels.UNet
{
	/// <summary>
	/// Channel-wise + spatial attention UNet model.
	/// </summary>
	public sealed class UNetAttentionModel : Module<Tensor, Tensor>
	{
		private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly Module<Tensor, Tensor> bottleneckLayer;
		private readonly Module<Tensor, Tensor> selfAttention;
		private readonly ModuleList<Module<Tensor, Tensor>> upConvolutionLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> decoderLayers = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<(Tensor, Tensor), Tensor>> attentionGates = new ModuleList<Module<(Tensor, Tensor), Tensor>>();
		private readonly Module<Tensor, Tensor> outLayer;

		private readonly bool useInterpolationUp;
		private readonly bool useInterpolationDown;

		public UNetAttentionModel(IHyperParameter para) : base(nameof(UNetAttentionModel))
		{
			this.useInterpolationUp = para.UseInterpolationUp;
			this.useInterpolationDown = para.UseInterpolationDown;

			var filterSizes = new List<int>();
			var inChannels = para.TrainImagesAsGreyscale ? 1 : 3;
			var outChannels = para.Features;
			var device = new Device(para.TrainOnDevice);
			var precision = para.TrainPrecision;

			if (para.FirstFilterSize % 2 != 0)
			{
				throw new ArgumentException("FirstFilterSize must be an even number.");
			}

			for (var i = 0; i <= para.Depth; i++)
				filterSizes.Add(para.FirstFilterSize << i);

			// Encoder
			var inC = inChannels;
			for (var i = 0; i < para.Depth; i++)
			{
				encoderLayers.Add(new DoubleConvolutionBuilder($"EncoderLayer{i}", inC, filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build());

				downSamplingLayers.Add(DownSamplingBuilder.Build($"DownSamplingLayer{i}", filterSizes[i], para.UsePooling, para.UseStridedConv, useInterpolationDown, device, precision));
				inC = filterSizes[i];
			}

			bottleneckLayer = new DoubleConvolutionBuilder($"Bottleneck", inC, filterSizes[para.Depth], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build();

			var rawHeads = 1 << (para.Depth - 1);
			var numHeads = Math.Min(Math.Max(rawHeads, 2), 8);
			var bottleneckChannels = filterSizes[para.Depth];

			if (bottleneckChannels % numHeads != 0)
				throw new ArgumentException($"Bottleneck channels ({bottleneckChannels}) must be divisible by number of attention heads ({numHeads}).");

			selfAttention = new MultiHeadSelfAttention(bottleneckChannels, numHeads, para.UseAdaptiveAttentionFusion, device: device, dtype: precision);
			inC = filterSizes[para.Depth];

			// Decoder
			for (var i = para.Depth - 1; i >= 0; i--)
			{
				upConvolutionLayers.Add(UpConvolutionBuilder.Build($"UpConvolutionLayer{i}", inC, filterSizes[i], useInterpolationUp, device, precision));

				decoderLayers.Add(new DoubleConvolutionBuilder($"DecoderLayer{i}", 2 * filterSizes[i], filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build());

				attentionGates.Add(new AttentionGate(filterSizes[i], filterSizes[i], filterSizes[i], device, precision));
				inC = filterSizes[i];
			}

			outLayer = Conv2d(filterSizes[0], outChannels, 1, device: device, dtype: precision);

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
				x = selfAttention.call(x);

				for (var i = 0; i < decoderLayers.Count; i++)
				{
					var skip = skips[skips.Count - 1 - i];

					var upSampled = useInterpolationUp
						? interpolate(x, scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
						: upConvolutionLayers[i].call(x);

					if (upSampled.shape[2] != skip.shape[2] || upSampled.shape[3] != skip.shape[3])
					{
						skip = interpolate(skip, new long[] { upSampled.shape[2], upSampled.shape[3] }, mode: InterpolationMode.Bilinear, align_corners: false);
					}

					var gatedSkip = attentionGates[i].call((skip, upSampled));
					var catT = cat(new List<Tensor> { upSampled, gatedSkip }, 1);
					x = decoderLayers[i].call(catT);
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
				foreach (var l in attentionGates) l.Dispose();
				bottleneckLayer.Dispose();
				outLayer.Dispose();
				ClearModules();
			}
			base.Dispose(disposing);
		}
	}
}
