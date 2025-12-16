using AnnotationTool.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using static AnnotationTool.Ai.Utils.LossFunctions;
using static TorchSharp.torch;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler.impl;

namespace AnnotationTool.Ai.Training
{
	/// <summary>
	/// Generic segmentation trainer operating on ISegmentationModel.
	/// </summary>
	public class SegmentationTrainer
	{
		private readonly ILogger<SegmentationTrainer> logger;
		private TrainingContext ctx;

		public SegmentationTrainer(ILogger<SegmentationTrainer> logger)
		{
			this.logger = logger;
		}

		public Task RunAsync(TrainingContext ctx, IProgress<LossReport> lossProgress, CancellationToken ct)
		{
			this.ctx = ctx;

			return Task.Run(() =>
			{
				try
				{
					var stoppingSettings = ctx.Settings.TrainingStoppingSettings;
					var maxEpochs = stoppingSettings.MaxIterationCount;
					var epoch = 1;

					while (epoch <= maxEpochs && !ct.IsCancellationRequested)
					{
						ct.ThrowIfCancellationRequested();

						var trainLoss = TrainEpoch();
						var valLoss = ValidateEpoch();

						if (ctx.Optimizer.scheduler is ReduceLROnPlateau rlr)
						{
							rlr.step(trainLoss);
						}
						else
						{
							ctx.Optimizer.scheduler.step();
						}

						ReportProgress(logger, lossProgress, epoch, trainLoss, valLoss, GetLearningRate(ctx.Optimizer.optimizer));

						if (ctx.StoppingMonitor.ShouldStop(epoch, valLoss, out var reason))
						{
							logger.LogInformation(string.Format("Training stopped: {0}", reason));
							break;
						}
						epoch++;
					}
				}
				finally
				{
					if (ctx.Device.type == DeviceType.CUDA)
                        NativeTorchCudaOps.EmptyCudaCache();
                }
			}, ct);
		}

		private float TrainEpoch()
		{
			ctx.Model.AsModule().train();
			var totalLoss = 0.0f;
			var batchCount = 0;

			using (var d = NewDisposeScope())
			using (var useGrad = enable_grad())
			{
				//using var inferenceMode = inference_mode(false);

				foreach (var batch in ctx.TrainLoader)
				{
					// Execute model
					var prediction = ctx.Model.AsModule().call(batch["data"]);

					// Compute the loss: Comparing result tensor with ground truth tensor
					var computedLoss = ComputeSegmentationLoss(prediction, batch["masks"]);

					totalLoss += computedLoss.ToSingle();
					batchCount++;

					// Clear the gradients before doing the back-propagation
					ctx.Optimizer.optimizer.zero_grad();

					// Do back-propagation, which computes all the gradients
					computedLoss.backward();

					// Adjust the weights using the (newly calculated) gradients
					ctx.Optimizer.optimizer.step();

					d.DisposeEverything();
				}
			}
			return batchCount > 0 ? totalLoss / batchCount : 0;
		}

		private float ValidateEpoch()
		{
			ctx.Model.AsModule().eval();
			var totalLoss = 0.0f;
			var batchCount = 0;

			using (var d = NewDisposeScope())
			using (var noGrad = no_grad())
			{
				//using var inferenceMode = inference_mode(true);

				foreach (var batch in ctx.ValLoader)
				{
					var prediction = ctx.Model.AsModule().call(batch["data"]);

					var computedLoss = ComputeSegmentationLoss(prediction, batch["masks"]);

					totalLoss += computedLoss.ToSingle();
					batchCount++;

					d.DisposeEverything();
				}
			}
			return batchCount > 0 ? totalLoss / batchCount : 0;
		}

		private static void ReportProgress(
			ILogger logger,
			IProgress<LossReport> lossProgress,
			int epoch,
			float trainLoss,
			float valLoss,
			double learningRate)
		{
			lossProgress?.Report(new LossReport
			{
				Epoch = epoch,
				TrainLoss = (float)Math.Round(trainLoss, 4),
				ValidationLoss = (float)Math.Round(valLoss, 4),
				LearningRate = (float)Math.Round(learningRate, 4)
			});

			logger.LogInformation(
				"Epoch: " + epoch + Environment.NewLine +
				"TrainLoss: " + (float)Math.Round(trainLoss, 4) + Environment.NewLine +
				"ValLoss: " + (float)Math.Round(valLoss, 4) + Environment.NewLine +
				"LearningRate: " + (float)Math.Round(learningRate, 8) + Environment.NewLine 
				);
		}

		private static double GetLearningRate(Optimizer optimizer)
		{
			return optimizer.ParamGroups.FirstOrDefault().LearningRate;
		}

	}
}
