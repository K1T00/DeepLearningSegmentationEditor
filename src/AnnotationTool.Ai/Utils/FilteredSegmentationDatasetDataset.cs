using AnnotationTool.Core.Services;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using static TorchSharp.torch;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageAnalysis;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;


namespace AnnotationTool.Ai.Utils
{
    // This is mostly duplicattion from SegmentationDataset.
    // ToDo: refactor later (maybe this is not needed because diminishing results since one can use ROIs)
    public class FilteredSegmentationDatasetDataset : utils.data.Dataset
    {
        private readonly List<Tensor> data = new List<Tensor>();
        private readonly List<Tensor> masks = new List<Tensor>();
        private readonly IPairedTransform transforms;
        private readonly int count;
        private readonly Device device;
        private readonly IReadOnlyList<(string imagePath, string maskPath)> imagePairs;
        private readonly IProjectPresenter project;
        private readonly static ScalarType trainPrecision = ScalarType.Float32;

        public FilteredSegmentationDatasetDataset(IReadOnlyList<(string imagePath, string maskPath)> imagePairs, IProjectPresenter project, Device device, IPairedTransform transforms)
        {
            this.imagePairs = imagePairs;
            this.device = device;
            this.transforms = transforms;
            this.project = project;
            count = ReadFiles();
        }

        private int ReadFiles()
        {
            // For every image there needs to be one mask per feature
            for (var imIdx = 0; imIdx < imagePairs.Count; imIdx++)
            {

                if (project.Project.Features.Count != 1)
                    throw new ArgumentException("FilteredSegmentationDatasetDataset currently only supports one feature per project.");


                using (var mskGrey = Cv2.ImRead(imagePairs[imIdx].maskPath, ImreadModes.Grayscale))
                {
                    if (IsBlobInImage(mskGrey))
                    {
                        masks.Add(MaskToTensor(mskGrey, device, trainPrecision));

                        // Images
                        if (project.Project.Settings.PreprocessingSettings.TrainAsGreyscale)
                        {
                            using (var img = Cv2.ImRead(imagePairs[imIdx].imagePath, ImreadModes.Grayscale))
                            {
                                data.Add(
                                    GreyMatToNormalizedTensor(
                                        img,
                                        device,
                                        trainPrecision,
                                        project.Project.Settings.PreprocessingSettings.Normalization)
                                    );
                            }
                        }
                        else
                        {
                            using (var img = Cv2.ImRead(imagePairs[imIdx].imagePath, ImreadModes.Color))
                            {
                                //data.Add(RgbImageToTensor(img, device, project.Project.Settings.HyperParameters.TrainPrecision));
                                data.Add(
                                    RgbMatToNormalizedTensor(
                                        img,
                                        device,
                                        trainPrecision,
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
            var image = data[(int)index];
            var mask = masks[(int)index];

            if (transforms != null)
            {
                (image, mask) = transforms.Apply(image, mask);
            }

            return new Dictionary<string, Tensor>
            {
                { "data", image },
                { "masks", mask }
            };
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
