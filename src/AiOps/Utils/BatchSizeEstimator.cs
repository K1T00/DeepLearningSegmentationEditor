using AnnotationTool.Core.Models;
using System;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Utils
{
    /// <summary>
    /// WIP
    /// </summary>
    public static class BatchSizeEstimator
    {
        public static int EstimateBatchSize(
            int imageWidth,
            int imageHeight,
            int channels,
            long availableMemoryBytes,
            int unetDepth,
            int firstFilter,
            ComputeDevice computeDevice,
            ScalarType precision,
            bool keepSkipBuffers,
            bool usePooling,
            bool useStridedConv,
            bool useInterpolationDown,
            bool useInterpolationUp,
            bool useChannelAttention,
            bool useAttentionGates,
            bool useSelfAttention)
        {

            if (imageWidth <= 0 || imageHeight <= 0 || channels <= 0 || availableMemoryBytes <= 0)
                return 1;
            if (unetDepth < 1) unetDepth = 1;
            if (firstFilter < 4) firstFilter = 4;

            // Bytes per element (dtype)
            int bpe = 4;
            if (precision == ScalarType.Float16 || precision == ScalarType.BFloat16)
                bpe = 2;

            
            // Estimate convolutional activation memory
            double activations = SumActivationsUNet(
                imageHeight,
                imageWidth,
                unetDepth,
                firstFilter,
                keepSkipBuffers,
                usePooling,
                useStridedConv,
                useInterpolationDown,
                useInterpolationUp,
                useChannelAttention,
                useAttentionGates,
                useSelfAttention);

            // Include input + output + loss buffers
            double inputBytes = (double)imageHeight * imageWidth * channels * bpe;
            double outputBytes = (double)imageHeight * imageWidth * 1 * bpe; // masks
            double ioExtra = (inputBytes + outputBytes) * 1.2; // include loss + temp workspace

            // Backprop multiplier (hardware-adaptive)
            double bwdMultiplier = GetBackpropMultiplier(computeDevice);

            double bytesPerImage = (activations * bpe * bwdMultiplier) + ioExtra;


            // Safety margin for fragmentation & OS usage
            double safetyFraction = GetSafetyFraction(availableMemoryBytes, computeDevice);
            double usable = availableMemoryBytes * safetyFraction;

            // Estimate batch size
            int batch = (int)Math.Max(1.0, Math.Floor(usable / bytesPerImage));

            // Smoothing: prefer clean batch sizes
            if (batch >= 16)
                batch = (batch / 4) * 4;
            else if (batch >= 8)
                batch = (batch / 2) * 2;

            if (batch < 1)
                batch = 1;

            return batch;
        }

        /// <summary>
        /// Improved UNet activation estimator including attention overhead.
        /// </summary>
        private static double SumActivationsUNet(
            int h,
            int w,
            int depth,
            int firstFilter,
            bool keepSkipBuffers,
            bool usePooling,
            bool useStridedConv,
            bool useInterpolationDown,
            bool useInterpolationUp,
            bool useChannelAttention,
            bool useAttentionGates,
            bool useSelfAttention)
        {
            double sum = 0.0;
            int curH = h;
            int curW = w;

            const int convsPerBlock = 2;

            // Encoder
            for (int i = 0; i < depth; i++)
            {
                int outC = firstFilter << i;

                // DoubleConv
                sum += convsPerBlock * curH * curW * outC;

                if (useChannelAttention)
                    sum += curH * curW * outC * 1.2; // safer factor

                if (keepSkipBuffers)
                    sum += curH * curW * outC;

                if (useStridedConv)
                    sum += curH * curW * outC;

                // Downsampling resolution change
                if (useInterpolationDown)
                {
                    curH = Math.Max(1, (int)(curH * 0.5));
                    curW = Math.Max(1, (int)(curW * 0.5));
                }
                else
                {
                    curH = Math.Max(1, (curH + 1) / 2);
                    curW = Math.Max(1, (curW + 1) / 2);
                }
            }

            
            // Bottleneck
            int bottleneckC = firstFilter << depth;
            sum += convsPerBlock * curH * curW * bottleneckC;

            if (useSelfAttention)
                sum += curH * curW * bottleneckC * 0.7; // safe approx

            
            // Decoder
            for (int i = depth - 1; i >= 0; i--)
            {
                if (!useInterpolationUp)
                {
                    int upC = firstFilter << i;
                    sum += curH * curW * upC;
                }

                // Resolution doubles
                curH *= 2;
                curW *= 2;

                int outC = firstFilter << i;

                // Concat skip + upsampled
                sum += curH * curW * (2 * outC);

                // Decoder DoubleConv
                sum += convsPerBlock * curH * curW * outC;

                if (useAttentionGates)
                    sum += curH * curW * outC * 1.2;
            }

            return sum;
        }

        /// <summary>
        /// Hardware-dependent backprop multiplier.
        /// </summary>
        private static double GetBackpropMultiplier(ComputeDevice computeDevice)
        {
            return computeDevice == ComputeDevice.Gpu ?
                5 : // GPU → large workspace usage (3–5x)
                3; // CPU → lower workspace usage (1.5–3x)
        }

        /// <summary>
        /// Memory safety margin based on VRAM size.
        /// </summary>
        private static double GetSafetyFraction(long mem, ComputeDevice computeDevice)
        {
            var saveFraction = 0.5;

            switch (computeDevice)
            {
                case ComputeDevice.Cpu:

                    if (mem <= 4L * 1024 * 1024 * 1024) // <= 4GB
                        return 0.55;
                    if (mem <= 6L * 1024 * 1024 * 1024) // <= 6GB
                        return 0.60;
                    if (mem <= 12L * 1024 * 1024 * 1024) // <= 12GB
                        return 0.70;

                    break;
                case ComputeDevice.Gpu:

                    if (mem <= 4L * 1024 * 1024 * 1024) // <= 4GB
                        return 0.55;
                    if (mem <= 6L * 1024 * 1024 * 1024) // <= 6GB
                        return 0.60;
                    if (mem <= 12L * 1024 * 1024 * 1024) // <= 12GB
                        return 0.70;

                    break;
                default:
                    break;
            }



            return 0.90;
        }
    }

}
