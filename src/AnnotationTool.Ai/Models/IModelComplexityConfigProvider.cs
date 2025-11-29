using AnnotationTool.Core.Models;

namespace AnnotationTool.Ai.Models
{
    public interface IModelComplexityConfigProvider
    {
        SegmentationModelConfig GetConfig(
            ModelComplexity complexity,
            int imageWidth,
            int imageHeight);
    }
}
