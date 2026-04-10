using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace AnnotationTool.Ai.Utils.ImageProcessing
{
    public static class ImageUtils
    {
        // Extracts an ROI from an image and pads the borders if the ROI lies partially outside the image.
        public static Mat GetPaddedRoi(Mat input, int roiTopLeftX, int roiTopLeftY, int roiWidth, int roiHeight, Scalar paddingColor)
        {
            var x1 = roiTopLeftX;
            var y1 = roiTopLeftY;
            var x2 = roiTopLeftX + roiWidth;
            var y2 = roiTopLeftY + roiHeight;

            // Calculate padding (only positive values)
            var padLeft = Math.Max(0, -x1);
            var padTop = Math.Max(0, -y1);
            var padRight = Math.Max(0, x2 - input.Cols);
            var padBottom = Math.Max(0, y2 - input.Rows);

            // If no padding needed, return simple sub-ROI
            if (padLeft == 0 && padTop == 0 && padRight == 0 && padBottom == 0)
            {
                return input.SubMat(y1, y1 + roiHeight, x1, x1 + roiWidth);
            }

            // Create padded image
            var padded = new Mat();
            Cv2.CopyMakeBorder(
                input,
                padded,
                padTop,
                padBottom,
                padLeft,
                padRight,
                BorderTypes.Constant,
                paddingColor);

            // Adjust ROI coordinates relative to padded image
            var newX1 = x1 + padLeft;
            var newY1 = y1 + padTop;

            var roi = padded.SubMat(newY1, newY1 + roiHeight, newX1, newX1 + roiWidth);

            padded.Dispose();
            return roi;
        }

        // ToDo: Test alternative implementation
        public static Mat GetPaddedRoi2(Mat input, int x, int y, int w, int h, Scalar paddingColor)
        {
            var padLeft = x < 0 ? -x : 0;
            var padTop = y < 0 ? -y : 0;
            var padRight = (x + w > input.Cols) ? (x + w - input.Cols) : 0;
            var padBottom = (y + h > input.Rows) ? (y + h - input.Rows) : 0;

            if (padLeft == 0 && padTop == 0 && padRight == 0 && padBottom == 0)
                return input.SubMat(y, y + h, x, x + w);

            using (var padded = new Mat())
            {
                Cv2.CopyMakeBorder(input, padded, padTop, padBottom, padLeft, padRight,
                                   BorderTypes.Constant, paddingColor);

                return padded.SubMat(y + padTop, y + padTop + h,
                                     x + padLeft, x + padLeft + w);
            }
        }

        // Left here because might need it later for debugging/visualization, but not currently used in training/inference.
        public static unsafe (Mat superImposed, Mat heatmap) ImageToHeatmap(Mat img, Mat mask, int threshold)
        {
            // Convert grayscale to RGB if needed
            var image = new Mat();
            if (img.Type() == MatType.CV_8UC1)
                Cv2.CvtColor(img, image, ColorConversionCodes.GRAY2RGB);
            else
                img.CopyTo(image);

            // Threshold mask: pixels > threshold remain, else 0
            // Create thresholded copy of mask (NON-MUTATING)
            var threshMask = new Mat();
            Cv2.Threshold(mask, threshMask, threshold, 255, ThresholdTypes.Tozero);

            // Create color heatmap from mask
            var heatmap = new Mat();
            Cv2.ApplyColorMap(threshMask, heatmap, ColormapTypes.Turbo);

            var h = threshMask.Rows;
            var w = threshMask.Cols;

            var imgHeatRgb = new Mat(h, w, MatType.CV_8UC3);

            var pMask = threshMask.DataPointer;
            var pHeat = heatmap.DataPointer;
            var pOut = imgHeatRgb.DataPointer;

            var maskStride = (int)threshMask.Step();
            var heatStride = (int)heatmap.Step();
            var outStride = (int)imgHeatRgb.Step();

            for (var y = 0; y < h; y++)
            {
                var rowMask = pMask + y * maskStride;
                var rowHeat = pHeat + y * heatStride;
                var rowOut = pOut + y * outStride;

                for (var x = 0; x < w; x++)
                {
                    var maskVal = rowMask[x];

                    if (maskVal > 0)
                    {
                        // Copy heatmap B,G,R into output
                        var pxHeat = rowHeat + x * 3;
                        var pxOut = rowOut + x * 3;

                        pxOut[0] = pxHeat[0]; // B
                        pxOut[1] = pxHeat[1]; // G
                        pxOut[2] = pxHeat[2]; // R
                    }
                    else
                    {
                        // Black pixel
                        var pxOut = rowOut + x * 3;
                        pxOut[0] = pxOut[1] = pxOut[2] = 0;
                    }
                }
            }

            // Blend heatmap and image
            var superImposed = new Mat();
            Cv2.AddWeighted(imgHeatRgb, 1.0, image, 1.0, 0.0, superImposed);

            // Cleanup temporaries
            threshMask.Dispose();
            imgHeatRgb.Dispose();
            image.Dispose();

            return (superImposed, heatmap);
        }

        public static Mat[] SliceImage(Mat image, int roiSize, bool withBorderPadding, int nImagesRow, int nImagesColumn)
        {
            var result = new Mat[nImagesRow * nImagesColumn];

            var index = 0;

            for (var py = 0; py < nImagesRow; py++)
            {
                var y = py * roiSize;

                for (var px = 0; px < nImagesColumn; px++)
                {
                    var x = px * roiSize;

                    if (withBorderPadding)
                    {
                        result[index] = GetPaddedRoi(image, x, y, roiSize, roiSize, Scalar.Black);
                    }
                    else
                    {
                        // Normal ROI without padding
                        // Clip coordinates to avoid exceptions
                        var x2 = Math.Min(x + roiSize, image.Cols);
                        var y2 = Math.Min(y + roiSize, image.Rows);
                        var w = x2 - x;
                        var h = y2 - y;

                        //result[index] = image.SubMat(y, y + h, x, x + w);
                        result[index] = image.SubMat(y, y + h, x, x + w).Clone();
                    }

                    index++;
                }
            }

            return result;
        }

        /// <summary>
        /// DownSamples an image by a factor of (2^n).
        /// </summary>
        public static Mat DownSampleImage(Mat image, int nDownSampling)
        {
            if (nDownSampling < 0)
                throw new ArgumentException("Downsampling step count must be non-negative.");

            if (nDownSampling == 0)
                return image.Clone();

            var current = image.Clone();

            for (var i = 0; i < nDownSampling; i++)
            {
                var next = new Mat();
                Cv2.Resize(current, next, new Size(0, 0), 0.5, 0.5, InterpolationFlags.Nearest);
                current.Dispose();
                current = next;
            }
            return current;
        }

        /// <summary>
        /// UpSamples image by a factor of (2^n).
        /// </summary>
        public static Mat UpSampleImage(Mat image, int nUpSampling)
        {
            if (nUpSampling < 0)
                throw new ArgumentException("Up-sampling step count must be non-negative.");

            if (nUpSampling == 0)
                return image.Clone();

            var current = image.Clone();

            for (var i = 0; i < nUpSampling; i++)
            {
                var next = new Mat();
                Cv2.Resize(
                    current,
                    next,
                    new Size(0, 0),
                    2.0,       // scaleX
                    2.0,       // scaleY
                    InterpolationFlags.Nearest);

                current.Dispose(); // dispose prev step
                current = next;
            }

            return current;
        }

        public static Mat Binarize(Mat src)
        {
            if (src.Empty())
                return Mat.Zeros(new Size(1, 1), MatType.CV_8UC1);

            double minVal, maxVal;
            Cv2.MinMaxLoc(src, out minVal, out maxVal);

            var dst = new Mat(src.Size(), MatType.CV_8UC1);

            if (maxVal <= 0)
            {
                dst.SetTo(0);
                return dst;
            }

            var thr = (minVal + maxVal) * 0.5;
            Cv2.Threshold(src, dst, thr, 1.0, ThresholdTypes.Binary);
            return dst;
        }

        /// <summary>
        /// Calculates how many slices of size <paramref name="sliceSize"/> fit into an image.
        /// </summary>
        public static (int rows, int cols) GetAmtImages(int imageHeight, int imageWidth, int sliceSize, bool withBorderPadding)
        {
            if (withBorderPadding)
            {
                // Round UP → include partial slices
                var rows = (imageHeight + sliceSize - 1) / sliceSize;
                var cols = (imageWidth + sliceSize - 1) / sliceSize;
                return (rows, cols);
            }
            else
            {
                // Round DOWN → only full slices
                var rows = imageHeight / sliceSize;
                var cols = imageWidth / sliceSize;
                return (rows, cols);
            }
        }

        public static void CopyMatWithClipping(Mat smallImage, Mat largeImage, int x, int y)
        {
            // Calculate the intersection between the small image bounds and large image bounds
            var srcX = Math.Max(0, -x);
            var srcY = Math.Max(0, -y);
            var dstX = Math.Max(0, x);
            var dstY = Math.Max(0, y);

            var copyWidth = Math.Min(smallImage.Width - srcX, largeImage.Width - dstX);
            var copyHeight = Math.Min(smallImage.Height - srcY, largeImage.Height - dstY);

            // Check if there's any valid region to copy
            if (copyWidth > 0 && copyHeight > 0)
            {
                // Define the source ROI (part of small image to copy)
                var srcRoi = new Rect(srcX, srcY, copyWidth, copyHeight);

                // Define the destination ROI (where to place it in large image)
                var dstRoi = new Rect(dstX, dstY, copyWidth, copyHeight);

                // Copy the valid region
                smallImage[srcRoi].CopyTo(largeImage[dstRoi]);
            }
        }

        public static void DisposeTiles(Mat[] tiles)
        {
            if (tiles == null)
                return;

            for (var i = 0; i < tiles.Length; i++)
            {
                try
                {
                    tiles[i]?.Dispose();
                }
                catch
                {
                    // Intentionally swallow:
                    // disposing should never crash training/inference
                }

                tiles[i] = null;
            }
        }

        public static void DisposePred(Dictionary<int, Mat> matDic)
        {
            foreach (var kv in matDic)
            {
                try
                {
                    kv.Value?.Dispose();
                }
                catch { }

            }
            matDic.Clear();
        }

        public static void DisposePreds(Dictionary<int, Mat[]> matsDic)
        {
            foreach (var kv in matsDic)
            {
                try
                {
                    DisposeTiles(kv.Value);
                }
                catch { }

            }
            matsDic.Clear();
        }
    }
}
