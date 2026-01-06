using AnnotationTool.Ai.Models.UNet;
using AnnotationTool.Core.Models;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Models
{
    /// <summary>
    /// SDK-safe segmentation model factory.
    /// </summary>
    public static class SegmentationModelFactoryForSdk
    {
        public static Module<Tensor, Tensor> Create(DeepLearningSettings settings, Device device, SegmentationModelConfig cfg, int numClasses)
        {
            return new UNetModel(settings, device, cfg, numClasses).AsModule();
        }
    }
}
