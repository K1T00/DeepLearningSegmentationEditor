using AnnotationTool.Ai.Models;
using AnnotationTool.Core.Models;
using System;
using System.Globalization;
using System.Text;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler;

namespace AnnotationTool.Ai.Training
{
    internal static class TrainingOptimizationFactory
    {
        public static TrainingOptimizationContext Build(ISegmentationModel model, DeepLearningSettings settings, SegmentationModelConfig modelConfig, int batchSize)
        {
            var preprocessing = settings.PreprocessingSettings;
            var stopping = settings.TrainingStoppingSettings;

            var initialLearningRate = GetInitialLearningRate(modelConfig, preprocessing, batchSize);
            var weightDecay = GetWeightDecay(modelConfig, preprocessing);
            var minLearningRate = GetMinimumLearningRate(initialLearningRate);

            var optimizer = AdamW(
                model.AsModule().parameters(),
                initialLearningRate,
                weight_decay: weightDecay);

            LRScheduler scheduler = null;
            var schedulerRequiresMetric = false;
            string schedulerName = null;

            if (stopping != null && stopping.IterationWithoutImprovement > 0)
            {
                var plateauPatience = GetPlateauPatience(stopping.IterationWithoutImprovement);

                scheduler = ReduceLROnPlateau(
                    optimizer,
                    mode: "min",
                    factor: 0.2,
                    patience: plateauPatience,
                    threshold: 1e-4,
                    threshold_mode: "rel",
                    cooldown: 1,
                    min_lr: new double[] { minLearningRate },
                    eps: 1e-8,
                    verbose: true);

                schedulerRequiresMetric = true;
                schedulerName = "ReduceLROnPlateau";
            }
            else if (stopping != null && stopping.MaxIterationCount > 0)
            {
                var stepSize = GetStepSize(stopping.MaxIterationCount);

                scheduler = StepLR(
                    optimizer,
                    step_size: stepSize,
                    gamma: 0.5);

                schedulerRequiresMetric = false;
                schedulerName = "StepLR";
            }

            const double gradientClipNorm = 1.0;

            return new TrainingOptimizationContext
            {
                Optimizer = optimizer,
                Scheduler = scheduler,
                SchedulerRequiresMetric = schedulerRequiresMetric,
                InitialLearningRate = initialLearningRate,
                WeightDecay = weightDecay,
                MinLearningRate = scheduler != null ? (double?)minLearningRate : null,
                OptimizerName = "AdamW",
                SchedulerName = schedulerName,
                GradientClipNorm = gradientClipNorm,
                PolicySummary = BuildPolicySummary(
                    "AdamW",
                    schedulerName,
                    initialLearningRate,
                    weightDecay,
                    minLearningRate,
                    gradientClipNorm,
                    batchSize,
                    modelConfig,
                    preprocessing,
                    stopping)
            };
        }

        private static double GetInitialLearningRate(SegmentationModelConfig modelConfig, PreprocessingSettings preprocessing, int batchSize)
        {
            var baseLearningRate = GetBaseLearningRate(modelConfig);
            var learningRate = ScaleLearningRateForBatchSize(baseLearningRate, batchSize);
            learningRate = ScaleLearningRateForPreprocessing(learningRate, preprocessing);

            return Clamp(learningRate, 1e-2, 1e-1);
        }

        private static double GetBaseLearningRate(SegmentationModelConfig modelConfig)
        {
            if (modelConfig.Depth <= 2 && modelConfig.FirstFilter <= 32)
                return 0.05;

            if (modelConfig.Depth <= 3 && modelConfig.FirstFilter <= 64)
                return 0.01;

            return 0.05;
        }

        private static double ScaleLearningRateForBatchSize(double baseLearningRate, int batchSize)
        {
            return baseLearningRate * Math.Sqrt(batchSize / 4.0);
        }

        private static double ScaleLearningRateForPreprocessing(double learningRate, PreprocessingSettings preprocessing)
        {
            if (preprocessing == null)
                return learningRate;

            if (preprocessing.SliceSize <= 96)
                learningRate *= 0.95;
            else if (preprocessing.SliceSize >= 256)
                learningRate *= 1.05;

            if (preprocessing.TrainAsGreyscale)
                learningRate *= 1.05;

            return learningRate;
        }

        private static double GetWeightDecay(SegmentationModelConfig modelConfig, PreprocessingSettings preprocessing)
        {
            if (preprocessing != null && preprocessing.TrainOnlyFeatures)
                return 5e-5;

            return 1e-4;
        }

        private static double GetMinimumLearningRate(double initialLearningRate)
        {
            var minLearningRate = initialLearningRate * 0.01;
            return Clamp(minLearningRate, 1e-4, 1e-3);
        }

        private static int GetPlateauPatience(int iterationWithoutImprovement)
        {
            return Math.Max(1, iterationWithoutImprovement / 4);
        }

        private static int GetStepSize(int maxIterationCount)
        {
            return Math.Max(3, maxIterationCount / 5);
        }

        private static string BuildPolicySummary(
            string optimizerName,
            string schedulerName,
            double initialLearningRate,
            double weightDecay,
            double minLearningRate,
            double? gradientClipNorm,
            int batchSize,
            SegmentationModelConfig modelConfig,
            PreprocessingSettings preprocessing,
            TrainingStoppingSettings stopping)
        {
            var sb = new StringBuilder();

            sb.Append("Optimizer=").Append(optimizerName);
            sb.Append(", Scheduler=").Append(string.IsNullOrWhiteSpace(schedulerName) ? "None" : schedulerName);
            sb.Append(", LR=").Append(FormatDouble(initialLearningRate));
            sb.Append(", WeightDecay=").Append(FormatDouble(weightDecay));
            sb.Append(", MinLR=").Append(FormatDouble(minLearningRate));
            sb.Append(", BatchSize=").Append(batchSize);

            if (gradientClipNorm.HasValue)
                sb.Append(", GradClip=").Append(FormatDouble(gradientClipNorm.Value));

            if (modelConfig != null)
            {
                sb.Append(", Depth=").Append(modelConfig.Depth);
                sb.Append(", FirstFilter=").Append(modelConfig.FirstFilter);
                sb.Append(", InstanceNorm=").Append(modelConfig.UseInstanceNorm);
            }

            if (preprocessing != null)
            {
                sb.Append(", SliceSize=").Append(preprocessing.SliceSize);
                sb.Append(", DownSample=").Append(preprocessing.DownSample);
                sb.Append(", Greyscale=").Append(preprocessing.TrainAsGreyscale);
                sb.Append(", TrainOnlyFeatures=").Append(preprocessing.TrainOnlyFeatures);
            }

            if (stopping != null)
            {
                sb.Append(", MaxIterationCount=").Append(stopping.MaxIterationCount);
                sb.Append(", IterationWithoutImprovement=").Append(stopping.IterationWithoutImprovement);
                sb.Append(", MaxTrainingTime=").Append(stopping.MaxTrainingTime);
                sb.Append(", MinValidationError=").Append(FormatDouble(stopping.MinValidationError));
            }

            return sb.ToString();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.#####E+0", CultureInfo.InvariantCulture);
        }

        public static TrainingOptimizationContext Build_old(ISegmentationModel model, DeepLearningSettings settings, SegmentationModelConfig modelConfig, int batchSize)
        {
            const double learningRate = 0.05;
            //const int maxEpochs = 200;

            var optimizer = SGD(model.AsModule().parameters(), learningRate, 0.9, 0, learningRate); // lr: 0.05
            //var optimizer = AdamW(model.AsModule().parameters(), learningRate); // lr: 0.001
            //var optimizer = Adam(model.AsModule().parameters(), learningRate); // lr: 0.001

            var scheduler = StepLR(optimizer, 15, 0.75); // lr: 0.05
            //var scheduler = StepLR(optimizer, Convert.ToInt32(0.8 * maxEpochs), 0.6);
            //var scheduler = ReduceLROnPlateau(optimizer, "min", 0.5, 5, verbose: true);
            //var scheduler = OneCycleLR(optimizer, 1e-3, 5, 90, anneal_strategy: impl.OneCycleLR.AnnealStrategy.Cos);
            //var scheduler = CosineAnnealingLR(optimizer, 25);

            return new TrainingOptimizationContext
            {
                Optimizer = optimizer,
                Scheduler = scheduler,
                SchedulerRequiresMetric = false,
                InitialLearningRate = 0,
                WeightDecay = 0,
                MinLearningRate = null,
                OptimizerName = "SGD",
                SchedulerName = "StepLR",
                GradientClipNorm = 0,
                PolicySummary = BuildPolicySummary(
                    "AdamW",
                    "StepLR",
                    0.05,
                    0,
                    0,
                    0,
                    batchSize,
                    modelConfig,
                    settings.PreprocessingSettings,
                    settings.TrainingStoppingSettings),
                LegacyMode = true
            };
        }





    }
}