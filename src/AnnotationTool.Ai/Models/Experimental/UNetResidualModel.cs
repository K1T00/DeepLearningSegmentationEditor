//using AnnotationTool.Ai.Utils;
//using AnnotationTool.Core.Models;
//using AnnotationTool.Core.Services;
//using System.Collections.Generic;
//using TorchSharp;
//using TorchSharp.Modules;
//using static TorchSharp.torch;
//using static TorchSharp.torch.nn;
//using static TorchSharp.torch.nn.functional;

//namespace AnnotationTool.Ai.Models.Experimental
//{
//	/// <summary>
//	/// Better on deeper models even on small data (with less over-fitting)
//	/// </summary>
//	public sealed class UNetResidualModel : Module<Tensor, Tensor>
//	{
//		private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
//		private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();
//		private readonly Module<Tensor, Tensor> bottleneckLayer;
//		private readonly ModuleList<Module<Tensor, Tensor>> upConvolutionLayers = new ModuleList<Module<Tensor, Tensor>>();
//		private readonly ModuleList<Module<Tensor, Tensor>> decoderLayers = new ModuleList<Module<Tensor, Tensor>>();
//		private readonly Module<Tensor, Tensor> outLayer;

//		private readonly bool useInterpolationUp;
//		private readonly bool useInterpolationDown;
//		private readonly int depth;
//		private readonly int firstFilterSize;

//		public UNetResidualModel(IProjectPresenter project, Device device) : base(nameof(UNetResidualModel))
//		{
//			var para = project.Project.Settings.HyperParameters;

//			useInterpolationUp = para.UseInterpolationUp;
//			useInterpolationDown = para.UseInterpolationDown;

//			// Depth to be tested
//			if (para.Depth == 0)
//			{
//				switch (project.Project.Settings.TrainModelSettings.ModelComplexity)
//				{
//					case ModelComplexity.Low:
//						depth = 3;
//						break;

//					case ModelComplexity.Medium:
//						depth = 3;
//						break;

//					case ModelComplexity.High:
//						depth = 3;
//						break;

//				}
//			}
//			else
//			{
//				depth = para.Depth;
//			}
//			// Filter sizes to be tested
//			if (para.FirstFilterSize == 0)
//			{
//				switch (project.Project.Settings.TrainModelSettings.ModelComplexity)
//				{
//					case ModelComplexity.Low:
//						firstFilterSize = 64;
//						break;

//					case ModelComplexity.Medium:
//						firstFilterSize = 64;
//						break;

//					case ModelComplexity.High:
//						firstFilterSize = 64;
//						break;

//				}
//			}
//			else
//			{
//				firstFilterSize = 64;
//			}

//			var filterSizes = new List<int>();
//            var inChannels = project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3;
//            var outChannels = project.Project.Features.Count;
//			var precision = para.TrainPrecision;

//			for (var i = 0; i <= depth; i++)
//				filterSizes.Add(firstFilterSize << i);

//			// Encoder
//			var inC = inChannels;
//			for (var i = 0; i < depth; i++)
//			{
//				var conv = new DoubleConvolutionBuilder(
//					$"EncoderLayer{i}", inC, filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build();

//				encoderLayers.Add(new ResidualBlock(conv));

//				downSamplingLayers.Add(DownSamplingBuilder.Build($"DownSamplingLayer{i}", filterSizes[i], para.UsePooling, para.UseStridedConv, useInterpolationDown, device, precision));
//				inC = filterSizes[i];
//			}

//			// Bottleneck
//			var bottleneckConv = new DoubleConvolutionBuilder(
//				$"Bottleneck", inC, filterSizes[depth], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build();
//			bottleneckLayer = new ResidualBlock(bottleneckConv);

//			inC = filterSizes[depth];

//			// Decoder
//			for (var i = depth - 1; i >= 0; i--)
//			{
//				upConvolutionLayers.Add(UpConvolutionBuilder.Build($"UpConvolutionLayer{i}", inC, filterSizes[i], useInterpolationUp, device, precision));

//				var decConv = new DoubleConvolutionBuilder(
//					$"DecoderLayer{i}", 2 * filterSizes[i], filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build();

//				decoderLayers.Add(new ResidualBlock(decConv));

//				inC = filterSizes[i];
//			}

//			outLayer = Conv2d(filterSizes[0], outChannels, 1, device: device, dtype: precision);

//			RegisterComponents();
//			this.to(device, true);
//		}

//		public override Tensor forward(Tensor input)
//		{
//			using (var _ = NewDisposeScope())
//			{
//				var skips = new List<Tensor>();
//				var x = input;

//				for (var i = 0; i < encoderLayers.Count; i++)
//				{
//					x = encoderLayers[i].call(x);
//					skips.Add(x);

//					x = useInterpolationDown
//						? interpolate(x, scale_factor: new double[] { 0.5, 0.5 }, mode: InterpolationMode.Bilinear, align_corners: false)
//						: downSamplingLayers[i].call(x);
//				}

//				x = bottleneckLayer.call(x);

//				for (var i = 0; i < decoderLayers.Count; i++)
//				{
//					var skip = skips[skips.Count - 1 - i];

//					var upSampled = useInterpolationUp
//						? interpolate(x, scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
//						: upConvolutionLayers[i].call(x);

//					if (upSampled.shape[2] != skip.shape[2] || upSampled.shape[3] != skip.shape[3])
//					{
//						skip = interpolate(skip, new long[] { upSampled.shape[2], upSampled.shape[3] }, mode: InterpolationMode.Bilinear, align_corners: false);
//					}
//					var catT = cat(new List<Tensor> { upSampled, skip }, 1);
//					x = decoderLayers[i].call(catT);
//				}
//				return outLayer.call(x).MoveToOuterDisposeScope();
//			}
//		}

//		protected override void Dispose(bool disposing)
//		{
//			if (disposing)
//			{
//				foreach (var l in encoderLayers) l.Dispose();
//				foreach (var l in downSamplingLayers) l.Dispose();
//				foreach (var l in upConvolutionLayers) l.Dispose();
//				foreach (var l in decoderLayers) l.Dispose();
//				bottleneckLayer.Dispose();
//				outLayer.Dispose();
//				ClearModules();
//			}
//			base.Dispose(disposing);
//		}
//	}
//}
