using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Utils;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
using static AnnotationTool.Ai.Utils.CudaOps.NativeTorchCudaOps;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageAnalysis;
using static TorchSharp.torch;
using static TorchSharp.torch.utils.data;


namespace AnnotationTool.Ai.Training
{
    /// <summary>
    /// High-level pipeline that orchestrates training for any ISegmentationModel produced by an ISegmentationModelFactory.
    /// </summary>
    public class SegmentationTrainingPipeline
    {
        private const int DefaultWorkerCount = 4;
        // Note: Batch size variation (450+50 -> 250+250) does not skew results significantly, so we use a fixed optimizer type for estimation
        // But BatchNorm2d should be used with a minimum batch size
        private const int MinBatchForBatchNorm = 8;

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

                EnsureNormalization(project);

                // Build training config based on model complexity
                var cfg = BuildModelConfig(project);

                // Build dataset + dataloaders (+ augmentations)
                var loaderInfo = BuildDataLoaders(project, cfg, cpuMemoryBudgetBytes, gpuMemoryBudgetBytes);

                var segmentationMode = GetSegmentationMode(project);

                var inChannels = project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3;
                var numClasses = project.Project.Features.Count;


                var model = modelFactory.Create(inChannels, numClasses, cfg, device);
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
                    SegmentationMode = segmentationMode
                };

                try
                {
                    // Training loop
                    await trainer.RunTrainer(ctx, lossProgress, ct).ConfigureAwait(false);

                    logger.LogInformation("Training done.");

                    // Save model and metadata
                    SaveModelAndMetadata(model.AsModule(), project, projectOptionsService, jsonOptions, logger);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Training canceled. Saving current model state.");

                    SaveModelAndMetadata(model.AsModule(), project, projectOptionsService, jsonOptions, logger);

                    throw;
                }
            }
            finally
            {
                if (device.type == DeviceType.CUDA)
                {
                    EmptyCudaCache();
                }
            }
        }

        private DataLoaderBuildResult BuildDataLoaders(IProjectPresenter project, SegmentationModelConfig cfg, long cpuMemoryBudgetBytes, long gpuMemoryBudgetBytes)
        {
            var settings = project.Project.Settings;
            var preprocessing = settings.PreprocessingSettings;

            var availableMemory =
                project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Gpu
                ? gpuMemoryBudgetBytes
                : cpuMemoryBudgetBytes;

            var sliceSize = preprocessing.SliceSize;
            var numChannels = preprocessing.TrainAsGreyscale ? 1 : 3;

            var trainPairs = project.GetSlicedTrainingPairs(DatasetSplit.Train);
            var valPairs = project.GetSlicedTrainingPairs(DatasetSplit.Validate);

            var batchSize = EstimateBatchSize(
                cfg,
                sliceSize,
                sliceSize,
                numChannels,
                availableMemory,
                settings.TrainModelSettings.Device,
                OptimizerType.AdamW);

            // BatchNorm should not run with very small batches.
            if (!cfg.UseInstanceNorm)
            {
                batchSize = AdjustBatchSizeIfNecessary(batchSize, trainPairs.Count, MinBatchForBatchNorm);
            }

            var augmentations = ImageAugmentations.BuildAugmentations(settings.AugmentationSettings);

            var trainDataset = BuildTrainingDataset(project, trainPairs, augmentations, cfg);

            // Validation dataset is not augmented in any mode, but we still need to apply preprocessing (e.g. normalization)
            var validationDataset = new SegmentationDataset(valPairs, project, null, cfg);

            return new DataLoaderBuildResult
            {
                BatchSize = batchSize,
                TrainLoader = DataLoader(trainDataset, batchSize, shuffle: true, device: CPU, num_worker: DefaultWorkerCount),
                ValLoader = DataLoader(validationDataset, batchSize, shuffle: false, device: CPU, num_worker: DefaultWorkerCount)
            };
        }

        private void EnsureNormalization(IProjectPresenter project)
        {
            var preprocessing = project.Project.Settings.PreprocessingSettings;
            var imgsPath = project.Paths.Images;
            var imgsExt = project.Paths.ImagesExt;

            var imagePaths = project.Project.Images.Select(i => Path.Combine(imgsPath, i.Guid + imgsExt));

            preprocessing.Normalization = preprocessing.TrainAsGreyscale
                ? ComputeGreyStats(imagePaths)
                : ComputeRgbStats(imagePaths);
        }

        private SegmentationModelConfig BuildModelConfig(IProjectPresenter project)
        {
            var preprocessing = project.Project.Settings.PreprocessingSettings;

            return complexityProvider.GetConfig(
                project.Project.Settings.TrainModelSettings.ModelComplexity,
                preprocessing.SliceSize,
                preprocessing.SliceSize);
        }

        private SegmentationMode GetSegmentationMode(IProjectPresenter project)
        {
            return project.Project.Features.Count == 1
                ? SegmentationMode.Binary
                : SegmentationMode.Multiclass;
        }

        private static Dataset BuildTrainingDataset(IProjectPresenter project, IReadOnlyList<(string imagePath, string maskPath)> trainPairs, IPairedTransform augmentations, SegmentationModelConfig cfg)
        {
            switch (project.Project.Settings.AugmentationSettings.AugmentationMode)
            {
                case AugmentationMode.Standard:
                    return new SegmentationDataset(trainPairs, project, augmentations, cfg);

                case AugmentationMode.Duplication:
                    return new ConcatSegmentationDataset(
                        new SegmentationDataset(trainPairs, project, null, cfg),
                        new SegmentationDataset(trainPairs, project, augmentations, cfg));

                case AugmentationMode.FeatureAware:
                    return new ConcatSegmentationDataset(
                        new SegmentationDataset(trainPairs, project, null, cfg),
                        new SegmentationDataset(trainPairs, project, augmentations, cfg, IsBlobInImage));

                default:
                    return new SegmentationDataset(trainPairs, project, augmentations, cfg);
            }
        }

        private sealed class DataLoaderBuildResult
        {
            public int BatchSize { get; set; }
            public DataLoader TrainLoader { get; set; }
            public DataLoader ValLoader { get; set; }
        }

    }
}
