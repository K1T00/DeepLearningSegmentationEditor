using AnnotationTool.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using static AnnotationTool.Ai.Utils.CudaOps.NativeTorchCudaOps;
using static AnnotationTool.Ai.Utils.LossFunctions;
using static TorchSharp.torch;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler.impl;

namespace AnnotationTool.Ai.Training
{
    /// <summary>
    /// Generic segmentation trainer.
    /// Stateless across runs: all runtime state is passed through TrainingContext.
    /// </summary>
    public sealed class SegmentationTrainer
    {
        private readonly ILogger<SegmentationTrainer> logger;

        public SegmentationTrainer(ILogger<SegmentationTrainer> logger)
        {
            this.logger = logger;
        }

        public async Task RunTrainer(TrainingContext ctx, IProgress<LossReport> lossProgress, CancellationToken ct)
        {
            try
            {
                var stoppingSettings = ctx.Settings.TrainingStoppingSettings;

                var maxEpochs = stoppingSettings != null && stoppingSettings.MaxIterationCount > 0
                    ? stoppingSettings.MaxIterationCount
                    : int.MaxValue;

                double? smoothedValLoss = null;

                for (var epoch = 1; epoch <= maxEpochs; epoch++)
                {
                    ct.ThrowIfCancellationRequested();

                    var trainLoss = TrainEpoch(ctx, ct);
                    var valLoss = ValidateEpoch(ctx, ct);

                    UpdateScheduler(ctx, trainLoss, valLoss, ref smoothedValLoss);

                    ReportProgress(
                        logger,
                        lossProgress,
                        epoch,
                        trainLoss,
                        valLoss,
                        GetLearningRate(ctx.Optimization.Optimizer));

                    if (ctx.StoppingMonitor.ShouldStop(epoch, valLoss, out var reason))
                    {
                        logger.LogInformation("Training stopped: {Reason}", reason);
                        break;
                    }

                    await Task.Yield();
                }
            }
            finally
            {
                if (ctx.Device.type == DeviceType.CUDA)
                {
                    EmptyCudaCache();
                }
            }
        }

        private float TrainEpoch(TrainingContext ctx, CancellationToken ct)
        {
            ctx.Model.AsModule().train();

            var totalLoss = 0.0f;
            var batchCount = 0;

            using (var scope = NewDisposeScope())
            using (var useGrad = enable_grad())
            {
                foreach (var batch in ctx.TrainLoader)
                {
                    using (var batchScope = NewDisposeScope())
                    {
                        var input = batch["data"].to(ctx.Device).MoveToOuterDisposeScope();
                        var target = batch["masks"].to(ctx.Device).MoveToOuterDisposeScope();

                        var prediction = ctx.Model.AsModule().call(input);
                        var computedLoss = ComputeLoss(ctx, prediction, target);

                        totalLoss += computedLoss.ToSingle();
                        batchCount++;

                        ctx.Optimization.Optimizer.zero_grad();
                        computedLoss.backward();

                        //ToDo: Add gradient clipping as an option in settings if needed. For now, it's commented out.
                        //if (ctx.Optimization.GradientClipNorm.HasValue)
                        //{
                        //    clip_grad_norm_(
                        //        ctx.Model.AsModule().parameters(),
                        //        ctx.Optimization.GradientClipNorm.Value);
                        //}

                        ctx.Optimization.Optimizer.step();
                    }
                }
            }

            return batchCount > 0 ? totalLoss / batchCount : 0f;
        }

        private float ValidateEpoch(TrainingContext ctx, CancellationToken ct)
        {
            ctx.Model.AsModule().eval();

            var totalLoss = 0.0f;
            var batchCount = 0;

            using (var scope = NewDisposeScope())
            using (var noGrad = no_grad())
            {
                foreach (var batch in ctx.ValLoader)
                {
                    using (var batchScope = NewDisposeScope())
                    {
                        var input = batch["data"].to(ctx.Device).MoveToOuterDisposeScope();
                        var target = batch["masks"].to(ctx.Device).MoveToOuterDisposeScope();

                        var prediction = ctx.Model.AsModule().call(input);
                        var computedLoss = ComputeLoss(ctx, prediction, target);

                        totalLoss += computedLoss.ToSingle();
                        batchCount++;
                    }
                }
            }

            return batchCount > 0 ? totalLoss / batchCount : 0f;
        }

        private static Tensor ComputeLoss(TrainingContext ctx, Tensor prediction, Tensor target)
        {
            return ctx.SegmentationMode == SegmentationMode.Binary
                ? ComputeBinarySegmentationLoss(prediction, target)
                : ComputeMulticlassSegmentationLoss(prediction, target);
        }

        private static void UpdateScheduler(
            TrainingContext ctx,
            float trainLoss,
            float valLoss,
            ref double? smoothedValLoss)
        {
            var scheduler = ctx.Optimization.Scheduler;
            if (scheduler == null)
            {
                return;
            }

            if (ctx.Optimization.LegacyMode)
            {
                if (scheduler is ReduceLROnPlateau legacyPlateau)
                {
                    legacyPlateau.step(trainLoss);
                }
                else
                {
                    scheduler.step();
                }

                return;
            }

            if (!ctx.Optimization.SchedulerRequiresMetric)
            {
                scheduler.step();
                return;
            }

            smoothedValLoss = smoothedValLoss.HasValue
                ? 0.7 * smoothedValLoss.Value + 0.3 * valLoss
                : valLoss;

            if (scheduler is ReduceLROnPlateau plateauScheduler)
            {
                plateauScheduler.step(smoothedValLoss.Value);
            }
            else
            {
                scheduler.step();
            }
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
                LearningRate = (float)Math.Round(learningRate, 8)
            });

            logger.LogInformation(
                "Epoch: {Epoch}{NewLine}TrainLoss: {TrainLoss}{NewLine}ValLoss: {ValLoss}{NewLine}LearningRate: {LearningRate}{NewLine}",
                epoch,
                Environment.NewLine,
                Math.Round(trainLoss, 4),
                Environment.NewLine,
                Math.Round(valLoss, 4),
                Environment.NewLine,
                Math.Round(learningRate, 8),
                Environment.NewLine);
        }

        private static double GetLearningRate(Optimizer optimizer)
        {
            var paramGroup = optimizer.ParamGroups.FirstOrDefault();
            return paramGroup != null ? paramGroup.LearningRate : 0.0;
        }
    }
}
