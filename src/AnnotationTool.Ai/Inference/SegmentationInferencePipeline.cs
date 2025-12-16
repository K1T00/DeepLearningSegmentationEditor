using AnnotationTool.Ai.Models;
using AnnotationTool.Core.IO;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageComposition;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;
using static AnnotationTool.Ai.Utils.DatasetStatistics;

namespace AnnotationTool.Ai.Inference
{
    /// <summary>
    /// Universal inference pipeline that works for ANY model that implements ISegmentationModel.
    /// 
    /// Currently runs inference on project.Project.Images one by one
    /// </summary>
    public class SegmentationInferencePipeline : ISegmentationInferencePipeline
    {
        private readonly ISegmentationModelFactory modelFactory;
        private readonly ILogger<SegmentationInferencePipeline> logger;
        private readonly IProjectOptionsService projectOptionsService;
        private Device device;
        private readonly IModelComplexityConfigProvider complexityProvider;

        public SegmentationInferencePipeline(
            ISegmentationModelFactory modelFactory,
            ILogger<SegmentationInferencePipeline> logger,
            IProjectOptionsService projectOptionsService,
            IModelComplexityConfigProvider complexityProvider)
        {
            this.modelFactory = modelFactory;
            this.logger = logger;
            this.projectOptionsService = projectOptionsService;
            this.complexityProvider = complexityProvider;
        }

        public Task RunAsync(
            IProjectPresenter project,
            string modelPath,
            IProgress<int> progress,
            CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    var deviceType = project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Cpu
                    ? DeviceType.CPU
                    : DeviceType.CUDA;
                    device = new Device(deviceType);

                    var heatmapImagesPath = projectOptionsService.GetFolderPath(project.ProjectPath, ProjectFolderType.HeatmapsImages);
                    var heatmapOverlaysPath = projectOptionsService.GetFolderPath(project.ProjectPath, ProjectFolderType.HeatmapsOverlays);

                    // Build config based on model complexity
                    var cfg = complexityProvider.GetConfig(
                        project.Project.Settings.TrainModelSettings.ModelComplexity,
                        project.Project.Settings.PreprocessingSettings.SliceSize,
                        project.Project.Settings.PreprocessingSettings.SliceSize);

                    using (var model = modelFactory.Create(project, device, cfg).AsModule())
                    using (var image = new Mat())
                    using (var maskGroundTruth = new Mat())
                    {
                        ct.ThrowIfCancellationRequested();


                        // Load model file
                        model.load(modelPath).to(cfg.TrainPrecision, true).eval();

                        using (var d = NewDisposeScope())
                        //using (var noGrad = no_grad())
                        using (var inferenceMode = inference_mode())
                        {
                            var i = 0;
                            foreach (var img in project.Project.Images)
                            {
                                ct.ThrowIfCancellationRequested();

                                if (project.Project.Settings.PreprocessingSettings.TrainAsGreyscale)
                                {
                                    Cv2.ImRead(img.Path, ImreadModes.Grayscale).CopyTo(image);
                                }
                                else
                                {
                                    Cv2.ImRead(img.Path, ImreadModes.Color).CopyTo(image);
                                }

                                Cv2.ImRead(img.MaskPath, ImreadModes.Grayscale).CopyTo(maskGroundTruth);

                                var sw = Stopwatch.StartNew();

                                var imageDs = DownSampleImage(image, project.Project.Settings.PreprocessingSettings.DownSample);
                                var (nRowImages, nColumnImages) = GetAmtImages(imageDs.Height, imageDs.Width, project.Project.Settings.PreprocessingSettings.SliceSize, project.Project.Settings.PreprocessingSettings.BorderPadding);
                                var slicedImages = SliceImage(imageDs, project.Project.Settings.PreprocessingSettings.SliceSize, project.Project.Settings.PreprocessingSettings.BorderPadding, nRowImages, nColumnImages);

                                var roiImagesTensor =
                                    SlicedImageToTensor(
                                        slicedImages,
                                        project.Project.Settings.PreprocessingSettings.TrainAsGreyscale,
                                        device,
                                        project.Project.Settings.PreprocessingSettings.Normalization,
                                        cfg.TrainPrecision);

                                var prediction = model.call(roiImagesTensor);
                                var predictionSplit = TensorTo2DArray(functional.sigmoid(prediction));
                                var resPredictionGreyImages = SlicedImageTensorToImage(predictionSplit);
                                var mergedPredictionGreyImage = MergeImages(resPredictionGreyImages, nRowImages, nColumnImages);
                                var predictionImageUs = UpSampleImage(mergedPredictionGreyImage, project.Project.Settings.PreprocessingSettings.DownSample);

                                // Depending on the up-sampled prediction image dimensions we need to add padding to the right and bottom of the image
                                var paddedPredictionImageUs = new Mat();
                                Cv2.CopyMakeBorder(
                                    predictionImageUs,
                                    paddedPredictionImageUs,
                                    0,
                                    image.Height - predictionImageUs.Height < 0 ? 0 : image.Height - predictionImageUs.Height,
                                    0,
                                    image.Width - predictionImageUs.Width < 0 ? 0 : image.Width - predictionImageUs.Width,
                                    BorderTypes.Constant,
                                    OpenCvSharp.Scalar.Black);

                                sw.Stop();

                                var (heatmapImage, heatmapOverlay) = ImageToHeatmap(
                                    image,
                                    paddedPredictionImageUs[0, image.Height, 0, image.Width], project.Project.Settings.HeatmapThreshold);


                                img.SegmentationStats = ComputeMetrics(paddedPredictionImageUs[0, image.Height, 0, image.Width],
                                    maskGroundTruth);
                                img.SegmentationStats.InferenceMs = sw.Elapsed.TotalMilliseconds;

                                Cv2.ImWrite(Path.Combine(heatmapImagesPath, img.Guid + ".png"), heatmapImage);
                                Cv2.ImWrite(Path.Combine(heatmapOverlaysPath, img.Guid + ".png"), heatmapOverlay);

                                i++;

                                progress.Report(i == project.Project.Images.Count ? 100 : (int)(100.0 / project.Project.Images.Count * i));

                                d.DisposeEverything();

                                if (device.type == DeviceType.CUDA)
                                {
                                    NativeTorchCudaOps.EmptyCudaCache();
                                }

                                if (ct.IsCancellationRequested)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (device.type == DeviceType.CUDA)
                    {
                        NativeTorchCudaOps.EmptyCudaCache();
                    }
                }
            }, ct);
        }
    }
}