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
            int h = probs[0].Rows;
            int w = probs[0].Cols;
            int classCount = probs.Count;

            var classMap = new Mat(h, w, MatType.CV_8UC1, Scalar.Black);

            byte*[] probPtrs = new byte*[classCount];
            int[] strides = new int[classCount];

            for (int c = 0; c < classCount; c++)
            {
                if (probs[c].Type() != MatType.CV_8UC1)
                    throw new ArgumentException("Probability maps must be CV_8UC1.");

                if (probs[c].Rows != h || probs[c].Cols != w)
                    throw new ArgumentException("Probability map size mismatch.");

                probPtrs[c] = (byte*)probs[c].DataPointer;
                strides[c] = (int)probs[c].Step();
            }

            byte* dstBase = (byte*)classMap.DataPointer;
            int dstStride = (int)classMap.Step();

            for (int y = 0; y < h; y++)
            {
                byte* dstRow = dstBase + y * dstStride;

                for (int x = 0; x < w; x++)
                {
                    byte bestProb = 0;
                    byte bestClass = 0;

                    for (int c = 0; c < classCount; c++)
                    {
                        byte p = *(probPtrs[c] + y * strides[c] + x);
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

        public static SegmentationStats ComputeMulticlassMetrics(Mat predictedClassMap, Mat groundTruthClassMap, int numClasses)
        {
            var stats = new SegmentationStats();

            long[] tp = new long[numClasses];
            long[] fp = new long[numClasses];
            long[] fn = new long[numClasses];

            int h = predictedClassMap.Rows;
            int w = predictedClassMap.Cols;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte pred = predictedClassMap.At<byte>(y, x);
                    byte gt = groundTruthClassMap.At<byte>(y, x);

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

            double sumIoU = 0.0;
            double sumDice = 0.0;
            int validClassCount = 0;

            // Skip background (class 0) in reported averages
            for (int c = 1; c < numClasses; c++)
            {
                long tpc = tp[c];
                long fpc = fp[c];
                long fnc = fn[c];

                long denomIoU = tpc + fpc + fnc;
                if (denomIoU == 0)
                    continue; // class absent in both pred & gt

                double iou = (double)tpc / denomIoU;
                double dice = (2.0 * tpc) / (2.0 * tpc + fpc + fnc);

                sumIoU += iou;
                sumDice += dice;
                validClassCount++;
            }

            if (validClassCount > 0)
            {
                stats.IoU = sumIoU / validClassCount;
                stats.Dice = sumDice / validClassCount;
            }
            else
            {
                stats.IoU = 0.0;
                stats.Dice = 0.0;
            }

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
                    int h = img.Rows;
                    int w = img.Cols;

                    byte* data = (byte*)img.DataPointer;
                    int stride = (int)img.Step();

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = data + y * stride;

                        for (int x = 0; x < w; x++)
                        {
                            byte* px = row + x * 3;

                            float b = px[0] * inv255;
                            float g = px[1] * inv255;
                            float r = px[2] * inv255;

                            sumR += r; sumG += g; sumB += b;
                            sqR += r * r; sqG += g * g; sqB += b * b;

                            count++;
                        }
                    }
                }
            }

            float meanR = (float)(sumR / count);
            float meanG = (float)(sumG / count);
            float meanB = (float)(sumB / count);

            float stdR = (float)Math.Sqrt(sqR / count - meanR * meanR);
            float stdG = (float)Math.Sqrt(sqG / count - meanG * meanG);
            float stdB = (float)Math.Sqrt(sqB / count - meanB * meanB);

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
                    int h = img.Rows;
                    int w = img.Cols;

                    byte* data = (byte*)img.DataPointer;
                    int stride = (int)img.Step();

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = data + y * stride;

                        for (int x = 0; x < w; x++)
                        {
                            float v = row[x] * inv255;
                            sum += v;
                            sq += v * v;
                            count++;
                        }
                    }
                }
            }

            float mean = (float)(sum / count);
            float std = (float)Math.Sqrt(sq / count - mean * mean);
            if (std < 1e-6f) std = 1e-6f;

            return new NormalizationSettings
            {
                Mean = new[] { mean },
                Std = new[] { std }
            };
        }



    }
}
