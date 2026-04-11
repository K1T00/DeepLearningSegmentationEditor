using AnnotationTool.Ai.Utils;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;

namespace AnnotationTool.Ai.Models.UNet
{
    /// <summary>
    /// Configurable U-Net++ family model. 
    /// Supports:
    /// - plain U-Net++
    /// - residual U-Net++
    /// - attention U-Net++
    /// - residual attention U-Net++
    ///
    /// Use case: probably better for more detail-focused segmentation tasks, where the additional complexity can help capture finer details.
    /// </summary>
    public sealed class UNetPlusPlusFamilyModel : Module<Tensor, Tensor>, ISegmentationModel
    {
        private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly ModuleList<ModuleList<Module<Tensor, Tensor>>> decoderNestedLayers = new ModuleList<ModuleList<Module<Tensor, Tensor>>>();
        private readonly ModuleList<ModuleList<Module<Tensor, Tensor>>> upConvolutionLayers = new ModuleList<ModuleList<Module<Tensor, Tensor>>>();
        private readonly ModuleList<Module<(Tensor, Tensor), Tensor>> attentionGates =  new ModuleList<Module<(Tensor, Tensor), Tensor>>();
        private readonly Module<Tensor, Tensor> selfAttention;
        private readonly Module<Tensor, Tensor> outLayer;
        private readonly SegmentationModelConfig cfg;

        public UNetPlusPlusFamilyModel(int inChannels, int numClasses, SegmentationModelConfig cfg, Device device) : base(nameof(UNetPlusPlusFamilyModel))
        {
            this.cfg = cfg;

            var outChannels = GetOutChannels(numClasses);
            var filterSizes = BuildFilterSizes();
            var inC = inChannels;

            // Encoder: X(i,0)
            for (var i = 0; i <= cfg.Depth; i++)
            {
                encoderLayers.Add(
                    BuildConvBlock(
                        string.Format("EncoderLayer{0}", i),
                        inC,
                        filterSizes[i],
                        device));

                if (i < cfg.Depth)
                {
                    downSamplingLayers.Add(
                        DownSamplingBuilder.Build(
                            string.Format("DownSamplingLayer{0}", i),
                            filterSizes[i],
                            cfg.UsePooling,
                            cfg.UseStridedConv,
                            cfg.UseInterpolationDown,
                            device,
                            cfg.TrainPrecision));
                }

                inC = filterSizes[i];
            }

            if (cfg.UseSelfAttention)
            {
                selfAttention = new SelfAttention(filterSizes[cfg.Depth], device, cfg.TrainPrecision);
            }

            // Nested decoder nodes: X(i,j), j >= 1
            for (var i = 0; i < cfg.Depth; i++)
            {
                decoderNestedLayers.Add(new ModuleList<Module<Tensor, Tensor>>());
                upConvolutionLayers.Add(new ModuleList<Module<Tensor, Tensor>>());

                if (cfg.UseAttentionGates)
                {
                    attentionGates.Add(
                        new AttentionGate(
                            filterSizes[i],
                            filterSizes[i],
                            filterSizes[i],
                            device,
                            cfg.TrainPrecision));
                }

                for (var j = 1; j <= cfg.Depth - i; j++)
                {
                    var decoderInChannels = (j + 1) * filterSizes[i];

                    upConvolutionLayers[i].Add(
                        UpConvolutionBuilder.Build(
                            string.Format("UpConvolutionLayer{0}_{1}", i, j),
                            filterSizes[i + 1],
                            filterSizes[i],
                            cfg.UseInterpolationUp,
                            device,
                            cfg.TrainPrecision));

                    decoderNestedLayers[i].Add(
                        BuildConvBlock(
                            string.Format("DecoderLayer{0}_{1}", i, j),
                            decoderInChannels,
                            filterSizes[i],
                            device));
                }
            }

            outLayer = Conv2d(filterSizes[0], outChannels, 1, device: device, dtype: cfg.TrainPrecision);

            RegisterComponents();
            this.to(device, true);
        }

        private Module<Tensor, Tensor> BuildConvBlock(string name, int inChannels, int outChannels, Device device)
        {
            var block = new DoubleConvolutionBuilder(
                name,
                inChannels,
                outChannels,
                cfg.UseInstanceNorm,
                cfg.UseDropout,
                cfg.UseChannelAttention,
                device,
                cfg.TrainPrecision).Build();

            if (cfg.UseResidualBlocks)
            {
                return new ResidualBlock(block);
            }

            return block;
        }

        private List<int> BuildFilterSizes()
        {
            var filterSizes = new List<int>();

            for (var i = 0; i <= cfg.Depth; i++)
            {
                filterSizes.Add(cfg.FirstFilter << i);
            }

            return filterSizes;
        }

        private static int GetOutChannels(int numClasses)
        {
            var numFeatures = numClasses == 1 ? 1 : numClasses + 1;
            return numFeatures == 1 ? 1 : numFeatures;
        }

        public override Tensor forward(Tensor input)
        {
            using (var _ = NewDisposeScope())
            {
                var enc = new List<Tensor>();
                var x = input;

                // Encoder path: build X(i,0)
                for (var i = 0; i <= cfg.Depth; i++)
                {
                    x = encoderLayers[i].call(x);

                    if (i == cfg.Depth && selfAttention != null)
                    {
                        x = selfAttention.call(x);
                    }

                    enc.Add(x);

                    if (i < cfg.Depth)
                    {
                        x = cfg.UseInterpolationDown
                            ? interpolate(x, scale_factor: new double[] { 0.5, 0.5 }, mode: InterpolationMode.Bilinear, align_corners: false)
                            : downSamplingLayers[i].call(x);
                    }
                }

                // xNodes[i][j] corresponds to X(i,j)
                var xNodes = new List<List<Tensor>>();
                for (var i = 0; i <= cfg.Depth; i++)
                {
                    var row = new List<Tensor>();

                    for (var j = 0; j <= cfg.Depth - i; j++)
                    {
                        row.Add(null);
                    }

                    row[0] = enc[i];
                    xNodes.Add(row);
                }

                // Nested decoder
                for (var j = 1; j <= cfg.Depth; j++)
                {
                    for (var i = 0; i <= cfg.Depth - j; i++)
                    {
                        var lowerNode = xNodes[i + 1][j - 1];

                        var upSampled = cfg.UseInterpolationUp
                            ? interpolate(lowerNode, scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
                            : upConvolutionLayers[i][j - 1].call(lowerNode);

                        var reference = xNodes[i][0];
                        if (upSampled.shape[2] != reference.shape[2] || upSampled.shape[3] != reference.shape[3])
                        {
                            upSampled = interpolate(
                                upSampled,
                                new long[] { reference.shape[2], reference.shape[3] },
                                mode: InterpolationMode.Bilinear,
                                align_corners: false);
                        }

                        var concatInputs = new List<Tensor>();

                        for (var k = 0; k < j; k++)
                        {
                            if (k == 0 && cfg.UseAttentionGates)
                            {
                                concatInputs.Add(attentionGates[i].call((xNodes[i][k], upSampled)));
                            }
                            else
                            {
                                concatInputs.Add(xNodes[i][k]);
                            }
                        }

                        concatInputs.Add(upSampled);

                        xNodes[i][j] = decoderNestedLayers[i][j - 1].call(cat(concatInputs.ToArray(), 1));
                    }
                }

                return outLayer.call(xNodes[0][cfg.Depth]).MoveToOuterDisposeScope();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var layer in encoderLayers) layer.Dispose();
                foreach (var layer in downSamplingLayers) layer.Dispose();

                foreach (var list in upConvolutionLayers)
                {
                    foreach (var layer in list) layer.Dispose();
                }

                foreach (var list in decoderNestedLayers)
                {
                    foreach (var layer in list) layer.Dispose();
                }

                foreach (var layer in attentionGates) layer.Dispose();

                if (selfAttention != null)
                {
                    selfAttention.Dispose();
                }

                outLayer.Dispose();
                ClearModules();
            }

            base.Dispose(disposing);
        }

        public Module<Tensor, Tensor> AsModule()
        {
            return this;
        }

        public void To(Device device)
        {
            this.to(device);
        }

        public void Save(string filePath)
        {
            this.save(filePath);
        }

        public void Load(string filePath)
        {
            this.load(filePath);
        }
    }
}
