using AnnotationTool.Core.Models;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    /// <summary>
    /// SDK-safe static factory for building SegmentationModelConfig.
    /// This MUST stay in sync with ModelComplexityConfigProvider.
    /// </summary>
    public static class ModelComplexityConfigFactory
    {
        public static SegmentationModelConfig Create(ModelComplexity complexity, int imageWidth, int imageHeight)
        {
            var cfg = new SegmentationModelConfig();

            //cfg.TrainPrecision = ScalarType.BFloat16;
            cfg.TrainPrecision = ScalarType.Float32;

            if (complexity == ModelComplexity.Low)
            {
                cfg.Depth = 2;
                cfg.FirstFilter = 32;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = false;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.Medium)
            {

                cfg.Depth = 3;
                cfg.FirstFilter = 64;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = true;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.High)
            {
                cfg.Depth = 4;
                cfg.FirstFilter = 96;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = true;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            return cfg;
        }

    }
}
