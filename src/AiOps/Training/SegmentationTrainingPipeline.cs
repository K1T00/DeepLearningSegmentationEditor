using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Utils;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using TorchSharp;
using TorchSharp.Modules;
using static AnnotationTool.Ai.Utils.AiUtils;
using static AnnotationTool.Ai.Utils.CudaOps.CudaNativeOps;
using static AnnotationTool.Ai.Utils.LearningRateOptimizer;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static AnnotationTool.Ai.IO.ModelMetaData;
using static AnnotationTool.Ai.Utils.BatchSizeEstimator;
using static TorchSharp.torch;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler;
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

		public Task TrainAsync(IProjectPresenter project, IProgress<LossReport> lossProgress, CancellationToken ct)
		{
			return Task.Run(async () =>
			{
				try
				{
					device = ResolveDevice(project.Project.Settings);

					logger.LogInformation("Starting training on: " + device);

					// Compute normalization statistics
					project.Project.Settings.PreprocessingSettings.Normalization =
						project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ?
						ComputeGreyStats(project.Project.Images.Select(i => i.Path)) :
						ComputeRgbStats(project.Project.Images.Select(i => i.Path));

                    // Build config based on model complexity
					var cfg = complexityProvider.GetConfig(
						project.Project.Settings.TrainModelSettings.ModelComplexity,
						project.Project.Settings.PreprocessingSettings.SliceSize,
                        project.Project.Settings.PreprocessingSettings.SliceSize);

                    // Build dataset + dataloaders (+ augmentations)
                    var (trainLoader, valLoader) = BuildDataLoaders(project, device, cfg);

					var model = modelFactory.Create(project, device, cfg);
					var optimizer = BuildOptimizer(model, project.Project.Settings);

					var ctx = new TrainingContext
					{
						Device = device,
						Model = model,
						Optimizer = optimizer,
						TrainLoader = trainLoader,
						ValLoader = valLoader,
						Settings = project.Project.Settings,
						StoppingMonitor = new TrainingStopMonitor(project.Project.Settings.TrainingStoppingSettings)
					};

					// Training loop
					await trainer.RunAsync(ctx, lossProgress, ct).ConfigureAwait(false);

					logger.LogInformation("Training done.");

					// Save model and metadata
					await SaveModelAndMetadataAsync(model.AsModule(), project, projectOptionsService, jsonOptions, logger);
				}
				finally
				{
					if (device.type == DeviceType.CUDA)
					{
						EmptyCudaCache();
					}
				}
			}, ct);
		}

		private (DataLoader trainLoader, DataLoader valLoader) BuildDataLoaders(IProjectPresenter project, Device device, SegmentationModelConfig cfg)
		{
			var nWorkers = 4;

            var availableMemory =
                    project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Gpu ?
                    project.Project.GpuMemoryBudgetBytes :
                    project.Project.CpuMemoryBudgetBytes;

            var saveFraction =
                project.Project.Settings.TrainModelSettings.Device == ComputeDevice.Gpu ?
                0.65 :
                0.35;

            var batchSize = EstimateBatchSize(
                project.Project.Settings.PreprocessingSettings.SliceSize,
                project.Project.Settings.PreprocessingSettings.SliceSize,
                project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3,
                availableMemory,
                cfg.Depth,
                cfg.FirstFilter,
                cfg.TrainPrecision,
                true,
                saveFraction,
                3.0
                );


            //var batchSize = EstimateBatchSize(
            //	project.Project.Settings.PreprocessingSettings.SliceSize,
            //	project.Project.Settings.PreprocessingSettings.SliceSize,
            //	project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3,
            //	availableMemory,
            //	cfg.Depth,
            //	cfg.FirstFilter,
            //  project.Project.Settings.TrainModelSettings.Device,
            //  cfg.TrainPrecision,
            //  true,
            //	cfg.UsePooling,
            //	cfg.UseStridedConv,
            //	cfg.UseInterpolationDown,
            //	cfg.UseInterpolationUp,
            //	cfg.UseChannelAttention,
            //	cfg.UseAttentionGates,
            //	cfg.UseSelfAttention);


            var augmentations = ImageAugmentations.BuildAugmentations(project.Project.Settings.AugmentationSettings);
            var trainPairs = project.GetSlicedTrainingPairs(DatasetSplit.Train);
            var valPairs = project.GetSlicedTrainingPairs(DatasetSplit.Validate);

            Dataset finalTrainDataset = null;
            var validationDataSet = new SegmentationDataset(valPairs, project, device, augmentations);

            switch (project.Project.Settings.AugmentationSettings.AugmentationMode)
			{
				case AugmentationMode.Standard:

                    finalTrainDataset = new SegmentationDataset(project.GetSlicedTrainingPairs(DatasetSplit.Train), project, device, augmentations);
                    
                    break;
				case AugmentationMode.Duplication:

					finalTrainDataset = new ConcatSegmentationDataset(
						new SegmentationDataset(trainPairs, project, device, null), 
						new SegmentationDataset(trainPairs, project, device, augmentations));

                    break;
				case AugmentationMode.FeatureAware:

                    finalTrainDataset = new ConcatSegmentationDataset(
						new SegmentationDataset(trainPairs, project, device, null),
						new FilteredSegmentationDatasetDataset(trainPairs, project, device, augmentations));

                    break;
            }

			return (DataLoader(finalTrainDataset, batchSize, true, device, num_worker: nWorkers), 
				DataLoader(validationDataSet, batchSize, false, device, num_worker: nWorkers));
		}

		private static (Optimizer optimizer, LRScheduler scheduler) BuildOptimizer(ISegmentationModel model, DeepLearningSettings settings)
		{
			// ToDo
			var maxEpochs = 300;

			var module = model.AsModule();
			var optimizer = BuildSegmentationOptimizer(module.parameters());
			var lrScheduler = 
				BuildSegmentationScheduler(
					optimizer, 
					settings.TrainingStoppingSettings.MaxIterationCount == 0 ? maxEpochs : settings.TrainingStoppingSettings.MaxIterationCount);

			return (optimizer, lrScheduler);
		}

	}
}
