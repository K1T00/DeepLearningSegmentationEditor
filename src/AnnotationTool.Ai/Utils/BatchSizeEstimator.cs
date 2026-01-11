using AnnotationTool.Ai.Models;
using AnnotationTool.Core.Models;
using System;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Utils
{
    internal class BatchSizeEstimator
    {
        public enum OptimizerType
        {
            SGD,
            Adam,
            AdamW
        }

        public static int EstimateBatchSize(
            SegmentationModelConfig cfg,
            int inputWidth,
            int inputHeight,
            int numChannels,
            long availableMemoryBytes,
            ComputeDevice device,
            OptimizerType optimizer,
            double targetUtilization = 0.8)
        {
            if (availableMemoryBytes <= 0)
                throw new ArgumentException("Available memory must be > 0");

            var bytesPerElement = GetBytesPerElement(cfg.TrainPrecision);

            // Estimate activation memory per sample
            var activationElements = EstimateActivationElements(cfg, inputWidth, inputHeight, numChannels);

            var backpropMultiplier = GetBackpropMultiplier(device, cfg.TrainPrecision, availableMemoryBytes);

            var activationBytesPerSample = activationElements * bytesPerElement * backpropMultiplier;

            // Estimate parameter + optimizer overhead
            var parameterBytes = EstimateParameterBytes(cfg, bytesPerElement);

            var optimizerBytes = EstimateOptimizerBytes(parameterBytes, optimizer);

            // Compute usable memory
            var utilization = ClampUtilization(targetUtilization, device, cfg.TrainPrecision, availableMemoryBytes);

            var usableBytes = availableMemoryBytes * utilization - optimizerBytes;

            double workspaceReserveBytes;

            if (device == ComputeDevice.Gpu)
            {
                var vramGB = availableMemoryBytes / (1024.0 * 1024 * 1024);

                // allocator + fragmentation tax
                usableBytes -= 100e6;

                if (cfg.TrainPrecision == ScalarType.Float32)
                    workspaceReserveBytes = vramGB <= 8 ? 600e6 : 400e6;
                else
                    workspaceReserveBytes = vramGB <= 8 ? 400e6 : 250e6;
            }
            else
            {
                workspaceReserveBytes = 0;
            }

            usableBytes -= workspaceReserveBytes;

            if (usableBytes <= 0)
                return 1;

            // Final batch size
            var batch = (int)Math.Floor(usableBytes / activationBytesPerSample);

            return Math.Max(batch, 1);
        }

        /// <summary>
        /// Optional refinement step: run one dry forward/backward pass
        /// </summary>
        public static int RefineByDryRun(Func<int, bool> tryRunBatch, int initialBatch)
        {
            var batch = initialBatch;

            while (batch > 1)
            {
                if (tryRunBatch(batch))
                    return batch;

                batch /= 2;
            }
            return 1;
        }

        private static double EstimateActivationElements(SegmentationModelConfig cfg, int width, int height, int inChannels)
        {
            var depth = cfg.Depth;
            var filters = cfg.FirstFilter;

            double sum = 0;
            var curW = width;
            var curH = height;
            var curC = inChannels;

            // ---------- Encoder ----------
            for (var d = 0; d < depth; d++)
            {
                var outC = filters << d;

                // conv blocks
                sum += curW * curH * outC * 2;

                // skip connection (stored)
                sum += curW * curH * outC;

                // attention (optional)
                if (cfg.UseChannelAttention || cfg.UseSelfAttention)
                    sum += curW * curH * outC * 0.25;

                // downsample
                curW /= 2;
                curH /= 2;
                curC = outC;
            }

            // ---------- Bottleneck ----------
            sum += curW * curH * curC * 2;

            // ---------- Decoder ----------
            for (var d = depth - 1; d >= 0; d--)
            {
                var outC = filters << d;

                // upsample
                sum += curW * curH * curC;

                // concat (discounted)
                sum += curW * curH * (curC + outC) * 0.65;

                // conv blocks
                sum += curW * curH * outC * 2;

                curW *= 2;
                curH *= 2;
                curC = outC;
            }

            return sum;
        }

        private static double EstimateParameterBytes(SegmentationModelConfig cfg, double bytesPerElement)
        {
            // Approximate UNet parameters
            var depth = cfg.Depth;
            var filters = cfg.FirstFilter;

            double paramCount = 0;
            var inC = 1;

            for (var d = 0; d < depth; d++)
            {
                int outC = filters << d;
                paramCount += 9 * inC * outC * 2; // convs
                inC = outC;
            }

            // bottleneck
            paramCount += 9 * inC * inC * 2;

            for (var d = depth - 1; d >= 0; d--)
            {
                var outC = filters << d;
                paramCount += 9 * (inC + outC) * outC * 2;
                inC = outC;
            }
            return paramCount * bytesPerElement;
        }

        private static double EstimateOptimizerBytes(double parameterBytes, OptimizerType optimizer)
        {
            switch (optimizer)
            {
                case OptimizerType.Adam:
                case OptimizerType.AdamW:
                    return parameterBytes * 2; // m + v
                default:
                    return 0;
            }
        }

        private static double GetBackpropMultiplier(ComputeDevice device, ScalarType precision, long availableBytes)
        {
            if (device != ComputeDevice.Gpu)
                return 2.2;

            var vramGB = availableBytes / (1024.0 * 1024 * 1024);

            if (precision == ScalarType.Float16 || precision == ScalarType.BFloat16)
            {
                return vramGB <= 8 ? 3.0 : 2.4;
            }
            return vramGB <= 8 ? 3.8 : 3.0;
        }

        private static double GetBytesPerElement(ScalarType precision)
        {
            switch (precision)
            {
                case ScalarType.Float16:
                case ScalarType.BFloat16:
                    return 2;
                case ScalarType.Float64:
                    return 8;
                default:
                    return 4;
            }
        }

        private static double ClampUtilization(double target, ComputeDevice device, ScalarType precision, long availableBytes)
        {
            if (device != ComputeDevice.Gpu)
                return Math.Min(target, 0.80);

            var vramGB = availableBytes / (1024.0 * 1024 * 1024);

            double max;

            if (vramGB <= 8)
                max = 0.80;
            else if (vramGB <= 12)
                max = 0.85;
            else
                max = 0.90;

            if (precision != ScalarType.Float32)
                max += 0.03;

            return Math.Min(target, max);
        }

    }
}
