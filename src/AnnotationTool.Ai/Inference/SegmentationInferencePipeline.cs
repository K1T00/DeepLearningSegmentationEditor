using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Inference.Decoders;
using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Inference
{
    /// <summary>
    /// Universal inference pipeline that works for ANY model that implements ISegmentationModel.
    /// 
    /// Currently runs inference on project.Project.Images one by one (ToDo: run batch sized)
    /// </summary>
    public class SegmentationInferencePipeline : ISegmentationInferencePipeline
    {
        private readonly ISegmentationModelFactory modelFactory;
        private readonly ILogger<SegmentationInferencePipeline> logger;
        private readonly IModelComplexityConfigProvider complexityProvider;

        public SegmentationInferencePipeline(
            ISegmentationModelFactory modelFactory,
            ILogger<SegmentationInferencePipeline> logger,
            IProjectOptionsService projectOptionsService,
            IModelComplexityConfigProvider complexityProvider)
        {
            this.modelFactory = modelFactory;
            this.logger = logger;
            this.complexityProvider = complexityProvider;
        }

        public async Task RunInference(IProjectPresenter project, string modelPath, IProgress<int> progress, CancellationToken ct)
        {
            var device = new Device(project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Cpu
                ? DeviceType.CPU
                : DeviceType.CUDA);

            var paths = project.Paths;

            try
            {
                var segmentationMode = project.Project.Features.Count == 1
                    ? SegmentationMode.Binary
                    : SegmentationMode.Multiclass;

                var cfg = complexityProvider.GetConfig(
                    project.Project.Settings.TrainModelSettings.ModelComplexity,
                    project.Project.Settings.PreprocessingSettings.SliceSize,
                    project.Project.Settings.PreprocessingSettings.SliceSize);

                using (var model = modelFactory.Create(project.Project, device, cfg).AsModule())
                {
                    model.load(modelPath).to(cfg.TrainPrecision, true).eval();

                    using (var decoder = CreateDecoder(segmentationMode, project, device.type))
                    using (var inference = inference_mode())
                    {
                        var imgIndex = 0;
                        var total = project.Project.Images.Count;
                        var numClasses = project.Project.Features.Count + 1; // background + features

                        foreach (var img in project.Project.Images)
                        {
                            using (var d = NewDisposeScope())
                            {
                                ct.ThrowIfCancellationRequested();

                                var imagePath = Path.Combine(paths.Images, img.Guid + paths.ImagesExt);
                                var maskPath = Path.Combine(paths.Masks, img.Guid + paths.ImagesExt);

                                var imgSpace = new SegmentationImageSpace(
                                    new OpenCvSharp.Size(img.ImageSize.Width, img.ImageSize.Height),
                                    new OpenCvSharp.Rect(img.Roi.X, img.Roi.Y, img.Roi.Width, img.Roi.Height),
                                    project.Project.Settings.PreprocessingSettings.SliceSize,
                                    project.Project.Settings.PreprocessingSettings.DownSample,
                                    project.Project.Settings.PreprocessingSettings.BorderPadding);

                                var preProc = new SegmentationPreprocessor(imgSpace);
                                var postProc = new SegmentationPostprocessor(imgSpace);

                                using (var image = LoadImage(imagePath, project))
                                using (var maskGt = Cv2.ImRead(maskPath, ImreadModes.Grayscale))
                                {
                                    var sw = Stopwatch.StartNew();

                                    var imgTiles = preProc.ProcessImage(image);

                                    // Tensor conversion + forward
                                    var inputTensor = SlicedImageToTensor(
                                        imgTiles,
                                        project.Project.Settings.PreprocessingSettings.TrainAsGreyscale,
                                        device,
                                        project.Project.Settings.PreprocessingSettings.Normalization,
                                        cfg.TrainPrecision);

                                    var logits = model.call(inputTensor);
                                    var predTilesDic = decoder.Decode(logits);

                                    var fullMaskPredictions = new Dictionary<int, Mat>();
                                    foreach (var predTiles in predTilesDic)
                                    {
                                        fullMaskPredictions.Add(predTiles.Key, postProc.ProcessImageTiles(predTiles.Value));
                                    }
                                    sw.Stop();

                                    //Visualization
                                    foreach (var kv in fullMaskPredictions)
                                    {
                                        // Create visualization
                                        var (heatmap, overlay) =
                                            ImageToHeatmap(
                                                image,
                                                kv.Value[0, image.Height, 0, image.Width],
                                                project.Project.Settings.HeatmapThreshold);
                                        try
                                        {
                                            var subDirHeatmap = Path.Combine(paths.HeatmapsImages, project.Project.Features[kv.Key - 1].Name + "_" + kv.Key.ToString());
                                            var subDirOverlay = Path.Combine(paths.HeatmapsOverlays, project.Project.Features[kv.Key - 1].Name + "_" + kv.Key.ToString());

                                            // Create subdirectories for each feature if they don't exist
                                            Directory.CreateDirectory(subDirHeatmap);
                                            Directory.CreateDirectory(subDirOverlay);

                                            Cv2.ImWrite(Path.Combine(subDirHeatmap, img.Guid + paths.ImagesExt), heatmap);
                                            Cv2.ImWrite(Path.Combine(subDirOverlay, img.Guid + paths.ImagesExt), overlay);
                                        }
                                        finally
                                        {
                                            heatmap.Dispose();
                                            overlay.Dispose();
                                        }
                                    }

                                    // Segmentation stats
                                    img.SegmentationStats = decoder.ComputeMetrics(fullMaskPredictions, maskGt);
                                    img.InferenceMs = sw.Elapsed.TotalMilliseconds;

                                    DisposeTiles(imgTiles);
                                    DisposePred(fullMaskPredictions);
                                    DisposePreds(predTilesDic);
                                }
                                imgIndex++;
                                progress.Report(imgIndex == project.Project.Images.Count ? 100 : (int)(100.0 / project.Project.Images.Count * imgIndex));

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
                await Task.Yield();
            }
            finally
            {
                if (device.type == DeviceType.CUDA)
                {
                    NativeTorchCudaOps.EmptyCudaCache();
                }
            }

        }

        private static ISegmentationDecoder CreateDecoder(SegmentationMode mode, IProjectPresenter project, DeviceType device)
        {
            if (mode == SegmentationMode.Binary)
            {
                return new BinarySegmentationDecoder();
            }
            return new MulticlassSegmentationDecoder(project.Project.Features.Count + 1); // +1 for background
        }

        private static Mat LoadImage(string path, IProjectPresenter project)
        {
            return project.Project.Settings.PreprocessingSettings.TrainAsGreyscale
                ? Cv2.ImRead(path, ImreadModes.Grayscale)
                : Cv2.ImRead(path, ImreadModes.Color);
        }

    }
}