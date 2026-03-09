using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Utils;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using TorchSharp.Modules;
using static AnnotationTool.Ai.IO.ModelMetaData;
using static AnnotationTool.Ai.Utils.AiUtils;
using static AnnotationTool.Ai.Utils.BatchSizeEstimator;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static TorchSharp.torch;
using static TorchSharp.torch.utils.data;


namespace AnnotationTool.Ai.Training
{
    /// <summary>
    /// High-level pipeline that orchestrates training for any ISegmentationModel produced by an ISegmentationModelFactory.
    /// </summary>
    public class SegmentationTrainingPipeline
    {
        private readonly ILogger<SegmentationTrainingPipeline> logger;
        private readonly ISegmentationModelFactory modelFactory;
        private readonly SegmentationTrainer trainer;
        private readonly IProjectOptionsService projectOptionsService;
        private readonly JsonSerializerOptions jsonOptions;
        private Device device;
        private readonly IModelComplexityConfigProvider complexityProvider;


        public SegmentationTrainingPipeline(
            ILogger<SegmentationTrainingPipeline> logger,
            ISegmentationModelFactory modelFactory,
            SegmentationTrainer trainer,
            IProjectOptionsService projectOptionsService,
            JsonSerializerOptions jsonOptions,
            IModelComplexityConfigProvider complexityProvider)
        {
            this.logger = logger;
            this.modelFactory = modelFactory;
            this.trainer = trainer;
            this.projectOptionsService = projectOptionsService;
            this.jsonOptions = jsonOptions;
            this.complexityProvider = complexityProvider;
        }

        public async Task RunTraining(IProjectPresenter project, IProgress<LossReport> lossProgress, CancellationToken ct, long cpuMemoryBudgetBytes, long gpuMemoryBudgetBytes)
        {
            try
            {
                device = ResolveDevice(project.Project.Settings);
                logger.LogInformation("Starting training on: {Device}", device);

                var imgsPath = project.Paths.Images;
                var imgsExt = project.Paths.ImagesExt;

                // Compute normalization statistics
                project.Project.Settings.PreprocessingSettings.Normalization =
                    project.Project.Settings.PreprocessingSettings.TrainAsGreyscale
                    ? ComputeGreyStats(project.Project.Images.Select(i => Path.Combine(imgsPath, i.Guid + imgsExt)))
                    : ComputeRgbStats(project.Project.Images.Select(i => Path.Combine(imgsPath, i.Guid + imgsExt)));

                // Build training config based on model complexity
                var cfg = complexityProvider.GetConfig(
                    project.Project.Settings.TrainModelSettings.ModelComplexity,
                    project.Project.Settings.PreprocessingSettings.SliceSize,
                    project.Project.Settings.PreprocessingSettings.SliceSize);

                // Build dataset + dataloaders (+ augmentations)
                var loaderInfo = BuildDataLoaders(project, device, cfg, cpuMemoryBudgetBytes, gpuMemoryBudgetBytes);

                var model = modelFactory.Create(project.Project, device, cfg);

                var optimization = TrainingOptimizationFactory.Build(model, project.Project.Settings, cfg, loaderInfo.BatchSize);

                var ctx = new TrainingContext
                {
                    Device = device,
                    Model = model,
                    Optimization = optimization,
                    TrainLoader = loaderInfo.TrainLoader,
                    ValLoader = loaderInfo.ValLoader,
                    Settings = project.Project.Settings,
                    StoppingMonitor = new TrainingStopMonitor(project.Project.Settings.TrainingStoppingSettings),
                    SegmentationMode = project.Project.Features.Count == 1
                      ? SegmentationMode.Binary
                      : SegmentationMode.Multiclass
                };

                // Training loop
                await trainer.RunTrainer(ctx, lossProgress, ct).ConfigureAwait(false);

                logger.LogInformation("Training done.");

                // Save model and metadata
                SaveModelAndMetadata(model.AsModule(), project, projectOptionsService, jsonOptions, logger);
            }
            finally
            {
                if (device.type == DeviceType.CUDA)
                {
                    NativeTorchCudaOps.EmptyCudaCache();
                }
            }
        }

        private DataLoaderBuildResult BuildDataLoaders(
            IProjectPresenter project,
            Device device,
            SegmentationModelConfig cfg,
            long cpuMemoryBudgetBytes,
            long gpuMemoryBudgetBytes)
        {
            var nWorkers = 4;
            // Note: Batch size variation (450+50 -> 250+250) does not skew results significantly, so we use a fixed optimizer type for estimation
            // But BatchNorm2d should be used with a minimum batch size
            const int MinBatchForBatchNorm = 8;

            var settings = project.Project.Settings;
            var preprocessing = settings.PreprocessingSettings;

            var availableMemory =
                project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Gpu
                ? gpuMemoryBudgetBytes
                : cpuMemoryBudgetBytes;

            var sliceSize = preprocessing.SliceSize;
            var numChannels = preprocessing.TrainAsGreyscale ? 1 : 3;

            var batchSize = EstimateBatchSize(
                cfg,
                sliceSize,
                sliceSize,
                numChannels,
                availableMemory,
                settings.TrainModelSettings.Device,
                OptimizerType.AdamW);

            var trainPairs = project.GetSlicedTrainingPairs(DatasetSplit.Train);
            var valPairs = project.GetSlicedTrainingPairs(DatasetSplit.Validate);

            // BatchNorm should not run with very small batches.
            if (!cfg.UseInstanceNorm)
            {
                batchSize = AdjustBatchSizeIfNecessary(batchSize, trainPairs.Count, MinBatchForBatchNorm);
            }

            var augmentations = ImageAugmentations.BuildAugmentations(settings.AugmentationSettings);

            // Build datasets according to augmentation mode
            Dataset finalTrainDataset = null;

            // Validation dataset is not augmented in any mode, but we still need to apply preprocessing (e.g. normalization)
            var validationDataset = new SegmentationDataset(valPairs, project, device, null, cfg);

            switch (project.Project.Settings.AugmentationSettings.AugmentationMode)
            {
                case AugmentationMode.Standard:

                    finalTrainDataset = new SegmentationDataset(trainPairs, project, device, augmentations, cfg);

                    break;
                case AugmentationMode.Duplication:

                    finalTrainDataset = new ConcatSegmentationDataset(
                        new SegmentationDataset(trainPairs, project, device, null, cfg),
                        new SegmentationDataset(trainPairs, project, device, augmentations, cfg));

                    break;
                case AugmentationMode.FeatureAware:

                    finalTrainDataset = new ConcatSegmentationDataset(
                        new SegmentationDataset(trainPairs, project, device, null, cfg),
                        new FilteredSegmentationDatasetDataset(trainPairs, project, device, augmentations, cfg));

                    break;

                default:
                    finalTrainDataset = new SegmentationDataset(trainPairs, project, device, augmentations, cfg);
                    break;
            }
            return new DataLoaderBuildResult
            {
                BatchSize = batchSize,
                TrainLoader = DataLoader(finalTrainDataset, batchSize, true, device, num_worker: nWorkers),
                ValLoader = DataLoader(validationDataset, batchSize, false, device, num_worker: nWorkers)
            };
        }

        private sealed class DataLoaderBuildResult
        {
            public int BatchSize { get; set; }
            public DataLoader TrainLoader { get; set; }
            public DataLoader ValLoader { get; set; }
        }

    }
}
