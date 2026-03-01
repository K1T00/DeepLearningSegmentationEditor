using AnnotationTool.Core.Models;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    /// <summary>
    /// Factory interface to create configured segmentation models.
    ///</summary>
    public interface ISegmentationModelFactory
    {
        /// <summary>
        /// Model name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates a segmentation model for the given settings + device.
        /// </summary>
        ISegmentationModel Create(DeepLearningProject project, Device device, SegmentationModelConfig cfg);
    }
}
