using AnnotationTool.Core.Models;

namespace AnnotationTool.Ai.Models
{
    public interface IModelComplexityConfigProvider
    {
        SegmentationModelConfig GetConfig(SegmentationArchitecture architecture, ModelComplexity complexity);
    }
}