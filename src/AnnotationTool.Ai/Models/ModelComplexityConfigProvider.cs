using AnnotationTool.Core.Models;
using System;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    public class ModelComplexityConfigProvider : IModelComplexityConfigProvider
    {
        /// <summary>
        /// Returns a full segmentation model preset for the requested complexity level.
        /// 
        /// 
        /// Central configuration for segmentation model architecture and training-relevant model behavior.
        /// This config defines which model family is built, how much capacity it has, how features are
        /// down/up-sampled, and which optional mechanisms such as residual blocks or attention are enabled.
        /// 
        /// 
        /// General guidance:
        /// Plain U-Net is usually the best baseline for sparse industrial datasets.
        /// Residual U-Net is often the best next step when the baseline is close but needs better optimization or more capacity.
        /// U-Net++ is more detail-focused and can improve thin boundaries, but is more complex and may overfit more easily.
        /// Attention features are most useful when backgrounds are cluttered or distracting.
        ///
        /// Practical interpretation of the main settings:
        /// Architecture:
        /// UNet is the safest default for most industrial defect segmentation tasks.
        /// UNetPlusPlus is better suited for very fine, fragmented, or boundary-sensitive defects.
        ///
        /// Depth:
        /// Lower depth produces a smaller and faster model with less overfitting risk.
        /// Higher depth increases context and receptive field, which helps with larger, more subtle, or high-resolution defects.
        /// Typical rule:
        /// Depth 2 for very small datasets or simple use cases.
        /// Depth 3 as the usual default.
        /// Depth 4+ only when the task clearly needs more global context.
        ///
        /// FirstFilter:
        /// Controls base channel width and therefore model capacity.
        /// Smaller values such as 32 are safer for very limited data.
        /// 64 is a strong default for many industrial tasks.
        /// 96+ is useful only when the task is genuinely harder and enough data or augmentation is available.
        ///
        /// Down/Up sampling:
        /// Pooling is a stable default for sparse-data segmentation.
        /// Strided convolution can be more expressive but is usually less conservative.
        /// Interpolation is simple and stable, but may preserve less learned detail than transposed convolution in some cases.
        ///
        /// UseInstanceNorm:
        /// Usually preferred when training with small batch sizes.
        /// Often better than batch normalization for segmentation workloads with limited memory.
        ///
        /// UseDropout:
        /// Can reduce overfitting in larger models, but too much dropout may weaken fine spatial prediction.
        /// Usually best kept off initially and enabled only when overfitting is observed.
        ///
        /// UseResidualBlocks:
        /// Improves gradient flow and makes deeper models easier to optimize.
        /// Often the best architectural upgrade after plain U-Net for sparse industrial datasets.
        ///
        /// UseChannelAttention:
        /// Helps reweight feature channels and can improve subtle-signal detection.
        /// Usually a secondary optimization rather than a first-line choice.
        ///
        /// UseAttentionGates:
        /// Helps suppress irrelevant skip-connection content.
        /// Most useful when the background is cluttered, textured, or visually confusing.
        ///
        /// UseSelfAttention:
        /// Adds more global context modeling.
        /// Typically more expensive and usually not the first improvement to try on very small datasets.
        ///
        /// TrainPrecision:
        /// Float32 is the safest and most stable default.
        /// Lower precision may reduce memory use but should only be enabled when already validated.
        ///
        /// Suggested settings by industrial use case:
        /// Small thin defects such as cracks, scratches, pores:
        /// UNet, Depth 3, FirstFilter 64, InstanceNorm on.
        /// If baseline misses detail, enable residual blocks before moving to more complex families.
        ///
        /// Very tiny fragmented defects with strong boundary requirements:
        /// UNetPlusPlus, Depth 3, FirstFilter 64, InstanceNorm on.
        /// Use when boundary quality is the main failure mode.
        ///
        /// Large defects or large continuous feature regions:
        /// UNet, Depth 3 or 4, FirstFilter 64 or 96, InstanceNorm on.
        /// Prefer more depth before adding attention.
        ///
        /// Very sparse datasets with only a few labeled samples:
        /// UNet, Depth 2 or 3, FirstFilter 32 or 64, minimal extras enabled.
        /// Keep the model conservative to reduce overfitting risk.
        ///
        /// Busy textured surfaces or distracting backgrounds:
        /// UNet, Depth 3, FirstFilter 64, InstanceNorm on, optional residual blocks.
        /// Attention gates may help when the main issue is background confusion.
        ///
        /// High-resolution images with subtle low-contrast defects:
        /// UNet, Depth 4, FirstFilter 96, InstanceNorm on, residual blocks on.
        /// Consider channel attention only after a strong baseline has been validated.
        /// 
        /// </summary>
        public SegmentationModelConfig GetConfig(SegmentationArchitecture architecture, ModelComplexity complexity)
        {
            // Defaults
            var cfg = new SegmentationModelConfig
            {
                TrainPrecision = ScalarType.Float32,

                // Safe defaults
                UsePooling = true,
                UseStridedConv = false,
                UseInterpolationDown = false,
                UseInterpolationUp = false,

                UseInstanceNorm = false,
                UseDropout = false,

                UseResidualBlocks = false,
                UseChannelAttention = false,
                UseAttentionGates = false,
                UseSelfAttention = false
            };

            switch (complexity)
            {
                case ModelComplexity.L0:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 2;
                    cfg.FirstFilter = 16;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = true;

                    cfg.UseInstanceNorm = false;
                    break;

                case ModelComplexity.L1:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 2;
                    cfg.FirstFilter = 32;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = false;
                    break;

                case ModelComplexity.L2:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 3;
                    cfg.FirstFilter = 32;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    break;

                case ModelComplexity.L3:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 3;
                    cfg.FirstFilter = 64;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    cfg.UseResidualBlocks = true;
                    break;

                case ModelComplexity.L4:
                    cfg.Architecture = SegmentationArchitecture.UNetPlusPlus;
                    cfg.Depth = 4;
                    cfg.FirstFilter = 64;

                    cfg.UsePooling = true;
                    cfg.UseStridedConv = false;
                    cfg.UseInterpolationDown = false;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    cfg.UseDropout = true;

                    cfg.UseResidualBlocks = true;
                    cfg.UseChannelAttention = true;
                    cfg.UseAttentionGates = true;
                    cfg.UseSelfAttention = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(complexity), complexity, "Model complexity must be between 0 and 5.");
            }

            return cfg;
        }
    }

    public static class ModelComplexityConfigProviderSdk
    {
        public static SegmentationModelConfig GetConfig(SegmentationArchitecture architecture, ModelComplexity complexity)
        {
            // Defaults
            var cfg = new SegmentationModelConfig
            {
                TrainPrecision = ScalarType.Float32,

                // Safe defaults
                UsePooling = true,
                UseStridedConv = false,
                UseInterpolationDown = false,
                UseInterpolationUp = false,

                UseInstanceNorm = false,
                UseDropout = false,

                UseResidualBlocks = false,
                UseChannelAttention = false,
                UseAttentionGates = false,
                UseSelfAttention = false
            };

            switch (complexity)
            {
                case ModelComplexity.L0:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 2;
                    cfg.FirstFilter = 16;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = true;

                    cfg.UseInstanceNorm = false;
                    break;

                case ModelComplexity.L1:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 2;
                    cfg.FirstFilter = 32;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = false;
                    break;

                case ModelComplexity.L2:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 3;
                    cfg.FirstFilter = 32;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    break;

                case ModelComplexity.L3:
                    cfg.Architecture = SegmentationArchitecture.UNet;
                    cfg.Depth = 3;
                    cfg.FirstFilter = 64;

                    cfg.UsePooling = true;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    cfg.UseResidualBlocks = true;
                    break;

                case ModelComplexity.L4:
                    cfg.Architecture = SegmentationArchitecture.UNetPlusPlus;
                    cfg.Depth = 4;
                    cfg.FirstFilter = 64;

                    cfg.UsePooling = true;
                    cfg.UseStridedConv = false;
                    cfg.UseInterpolationDown = false;
                    cfg.UseInterpolationUp = false;

                    cfg.UseInstanceNorm = true;
                    cfg.UseDropout = true;

                    cfg.UseResidualBlocks = true;
                    cfg.UseChannelAttention = true;
                    cfg.UseAttentionGates = true;
                    cfg.UseSelfAttention = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(complexity), complexity, "Model complexity must be between 0 and 5.");
            }

            return cfg;
        }
    }
}