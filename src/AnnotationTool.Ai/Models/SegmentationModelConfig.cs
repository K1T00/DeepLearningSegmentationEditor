using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    public class SegmentationModelConfig
    {
        public int Depth { get; set; }
        public int FirstFilter { get; set; }
        public bool UsePooling { get; set; }
        public bool UseStridedConv { get; set; }
        public bool UseInterpolationDown { get; set; }
        public bool UseInterpolationUp { get; set; }
        public bool UseInstanceNorm { get; set; }
        public bool UseDropout { get; set; }
        public bool UseChannelAttention { get; set; }
        public bool UseAttentionGates { get; set; }
        public bool UseSelfAttention { get; set; }
        public ScalarType TrainPrecision { get; set; } = ScalarType.Float32;
    }
}
