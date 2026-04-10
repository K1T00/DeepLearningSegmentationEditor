using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;

namespace AnnotationTool.Ai.Utils
{
    public static class DatasetStatistics
    {
        public static SegmentationStats ComputeBinaryMetrics(Mat predicted, Mat groundTruth)
        {
            using (var predBin = Binarize(predicted))
            using (var gtBin = Binarize(groundTruth))
            using (var intersection = new Mat())
            using (var union = new Mat())
            using (var predOnly = new Mat())
            using (var missed = new Mat())
            {
                Cv2.BitwiseAnd(predBin, gtBin, intersection);
                Cv2.BitwiseOr(predBin, gtBin, union);

                double TP = Cv2.CountNonZero(intersection);

                Cv2.BitwiseAnd(predBin, ~gtBin, predOnly);
                double FP = Cv2.CountNonZero(predOnly);

                Cv2.BitwiseAnd(~predBin, gtBin, missed);
                double FN = Cv2.CountNonZero(missed);

                double total = predBin.Rows * predBin.Cols;
                var TN = total - TP - FP - FN;

                // Add small epsilon to denominators to avoid division by zero
                const double epsilon = 1e-8;

                return new SegmentationStats
                {
                    TP = (int)TP,
                    FP = (int)FP,
                    FN = (int)FN,
                    TN = (int)TN,

                    IoU = TP / (TP + FP + FN + epsilon),
                    Dice = (2 * TP) / (2 * TP + FP + FN + epsilon),
                    Precision = TP / (TP + FP + epsilon),
                    Recall = TP / (TP + FN + epsilon),
                    Accuracy = (TP + TN) / (TP + TN + FP + FN + epsilon),
                    FPR = FP / (FP + TN + epsilon),
                    Specificity = TN / (TN + FP + epsilon)
                };
            }
        }

        /// <summary>
        /// Builds a class-id map from per-class probability maps using argmax.
        /// </summary>
        public static unsafe Mat BuildClassMapUnsafe(IReadOnlyList<Mat> probs)
        {
            var h = probs[0].Rows;
            var w = probs[0].Cols;
            var classCount = probs.Count;

            var classMap = new Mat(h, w, MatType.CV_8UC1, Scalar.Black);

            var probPtrs = new byte*[classCount];
            var strides = new int[classCount];

            for (var c = 0; c < classCount; c++)
            {
                if (probs[c].Type() != MatType.CV_8UC1)
                    throw new ArgumentException("Probability maps must be CV_8UC1.");

                if (probs[c].Rows != h || probs[c].Cols != w)
                    throw new ArgumentException("Probability map size mismatch.");

                probPtrs[c] = probs[c].DataPointer;
                strides[c] = (int)probs[c].Step();
            }

            var dstBase = classMap.DataPointer;
            var dstStride = (int)classMap.Step();

            for (var y = 0; y < h; y++)
            {
                var dstRow = dstBase + y * dstStride;

                for (var x = 0; x < w; x++)
                {
                    byte bestProb = 0;
                    byte bestClass = 0;

                    for (var c = 0; c < classCount; c++)
                    {
                        var p = *(probPtrs[c] + y * strides[c] + x);
                        if (p > bestProb)
                        {
                            bestProb = p;
                            bestClass = (byte)(c + 1); // +1 → skip background
                        }
                    }

                    dstRow[x] = bestClass;
                }
            }

            return classMap;
        }

        public static unsafe SegmentationStats ComputeMulticlassMetrics(Mat predictedClassMap, Mat groundTruthClassMap, int numClasses)
        {
            var stats = new SegmentationStats();

            var tp = new long[numClasses];
            var fp = new long[numClasses];
            var fn = new long[numClasses];

            var h = predictedClassMap.Rows;
            var w = predictedClassMap.Cols;

            var predBase = predictedClassMap.DataPointer;
            var gtBase = groundTruthClassMap.DataPointer;
            var predStride = (int)predictedClassMap.Step();
            var gtStride = (int)groundTruthClassMap.Step();

            for (var y = 0; y < h; y++)
            {
                var predRow = predBase + y * predStride;
                var gtRow = gtBase + y * gtStride;

                for (var x = 0; x < w; x++)
                {
                    var pred = predRow[x];
                    var gt = gtRow[x];

                    if (pred >= numClasses || gt >= numClasses)
                        continue;

                    if (pred == gt)
                    {
                        tp[gt]++;
                    }
                    else
                    {
                        fp[pred]++;
                        fn[gt]++;
                    }
                }
            }

            var sumIoU = 0.0;
            var sumDice = 0.0;
            var validClassCount = 0;

            // Skip background (class 0) in reported averages
            for (var c = 1; c < numClasses; c++)
            {
                var tpc = tp[c];
                var fpc = fp[c];
                var fnc = fn[c];

                var denomIoU = tpc + fpc + fnc;
                if (denomIoU == 0)
                    continue;

                sumIoU += (double)tpc / denomIoU;
                sumDice += (2.0 * tpc) / (2.0 * tpc + fpc + fnc);
                validClassCount++;
            }

            stats.IoU = validClassCount > 0 ? sumIoU / validClassCount : 0.0;
            stats.Dice = validClassCount > 0 ? sumDice / validClassCount : 0.0;
            stats.Accuracy = tp.Sum() / (double)(h * w);

            return stats;
        }

        public static (SegmentationStats macro, SegmentationStats micro) AggregateResults(IList<SegmentationStats> results)
        {
            if (results == null || results.Count == 0)
                return (new SegmentationStats(), new SegmentationStats());

            const double Epsilon = 1e-8;

            // ----- MACRO (mean of per-image metrics) -----
            var macro = new SegmentationStats
            {
                Dice = results.Average(r => r.Dice),
                IoU = results.Average(r => r.IoU),
                Precision = results.Average(r => r.Precision),
                Recall = results.Average(r => r.Recall),
                Accuracy = results.Average(r => r.Accuracy),
                FPR = results.Average(r => r.FPR),
            };

            // ----- MICRO (aggregate all TP/FP/FN/TN) -----
            double TP = results.Sum(r => r.TP);
            double FP = results.Sum(r => r.FP);
            double FN = results.Sum(r => r.FN);
            double TN = results.Sum(r => r.TN);

            var micro = new SegmentationStats
            {
                TP = (int)TP,
                FP = (int)FP,
                FN = (int)FN,
                TN = (int)TN,

                IoU = TP / (TP + FP + FN + Epsilon),
                Dice = (2.0 * TP) / (2.0 * TP + FP + FN + Epsilon),
                Precision = TP / (TP + FP + Epsilon),
                Recall = TP / (TP + FN + Epsilon),
                Accuracy = (TP + TN) / (TP + TN + FP + FN + Epsilon),
                FPR = FP / (FP + TN + Epsilon)
            };

            return (macro, micro);
        }

        /// <summary>
        /// Computes mean/std for an RGB dataset (3-channel, CV_8UC3).
        /// Uses unsafe pointers for maximum speed.
        /// </summary>
        public static unsafe NormalizationSettings ComputeRgbStats(IEnumerable<string> imagePaths)
        {
            double sumR = 0, sumG = 0, sumB = 0;
            double sqR = 0, sqG = 0, sqB = 0;
            long count = 0;

            const float inv255 = 1f / 255f;

            foreach (var path in imagePaths)
            {
                using (var img = Cv2.ImRead(path, ImreadModes.Color))
                {
                    var h = img.Rows;
                    var w = img.Cols;
                    var data = img.DataPointer;
                    var stride = (int)img.Step();

                    for (var y = 0; y < h; y++)
                    {
                        var row = data + y * stride;

                        for (var x = 0; x < w; x++)
                        {
                            var px = row + x * 3;

                            var b = px[0] * inv255;
                            var g = px[1] * inv255;
                            var r = px[2] * inv255;

                            sumR += r;
                            sumG += g;
                            sumB += b;

                            sqR += r * r;
                            sqG += g * g;
                            sqB += b * b;

                            count++;
                        }
                    }
                }
            }

            var meanR = (float)(sumR / count);
            var meanG = (float)(sumG / count);
            var meanB = (float)(sumB / count);

            var stdR = (float)Math.Sqrt(sqR / count - meanR * meanR);
            var stdG = (float)Math.Sqrt(sqG / count - meanG * meanG);
            var stdB = (float)Math.Sqrt(sqB / count - meanB * meanB);

            // avoid division-by-zero during normalization
            if (stdR < 1e-6f) stdR = 1e-6f;
            if (stdG < 1e-6f) stdG = 1e-6f;
            if (stdB < 1e-6f) stdB = 1e-6f;

            return new NormalizationSettings
            {
                Mean = new[] { meanR, meanG, meanB },
                Std = new[] { stdR, stdG, stdB }
            };
        }

        /// <summary>
        /// Computes mean/std for a grayscale dataset (1-channel, CV_8UC1).
        /// Uses unsafe pointers for absolute maximum speed.
        /// </summary>
        public static unsafe NormalizationSettings ComputeGreyStats(IEnumerable<string> imagePaths)
        {
            double sum = 0;
            double sq = 0;
            long count = 0;

            const float inv255 = 1f / 255f;

            foreach (var path in imagePaths)
            {
                using (var img = Cv2.ImRead(path, ImreadModes.Grayscale))
                {
                    var h = img.Rows;
                    var w = img.Cols;
                    var data = img.DataPointer;
                    var stride = (int)img.Step();

                    for (var y = 0; y < h; y++)
                    {
                        var row = data + y * stride;

                        for (var x = 0; x < w; x++)
                        {
                            var v = row[x] * inv255;
                            sum += v;
                            sq += v * v;
                            count++;
                        }
                    }
                }
            }

            var mean = (float)(sum / count);
            var std = (float)Math.Sqrt(sq / count - mean * mean);
            if (std < 1e-6f) std = 1e-6f;

            return new NormalizationSettings
            {
                Mean = new[] { mean },
                Std = new[] { std }
            };
        }



    }
}
