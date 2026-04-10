using AnnotationTool.Core.Models;
using System;
using TorchSharp;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Utils
{
    /// <summary>
    /// Mat image and Tensor related computations
    /// </summary>
    public static class AiUtils
    {

        public static Device ResolveDevice(DeepLearningSettings settings)
        {
            switch (settings.TrainModelSettings.Device)
            {
                case ComputeDevice.Cpu:
                    return new Device(DeviceType.CPU);
                case ComputeDevice.Gpu:
                    return new Device(DeviceType.CUDA);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int AdjustBatchSizeIfNecessary(int estimatedBatchSize, int datasetSize, int minBatchSize)
        {
            if (estimatedBatchSize <= 0)
                throw new ArgumentException("Estimated batch size must be > 0");

            if (datasetSize <= 0)
                throw new ArgumentException("Dataset size must be > 0");

            // Never exceed dataset size
            var batchSize = Math.Min(estimatedBatchSize, datasetSize);

            // If everything fits in one batch, that's optimal
            if (batchSize == datasetSize)
                return batchSize;

            // If the remaining batch would be too small, rebalance
            var remainder = datasetSize % batchSize;

            if (remainder > 0 && remainder < minBatchSize)
            {
                int balanced = datasetSize / 2;

                // Make sure the new batch size still respects minBatchSize
                if (balanced >= minBatchSize)
                    return balanced;

                // Fallback: keep original batch size
                // (better one small batch than destabilizing everything)
            }
            return batchSize;
        }

    }
}