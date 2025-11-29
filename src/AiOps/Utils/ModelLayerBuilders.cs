using System;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;

namespace AnnotationTool.Ai.Utils
{

	public class DoubleConvolutionBuilder
	{
		private readonly int inC;
		private readonly int outC;
		private readonly bool useInstanceNorm;
		private readonly bool useDropout;
		private readonly Device device;
		private readonly ScalarType trainPrecision;
		private readonly string namePrefix;
		private readonly bool useChannelAttention;

		public DoubleConvolutionBuilder(string namePrefix, int inC, int outC, bool useInstanceNorm, bool useDropout, bool useChannelAttention, Device device, ScalarType trainPrecision)
		{
			this.namePrefix = namePrefix;
			this.inC = inC;
			this.outC = outC;
			this.useInstanceNorm = useInstanceNorm;
			this.useDropout = useDropout;
			this.useChannelAttention = useChannelAttention;
			this.device = device;
			this.trainPrecision = trainPrecision;
		}

		public Sequential Build()
		{
			var layers = new List<(string, Module<Tensor, Tensor>)>();

			layers.Add((namePrefix + "_conv1", Conv2d(inC, outC, 3, 1, 1, device: device, dtype: trainPrecision)));

			if (useInstanceNorm)
			{
				layers.Add((namePrefix + "_norm1", InstanceNorm2d(outC, device: device, dtype: trainPrecision)));
			}
			else
			{
				layers.Add((namePrefix + "_norm1", BatchNorm2d(outC, device: device, dtype: trainPrecision)));
			}

			layers.Add((namePrefix + "_relu1", ReLU()));
			layers.Add((namePrefix + "_conv2", Conv2d(outC, outC, 3, 1, 1, device: device, dtype: trainPrecision)));

			if (useInstanceNorm)
			{
				layers.Add((namePrefix + "_norm2", InstanceNorm2d(outC, device: device, dtype: trainPrecision)));
			}
			else
			{
				layers.Add((namePrefix + "_norm2", BatchNorm2d(outC, device: device, dtype: trainPrecision)));
			}

			layers.Add((namePrefix + "_relu2", ReLU()));

			if (useDropout)
			{
				layers.Add((namePrefix + "_dropout", Dropout2d(0.2)));
			}

			if (useChannelAttention)
			{
				layers.Add((namePrefix + "_attention", new ChannelAttention(outC)));
			}

			return Sequential(layers);
		}
	}

	public class DownSamplingBuilder
	{
		public static Sequential Build(string namePrefix, int outC, bool usePooling, bool useStridedConv, bool useInterpolation, Device device, ScalarType trainPrecision)
		{
			if (usePooling)
				return Sequential((namePrefix + "_pool", MaxPool2d(2, 2)));
			if (useInterpolation)
				return Sequential((namePrefix + "_identity", Identity()));
			if (useStridedConv)
				return Sequential((namePrefix + "_stridedConv", Conv2d(outC, outC, 3, 2, 1, device: device, dtype: trainPrecision)));
			return Sequential((namePrefix + "_identity", Identity()));
		}
	}

	public class UpConvolutionBuilder
	{
		public static Sequential Build(string namePrefix, int inC, int outC, bool useInterpolation, Device device, ScalarType trainPrecision)
		{
			if (useInterpolation) return Sequential((namePrefix + "_identity", Identity()));
			return Sequential((namePrefix + "_convT", ConvTranspose2d(inC, outC, 2, 2, device: device, dtype: trainPrecision)));
		}
	}

	public class ChannelAttention : Module<Tensor, Tensor>
	{
		private readonly Module<Tensor, Tensor> fc1;
		private readonly Module<Tensor, Tensor> fc2;

		public ChannelAttention(int channels, int reduction = 16) : base(nameof(ChannelAttention))
		{
			fc1 = Linear(channels, channels / reduction);
			fc2 = Linear(channels / reduction, channels);
			RegisterComponents();
		}

		public override Tensor forward(Tensor x)
		{
			using (var _ = NewDisposeScope())
			{
				var avg = x.mean(new long[] { 2, 3 }, keepdim: false);
				var attn = fc2.call(relu(fc1.call(avg))).sigmoid();
				var attnReshaped = attn.unsqueeze(2).unsqueeze(3);
				return (x * attnReshaped).MoveToOuterDisposeScope();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				fc1.Dispose();
				fc2.Dispose();

				ClearModules();
			}
			base.Dispose(disposing);
		}
	}

	public class ResidualBlock : Module<Tensor, Tensor>
	{
		private readonly Module<Tensor, Tensor> block;

		public ResidualBlock(Module<Tensor, Tensor> block) : base(nameof(ResidualBlock))
		{
			this.block = block;
			RegisterComponents();
		}

		public override Tensor forward(Tensor input)
		{
			using (var _ = NewDisposeScope())
			{
				var outT = block.call(input);
				if (outT.shape[1] == input.shape[1])
					return (outT + input).MoveToOuterDisposeScope();
				else
					return outT.MoveToOuterDisposeScope();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) block.Dispose();
			base.Dispose(disposing);
		}
	}

	public class AttentionGate : Module<(Tensor, Tensor), Tensor>
	{
		private readonly Module<Tensor, Tensor> theta;
		private readonly Module<Tensor, Tensor> phi;
		private readonly Module<Tensor, Tensor> psi;
		private readonly Module<Tensor, Tensor> act;

		public AttentionGate(int inF, int gatingF, int interF, Device device, ScalarType dtype) : base(nameof(AttentionGate))
		{
			theta = Conv2d(inF, interF, 2, stride: 2, device: device, dtype: dtype);
			phi = Conv2d(gatingF, interF, 1, device: device, dtype: dtype);
			psi = Conv2d(interF, 1, 1, device: device, dtype: dtype);
			act = Sequential(ReLU(), Sigmoid());

			RegisterComponents();
		}

		public override Tensor forward((Tensor, Tensor) inputs)
		{
			using (var _ = NewDisposeScope())
			{
				var (x, g) = inputs;
				var thetaX = theta.call(x);
				var phiG = phi.call(g);

				var size = thetaX.shape;
				var combined = ReLU().call(thetaX + interpolate(phiG, new long[] { size[2], size[3] }, mode: InterpolationMode.Bilinear, align_corners: false));

				var attention = act.call(psi.call(combined));
				attention = interpolate(attention, new long[] { x.shape[2], x.shape[3] }, mode: InterpolationMode.Bilinear, align_corners: false);

				return (x * attention).MoveToOuterDisposeScope();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				theta.Dispose();
				phi.Dispose();
				psi.Dispose();
				act.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	public class SelfAttention : Module<Tensor, Tensor>
	{
		private readonly Module<Tensor, Tensor> queryConv;
		private readonly Module<Tensor, Tensor> keyConv;
		private readonly Module<Tensor, Tensor> valueConv;
		private readonly Module<Tensor, Tensor> softmax;
		private readonly int channels;

		public SelfAttention(int channels, Device device, ScalarType dtype) : base(nameof(SelfAttention))
		{
			this.channels = channels;
			queryConv = Conv2d(channels, channels / 8, 1, device: device, dtype: dtype);
			keyConv = Conv2d(channels, channels / 8, 1, device: device, dtype: dtype);
			valueConv = Conv2d(channels, channels, 1, device: device, dtype: dtype);
			softmax = Softmax(dim: -1);
			RegisterComponents();
		}

		public override Tensor forward(Tensor x)
		{
			using (var _ = NewDisposeScope())
			{
				var batch = x.shape[0];
				var projQuery = queryConv.call(x).view(batch, -1, x.shape[2] * x.shape[3]).transpose(1, 2);
				var projKey = keyConv.call(x).view(batch, -1, x.shape[2] * x.shape[3]);
				var projValue = valueConv.call(x).view(batch, -1, x.shape[2] * x.shape[3]);

				var attention = softmax.call(projQuery.matmul(projKey));
				var outT = projValue.matmul(attention.transpose(1, 2)).view(batch, channels, x.shape[2], x.shape[3]);
				return (outT + x).MoveToOuterDisposeScope(); // residual connection
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				queryConv.Dispose();
				keyConv.Dispose();
				valueConv.Dispose();
				softmax.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	public class MultiHeadSelfAttention : Module<Tensor, Tensor>
	{
		private readonly int heads;
		private readonly int channelsPerHead;
		private readonly bool useAdaptiveFusion;
		private readonly ModuleList<Module<Tensor, Tensor>> queries = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> keys = new ModuleList<Module<Tensor, Tensor>>();
		private readonly ModuleList<Module<Tensor, Tensor>> values = new ModuleList<Module<Tensor, Tensor>>();
		private readonly Module<Tensor, Tensor> outputProjection;
		private readonly Parameter fusionWeights;

		public MultiHeadSelfAttention(int channels, int numHeads, bool useAdaptiveFusion, Device device, ScalarType dtype) : base(nameof(MultiHeadSelfAttention))
		{
			this.heads = numHeads;
			this.useAdaptiveFusion = useAdaptiveFusion;
			this.channelsPerHead = channels / numHeads;

			if (channels % numHeads != 0)
				throw new ArgumentException($"channels ({channels}) must be divisible by numHeads ({numHeads})");

			for (var i = 0; i < heads; i++)
			{
				queries.Add(Conv2d(channels, channelsPerHead, 1, device: device, dtype: dtype));
				keys.Add(Conv2d(channels, channelsPerHead, 1, device: device, dtype: dtype));
				values.Add(Conv2d(channels, channelsPerHead, 1, device: device, dtype: dtype));
			}

			outputProjection = Conv2d(channels, channels, 1, device: device, dtype: dtype);

			if (useAdaptiveFusion)
			{
				fusionWeights = Parameter(ones(heads, dtype: dtype, device: device, requires_grad: true));
				register_parameter("fusionWeights", fusionWeights);
			}
			RegisterComponents();
		}

		public override Tensor forward(Tensor x)
		{
			using (var _ = NewDisposeScope())
			{
				var batch = x.shape[0];
				var height = x.shape[2];
				var width = x.shape[3];

				var headOutputs = new List<Tensor>();
				for (var i = 0; i < heads; i++)
				{
					var q = queries[i].call(x).view(batch, channelsPerHead, -1).transpose(1, 2); // [B, HW, C']
					var k = keys[i].call(x).view(batch, channelsPerHead, -1);                  // [B, C', HW]
					var v = values[i].call(x).view(batch, channelsPerHead, -1);                // [B, C', HW]

					var attn = softmax(q.matmul(k) / Math.Sqrt(channelsPerHead), dim: -1); // [B, HW, HW]
					var weighted = v.matmul(attn.transpose(1, 2)).view(batch, channelsPerHead, height, width); // [B, C', H, W]
					headOutputs.Add(weighted);
				}

				Tensor combined;

				if (useAdaptiveFusion)
				{
					var stacked = torch.stack(headOutputs); // [heads, B, C', H, W]
					var weights = nn.functional.softmax(fusionWeights, dim: 0).unsqueeze(1).unsqueeze(2).unsqueeze(3).unsqueeze(4);
					combined = (stacked * weights).sum(0); // [B, C', H, W]
					combined = combined.repeat(new long[] { 1, heads, 1, 1 }); // Expand to match [B, channels, H, W]
				}
				else
				{
					combined = cat(headOutputs, 1); // [B, channels, H, W]
				}

				var outT = outputProjection.call(combined);
				return (outT + x).MoveToOuterDisposeScope(); // residual connection
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var m in queries) m.Dispose();
				foreach (var m in keys) m.Dispose();
				foreach (var m in values) m.Dispose();
				outputProjection.Dispose();
			}
			base.Dispose(disposing);
		}
	}

}
