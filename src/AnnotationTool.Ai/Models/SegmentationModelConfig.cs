using AnnotationTool.Core.Models;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    public class SegmentationModelConfig
    {
        /// <summary>
        /// Selects the segmentation model family to build.
        /// Use UNet as the default for most industrial defect segmentation tasks.
        /// Use UNetPlusPlus when thin structures, fragmented defects, or boundary quality are the main challenge.
        /// </summary>
        public SegmentationArchitecture Architecture { get; set; }

        /// <summary>
        /// Number of encoder/decoder resolution stages.
        /// Lower values produce smaller and faster models with less overfitting risk.
        /// Higher values increase receptive field and global context, which can help on large, subtle, or high-resolution defects.
        /// Typical guidance:
        /// 2 = conservative and fast,
        /// 3 = strong default,
        /// 4+ = only for harder use cases with enough data or augmentation.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Base number of feature channels in the first stage.
        /// This strongly affects total model capacity.
        /// Typical guidance:
        /// 32 = conservative for sparse data,
        /// 64 = balanced default,
        /// 96+ = higher-capacity option for harder tasks.
        /// </summary>
        public int FirstFilter { get; set; }

        /// <summary>
        /// Enables max-pooling based downsampling.
        /// This is usually the safest and most stable default for sparse industrial segmentation.
        /// </summary>
        public bool UsePooling { get; set; }

        /// <summary>
        /// Enables strided-convolution downsampling.
        /// Can be more expressive than pooling, but is usually a less conservative choice for small datasets.
        /// </summary>
        public bool UseStridedConv { get; set; }

        /// <summary>
        /// Enables interpolation-based downsampling.
        /// This is simple and lightweight, but often less common than pooling for segmentation backbones.
        /// </summary>
        public bool UseInterpolationDown { get; set; }

        /// <summary>
        /// Enables interpolation-based upsampling instead of learned transposed convolution.
        /// Interpolation is often stable and simple.
        /// Learned upsampling can recover more detail, but may be more sensitive.
        /// </summary>
        public bool UseInterpolationUp { get; set; }

        /// <summary>
        /// Enables instance normalization.
        /// Usually preferred over batch normalization when training segmentation models with small batch sizes.
        /// Recommended for most medium and high complexity industrial runs.
        /// </summary>
        public bool UseInstanceNorm { get; set; }

        /// <summary>
        /// Enables dropout in convolution blocks.
        /// Useful when larger models begin to overfit, but should usually remain off for initial experiments.
        /// </summary>
        public bool UseDropout { get; set; }

        /// <summary>
        /// Enables residual blocks inside convolution stages.
        /// Residual connections often improve optimization stability and are a strong next step beyond plain U-Net,
        /// especially for deeper models and subtle industrial defects.
        /// </summary>
        public bool UseResidualBlocks { get; set; }

        /// <summary>
        /// Enables channel attention inside convolution blocks.
        /// Can help highlight subtle informative features, especially in low-contrast or texture-heavy tasks.
        /// Usually a secondary refinement rather than a first-line setting.
        /// </summary>
        public bool UseChannelAttention { get; set; }

        /// <summary>
        /// Enables attention gates on skip connections.
        /// Most useful when the background contains clutter, strong textures, or distractors that should be suppressed.
        /// </summary>
        public bool UseAttentionGates { get; set; }

        /// <summary>
        /// Enables self-attention for more global context modeling.
        /// Can help when larger spatial context matters, but adds complexity and is usually not the first improvement
        /// to try for sparse-data industrial segmentation.
        /// </summary>
        public bool UseSelfAttention { get; set; }

        /// <summary>
        /// Numeric precision used during training and inference.
        /// Float32 is the safest and most stable default.
        /// Lower precision can reduce memory usage, but should only be used when validated for the target workload.
        /// </summary>
        public ScalarType TrainPrecision { get; set; }
    }
}