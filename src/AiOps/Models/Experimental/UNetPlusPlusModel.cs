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
//	/// ToDo: !!!!!!!!!!!!!! WIP !!!!!!!!!!!!!!
//	/// </summary>
//	public sealed class UNetPlusPlusModel : Module<Tensor, Tensor>
//	{
//		private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
//		private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();

//		private readonly ModuleList<ModuleList<Module<Tensor, Tensor>>> decoderNestedLayers =
//			new ModuleList<ModuleList<Module<Tensor, Tensor>>>();
//		private readonly ModuleList<ModuleList<Module<Tensor, Tensor>>> upConvolutionLayers = new ModuleList<ModuleList<Module<Tensor, Tensor>>>();
//		private readonly Module<Tensor, Tensor> outLayer;

//		private readonly bool useInterpolationUp;
//		private readonly int depth;
//		private readonly int firstFilterSize;

//		public UNetPlusPlusModel(IProjectPresenter project, Device device) : base(nameof(UNetPlusPlusModel))
//		{
//			var para = project.Project.Settings.HyperParameters;


//			useInterpolationUp = para.UseInterpolationUp;

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
//            var precision = para.TrainPrecision;

//			for (var i = 0; i <= depth; i++)
//				filterSizes.Add(firstFilterSize << i);

//			// Build Encoder
//			var inC = inChannels;
//			for (var i = 0; i <= depth; i++)
//			{
//				encoderLayers.Add(new DoubleConvolutionBuilder(
//					$"EncoderLayer{i}", inC, filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build());

//				if (i < depth)
//				{
//					downSamplingLayers.Add(DownSamplingBuilder
//						.Build($"DownSamplingLayer{i}", filterSizes[i], para.UsePooling, para.UseStridedConv, para.UseInterpolationDown, device, precision));
//				}

//				inC = filterSizes[i];
//			}

//			// Initialize Nested Decoder structure
//			for (var i = 0; i < depth; i++)
//			{
//				decoderNestedLayers.Add(new ModuleList<Module<Tensor, Tensor>>());
//				upConvolutionLayers.Add(new ModuleList<Module<Tensor, Tensor>>());

//				for (var j = 0; j <= i; j++)
//				{
//					var inDecoderC = j == 0
//						? filterSizes[i + 1] + filterSizes[i]
//						: filterSizes[i + 1] + (j + 1) * filterSizes[i];

//					upConvolutionLayers[i].Add(UpConvolutionBuilder
//						.Build($"UpConvolutionLayer{i}_{j}", filterSizes[i + 1], filterSizes[i + 1], useInterpolationUp, device, precision));

//					decoderNestedLayers[i].Add(new DoubleConvolutionBuilder(
//						$"DecoderLayer{i}_{j}", inDecoderC, filterSizes[i], para.UseInstanceNorm, para.UseDropout, para.UseChannelAttention, device, precision).Build());
//				}
//			}

//			outLayer = Conv2d(filterSizes[0], outChannels, 1, device: device, dtype: precision);

//			RegisterComponents();
//			this.to(device, true);
//		}

//		public override Tensor forward(Tensor input)
//		{
//			using (var _ = NewDisposeScope())
//			{
//				var enc = new List<Tensor> { encoderLayers[0].call(input) };
//				for (var i = 1; i <= depth; i++)
//				{
//					var x = downSamplingLayers[i - 1].call(enc[i - 1]);
//					x = encoderLayers[i].call(x);
//					enc.Add(x);
//				}

//				// Nested decoder outputs
//				var decoderOutputs = new List<List<Tensor>>();
//				for (var i = 0; i < depth; i++)
//					decoderOutputs.Add(new List<Tensor>(new Tensor[i + 1]));

//				for (var j = 0; j < depth; j++) // Vertical depth in nested structure
//				{
//					for (var i = 0; i < depth - j; i++) // Horizontal depth in nested structure (resolution level)
//					{
//						Tensor upSampled;
//						if (j == 0)
//						{
//							upSampled = useInterpolationUp
//								? interpolate(enc[i + 1], scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
//								: upConvolutionLayers[i][j].call(enc[i + 1]);

//							var catT = cat(new List<Tensor> { upSampled, enc[i] }, 1);
//							decoderOutputs[i][j] = decoderNestedLayers[i][j].call(catT);
//						}
//						else // Nested decoder
//						{
//							upSampled = useInterpolationUp
//								? interpolate(decoderOutputs[i + 1][j - 1], scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
//								: upConvolutionLayers[i][j].call(decoderOutputs[i + 1][j - 1]);

//							var skipConcat = new List<Tensor> { upSampled };
//							for (var k = 0; k < j; k++)
//								skipConcat.Add(decoderOutputs[i][k]);

//							skipConcat.Add(enc[i]);
//							var catT = cat(skipConcat, 1);

//							decoderOutputs[i][j] = decoderNestedLayers[i][j].call(catT);
//						}
//					}
//				}
//				return outLayer.call(decoderOutputs[0][depth - 1]).MoveToOuterDisposeScope();
//			}
//		}

//		protected override void Dispose(bool disposing)
//		{
//			if (disposing)
//			{
//				foreach (var l in encoderLayers) l.Dispose();
//				foreach (var l in downSamplingLayers) l.Dispose();
//				foreach (var lst in upConvolutionLayers) foreach (var l in lst) l.Dispose();
//				foreach (var lst in decoderNestedLayers) foreach (var l in lst) l.Dispose();
//				outLayer.Dispose();
//				ClearModules();
//			}
//			base.Dispose(disposing);
//		}

//	}
//}
