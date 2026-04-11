using AnnotationTool.Core.Models;
using System;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models.UNet
{
    public class UNetModelFactory : ISegmentationModelFactory
    {
        public string Name
        {
            get { return "SegmentationModel"; }
        }

        public ISegmentationModel Create(int inChannels, int numClasses, SegmentationModelConfig cfg, Device device)
        {
            switch (cfg.Architecture)
            {
                case SegmentationArchitecture.UNet:
                    return new UNetFamilyModel(inChannels, numClasses, cfg, device);

                case SegmentationArchitecture.UNetPlusPlus:
                    return new UNetPlusPlusFamilyModel(inChannels, numClasses, cfg, device);

                default:
                    throw new ArgumentOutOfRangeException(nameof(cfg.Architecture), cfg.Architecture, "Unknown segmentation architecture.");
            }
        }

    }

    public static class UNetModelFactorySdk
    {
        public static string Name
        {
            get { return "SegmentationModel"; }
        }

        public static ISegmentationModel Create(int inChannels, int numClasses, SegmentationModelConfig cfg, Device device)
        {
            switch (cfg.Architecture)
            {
                case SegmentationArchitecture.UNet:
                    return new UNetFamilyModel(inChannels, numClasses, cfg, device);

                case SegmentationArchitecture.UNetPlusPlus:
                    return new UNetPlusPlusFamilyModel(inChannels, numClasses, cfg, device);

                default:
                    throw new ArgumentOutOfRangeException(nameof(cfg.Architecture), cfg.Architecture, "Unknown segmentation architecture.");
            }
        }
    }
}