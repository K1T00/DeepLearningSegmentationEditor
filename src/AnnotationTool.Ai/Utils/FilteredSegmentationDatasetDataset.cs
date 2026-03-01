using AnnotationTool.Ai.Models;
using AnnotationTool.Core.Services;
using OpenCvSharp;
using System.Collections.Generic;
using TorchSharp;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageAnalysis;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static TorchSharp.torch;


namespace AnnotationTool.Ai.Utils
{
    // This is mostly duplicattion from SegmentationDataset.
    // ToDo: refactor later (or maybe this is not needed because diminishing results since one can use ROIs)
    public class FilteredSegmentationDatasetDataset : utils.data.Dataset
    {
        private readonly List<Tensor> data = new List<Tensor>();
        private readonly List<Tensor> masks = new List<Tensor>();
        private readonly IPairedTransform transforms;
        private readonly int count;
        private readonly Device device;
        private readonly IReadOnlyList<(string imagePath, string maskPath)> imagePairs;
        private readonly IProjectPresenter project;
        private readonly SegmentationModelConfig cfg;

        public FilteredSegmentationDatasetDataset(
            IReadOnlyList<(string imagePath, string maskPath)> imagePairs,
            IProjectPresenter project,
            Device device,
            IPairedTransform transforms,
            SegmentationModelConfig cfg)
        {
            this.imagePairs = imagePairs;
            this.device = device;
            this.transforms = transforms;
            this.project = project;
            this.cfg = cfg;
            this.count = ReadFiles();
        }

        private int ReadFiles()
        {
            // For every image there needs to be one mask per feature
            for (var imIdx = 0; imIdx < imagePairs.Count; imIdx++)
            {
                using (var mskGrey = Cv2.ImRead(imagePairs[imIdx].maskPath, ImreadModes.Grayscale))
                {
                    if (IsBlobInImage(mskGrey))
                    {
                        // Masks
                        if (project.Project.Features.Count == 1)
                        {

                            masks.Add(BinaryMaskToTensor(mskGrey, device));
                        }
                        else
                        {
                            masks.Add(MulticlassMaskToTensor(mskGrey, device));
                        }


                        // Images
                        if (project.Project.Settings.PreprocessingSettings.TrainAsGreyscale)
                        {
                            using (var img = Cv2.ImRead(imagePairs[imIdx].imagePath, ImreadModes.Grayscale))
                            {
                                data.Add(
                                    GreyMatToNormalizedTensor(
                                        img,
                                        device,
                                        cfg.TrainPrecision,
                                        project.Project.Settings.PreprocessingSettings.Normalization)
                                    );
                            }
                        }
                        else
                        {
                            using (var img = Cv2.ImRead(imagePairs[imIdx].imagePath, ImreadModes.Color))
                            {
                                data.Add(
                                    RgbMatToNormalizedTensor(
                                        img,
                                        device,
                                        cfg.TrainPrecision,
                                        project.Project.Settings.PreprocessingSettings.Normalization)
                                    );

                            }
                        }

                    }
                }
            }
            return data.Count;
        }

        /// <summary>
        /// Get tensor according to index
        /// </summary>
        /// <param name="index">Index for tensor</param>
        /// <returns>Tensors of index.</returns>
        public override Dictionary<string, Tensor> GetTensor(long index)
        {
            using (var scope = torch.NewDisposeScope())
            {
                var image = data[(int)index];
                var mask = masks[(int)index];

                if (transforms != null)
                {
                    var result = transforms.Apply(image, mask);
                    image = result.image;
                    mask = result.mask;
                }

                return new Dictionary<string, Tensor>{
                    { "data", image.MoveToOuterDisposeScope() },
                    { "masks", mask.MoveToOuterDisposeScope() }
                };
            }
        }

        public override long Count => count;

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            data.ForEach(d => d.Dispose());
            masks.ForEach(d => d.Dispose());
        }
    }
}
