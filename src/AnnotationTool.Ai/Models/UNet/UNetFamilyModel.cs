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
    /// Configurable U-Net family model.
    /// Supports:
    /// - plain U-Net
    /// - residual U-Net
    /// - attention U-Net
    /// - residual attention U-Net
    /// </summary>
    public sealed class UNetFamilyModel : Module<Tensor, Tensor>, ISegmentationModel
    {
        private readonly ModuleList<Module<Tensor, Tensor>> encoderLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly ModuleList<Module<Tensor, Tensor>> downSamplingLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly Module<Tensor, Tensor> bottleneckLayer;
        private readonly Module<Tensor, Tensor> selfAttention;
        private readonly ModuleList<Module<Tensor, Tensor>> upConvolutionLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly ModuleList<Module<Tensor, Tensor>> decoderLayers = new ModuleList<Module<Tensor, Tensor>>();
        private readonly ModuleList<Module<(Tensor, Tensor), Tensor>> attentionGates = new ModuleList<Module<(Tensor, Tensor), Tensor>>();
        private readonly Module<Tensor, Tensor> outLayer;

        private readonly SegmentationModelConfig cfg;

        public UNetFamilyModel(int inChannels, int numClasses, SegmentationModelConfig cfg, Device device) : base(nameof(UNetFamilyModel))
        {
            this.cfg = cfg;

            var outChannels = GetOutChannels(numClasses);
            var filterSizes = BuildFilterSizes();
            var inC = inChannels;

            // Encoder
            for (var nLayer = 0; nLayer < cfg.Depth; nLayer++)
            {
                encoderLayers.Add(
                    BuildConvBlock(
                        string.Format("EncoderLayer{0}", nLayer),
                        inC,
                        filterSizes[nLayer],
                        device));

                downSamplingLayers.Add(
                    DownSamplingBuilder.Build(
                        string.Format("DownSamplingLayer{0}", nLayer),
                        filterSizes[nLayer],
                        cfg.UsePooling,
                        cfg.UseStridedConv,
                        cfg.UseInterpolationDown,
                        device,
                        cfg.TrainPrecision));

                inC = filterSizes[nLayer];
            }

            // Bottleneck
            bottleneckLayer = BuildConvBlock("Bottleneck", inC, filterSizes[cfg.Depth], device);

            if (cfg.UseSelfAttention)
            {
                selfAttention = new SelfAttention(filterSizes[cfg.Depth], device, cfg.TrainPrecision);
            }

            inC = filterSizes[cfg.Depth];

            // Decoder
            for (var nLayer = cfg.Depth - 1; nLayer >= 0; nLayer--)
            {
                upConvolutionLayers.Add(
                    UpConvolutionBuilder.Build(
                        string.Format("UpConvolutionLayer{0}", nLayer),
                        inC,
                        filterSizes[nLayer],
                        cfg.UseInterpolationUp,
                        device,
                        cfg.TrainPrecision));

                decoderLayers.Add(
                    BuildConvBlock(
                        string.Format("DecoderLayer{0}", nLayer),
                        2 * filterSizes[nLayer],
                        filterSizes[nLayer],
                        device));

                if (cfg.UseAttentionGates)
                {
                    attentionGates.Add(
                        new AttentionGate(
                            filterSizes[nLayer],
                            filterSizes[nLayer],
                            filterSizes[nLayer],
                            device,
                            cfg.TrainPrecision));
                }

                inC = filterSizes[nLayer];
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
                var skips = new List<Tensor>();
                var x = input;

                for (var i = 0; i < encoderLayers.Count; i++)
                {
                    x = encoderLayers[i].call(x);
                    skips.Add(x);

                    x = cfg.UseInterpolationDown
                        ? interpolate(x, scale_factor: new double[] { 0.5, 0.5 }, mode: InterpolationMode.Bilinear, align_corners: false)
                        : downSamplingLayers[i].call(x);
                }

                x = bottleneckLayer.call(x);

                if (selfAttention != null)
                {
                    x = selfAttention.call(x);
                }

                for (var i = 0; i < decoderLayers.Count; i++)
                {
                    var skip = skips[skips.Count - 1 - i];

                    var upSampled = cfg.UseInterpolationUp
                        ? interpolate(x, scale_factor: new double[] { 2.0, 2.0 }, mode: InterpolationMode.Bilinear, align_corners: false)
                        : upConvolutionLayers[i].call(x);

                    if (upSampled.shape[2] != skip.shape[2] || upSampled.shape[3] != skip.shape[3])
                    {
                        skip = interpolate(
                            skip,
                            new long[] { upSampled.shape[2], upSampled.shape[3] },
                            mode: InterpolationMode.Bilinear,
                            align_corners: false);
                    }

                    if (cfg.UseAttentionGates)
                    {
                        skip = attentionGates[i].call((skip, upSampled));
                    }

                    x = decoderLayers[i].call(cat(new[] { upSampled, skip }, 1));
                }

                return outLayer.call(x).MoveToOuterDisposeScope();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var layer in encoderLayers) layer.Dispose();
                foreach (var layer in downSamplingLayers) layer.Dispose();
                foreach (var layer in upConvolutionLayers) layer.Dispose();
                foreach (var layer in decoderLayers) layer.Dispose();
                foreach (var layer in attentionGates) layer.Dispose();

                bottleneckLayer.Dispose();

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
