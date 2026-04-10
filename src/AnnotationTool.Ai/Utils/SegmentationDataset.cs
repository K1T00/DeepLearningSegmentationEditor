using AnnotationTool.Ai.Models;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static TorchSharp.torch;


namespace AnnotationTool.Ai.Utils
{
    public class SegmentationDataset : utils.data.Dataset
    {
        private readonly IReadOnlyList<(string imagePath, string maskPath)> imagePairs;
        private readonly IPairedTransform transforms;
        private readonly SegmentationModelConfig cfg;
        private readonly bool trainAsGreyscale;
        private readonly bool binaryMode;
        private readonly NormalizationSettings normalization;

        public SegmentationDataset(
            IReadOnlyList<(string imagePath, string maskPath)> imagePairs,
            IProjectPresenter project,
            IPairedTransform transforms,
            SegmentationModelConfig cfg,
            Func<Mat, bool> maskFilter = null)
        {
            this.transforms = transforms;
            this.cfg = cfg;
            this.trainAsGreyscale = project.Project.Settings.PreprocessingSettings.TrainAsGreyscale;
            this.binaryMode = project.Project.Features.Count == 1;
            this.normalization = project.Project.Settings.PreprocessingSettings.Normalization;
            this.imagePairs = maskFilter == null
                ? imagePairs
                : FilterPairs(imagePairs, maskFilter);
        }

        public override long Count => imagePairs.Count;

        public override Dictionary<string, Tensor> GetTensor(long index)
        {
            var pair = imagePairs[(int)index];

            using (var scope = NewDisposeScope())
            using (var image = Cv2.ImRead(pair.imagePath, trainAsGreyscale ? ImreadModes.Grayscale : ImreadModes.Color))
            using (var mask = Cv2.ImRead(pair.maskPath, ImreadModes.Grayscale))
            {
                if (image.Empty())
                    throw new InvalidOperationException($"Could not load image '{pair.imagePath}'.");

                if (mask.Empty())
                    throw new InvalidOperationException($"Could not load mask '{pair.maskPath}'.");

                // Keep tensors on CPU here. Let DataLoader or trainer move them to the target device.
                var imageTensor = trainAsGreyscale
                    ? GreyMatToNormalizedTensor(image, CPU, cfg.TrainPrecision, normalization)
                    : RgbMatToNormalizedTensor(image, CPU, cfg.TrainPrecision, normalization);

                var maskTensor = binaryMode
                    ? BinaryMaskToTensor(mask, CPU)
                    : MulticlassMaskToTensor(mask, CPU);

                if (transforms != null)
                {
                    var result = transforms.Apply(imageTensor, maskTensor);
                    imageTensor = result.image;
                    maskTensor = result.mask;
                }

                return new Dictionary<string, Tensor>
                {
                    ["data"] = imageTensor.MoveToOuterDisposeScope(),
                    ["masks"] = maskTensor.MoveToOuterDisposeScope()
                };
            }
        }

        private static IReadOnlyList<(string imagePath, string maskPath)> FilterPairs(IReadOnlyList<(string imagePath, string maskPath)> source, Func<Mat, bool> maskFilter)
        {
            var filtered = new List<(string imagePath, string maskPath)>(source.Count);

            foreach (var pair in source)
            {
                using (var mask = Cv2.ImRead(pair.maskPath, ImreadModes.Grayscale))
                {
                    if (mask.Empty())
                        throw new InvalidOperationException($"Could not load mask '{pair.maskPath}'.");

                    if (maskFilter(mask))
                        filtered.Add(pair);
                }
            }

            return filtered;
        }
    }
}
