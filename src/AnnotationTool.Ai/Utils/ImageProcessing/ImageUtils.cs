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
            int x1 = roiTopLeftX;
            int y1 = roiTopLeftY;
            int x2 = roiTopLeftX + roiWidth;
            int y2 = roiTopLeftY + roiHeight;

            // Calculate padding (only positive values)
            int padLeft = Math.Max(0, -x1);
            int padTop = Math.Max(0, -y1);
            int padRight = Math.Max(0, x2 - input.Cols);
            int padBottom = Math.Max(0, y2 - input.Rows);

            // If no padding needed, return simple sub-ROI
            if (padLeft == 0 && padTop == 0 && padRight == 0 && padBottom == 0)
            {
                return input.SubMat(y1, y1 + roiHeight, x1, x1 + roiWidth);
            }

            // Create padded image
            Mat padded = new Mat();
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
            int newX1 = x1 + padLeft;
            int newY1 = y1 + padTop;

            Mat roi = padded.SubMat(newY1, newY1 + roiHeight, newX1, newX1 + roiWidth);

            padded.Dispose();
            return roi;
        }

        // ToDo: Test alternative implementation
        public static Mat GetPaddedRoi2(Mat input, int x, int y, int w, int h, Scalar paddingColor)
        {
            int padLeft = x < 0 ? -x : 0;
            int padTop = y < 0 ? -y : 0;
            int padRight = (x + w > input.Cols) ? (x + w - input.Cols) : 0;
            int padBottom = (y + h > input.Rows) ? (y + h - input.Rows) : 0;

            if (padLeft == 0 && padTop == 0 && padRight == 0 && padBottom == 0)
                return input.SubMat(y, y + h, x, x + w);

            using (Mat padded = new Mat())
            {
                Cv2.CopyMakeBorder(input, padded, padTop, padBottom, padLeft, padRight,
                                   BorderTypes.Constant, paddingColor);

                return padded.SubMat(y + padTop, y + padTop + h,
                                     x + padLeft, x + padLeft + w);
            }
        }

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

            int h = threshMask.Rows;
            int w = threshMask.Cols;

            var imgHeatRgb = new Mat(h, w, MatType.CV_8UC3);

            byte* pMask = (byte*)threshMask.DataPointer;
            byte* pHeat = (byte*)heatmap.DataPointer;
            byte* pOut = (byte*)imgHeatRgb.DataPointer;

            int maskStride = (int)threshMask.Step();
            int heatStride = (int)heatmap.Step();
            int outStride = (int)imgHeatRgb.Step();

            for (int y = 0; y < h; y++)
            {
                byte* rowMask = pMask + y * maskStride;
                byte* rowHeat = pHeat + y * heatStride;
                byte* rowOut = pOut + y * outStride;

                for (int x = 0; x < w; x++)
                {
                    byte maskVal = rowMask[x];

                    if (maskVal > 0)
                    {
                        // Copy heatmap B,G,R into output
                        byte* pxHeat = rowHeat + x * 3;
                        byte* pxOut = rowOut + x * 3;

                        pxOut[0] = pxHeat[0]; // B
                        pxOut[1] = pxHeat[1]; // G
                        pxOut[2] = pxHeat[2]; // R
                    }
                    else
                    {
                        // Black pixel
                        byte* pxOut = rowOut + x * 3;
                        pxOut[0] = pxOut[1] = pxOut[2] = 0;
                    }
                }
            }

            // Blend heatmap and image
            Mat superImposed = new Mat();
            Cv2.AddWeighted(imgHeatRgb, 1.0, image, 1.0, 0.0, superImposed);

            // Cleanup temporaries
            threshMask.Dispose();
            imgHeatRgb.Dispose();
            image.Dispose();

            return (superImposed, heatmap);
        }

        public static Mat[] SliceImage(Mat image, int roiSize, bool withBorderPadding, int nImagesRow, int nImagesColumn)
        {
            Mat[] result = new Mat[nImagesRow * nImagesColumn];

            int index = 0;

            for (int py = 0; py < nImagesRow; py++)
            {
                int y = py * roiSize;

                for (int px = 0; px < nImagesColumn; px++)
                {
                    int x = px * roiSize;

                    if (withBorderPadding)
                    {
                        result[index] = GetPaddedRoi(image, x, y, roiSize, roiSize, Scalar.Black);
                    }
                    else
                    {
                        // Normal ROI without padding
                        // Clip coordinates to avoid exceptions
                        int x2 = Math.Min(x + roiSize, image.Cols);
                        int y2 = Math.Min(y + roiSize, image.Rows);
                        int w = x2 - x;
                        int h = y2 - y;

                        result[index] = image.SubMat(y, y + h, x, x + w);
                    }

                    index++;
                }
            }

            return result;
        }

        /// <summary>
        /// Downsamples an image by a factor of (2^n).
        /// </summary>
        public static Mat DownSampleImage(Mat image, int nDownSampling)
        {
            if (nDownSampling < 0)
                throw new ArgumentException("Downsampling step count must be non-negative.");

            if (nDownSampling == 0)
                return image.Clone();

            Mat current = image.Clone();

            for (int i = 0; i < nDownSampling; i++)
            {
                Mat next = new Mat();
                Cv2.Resize(current, next, new Size(0, 0), 0.5, 0.5, InterpolationFlags.Nearest);
                current.Dispose();
                current = next;
            }
            return current;
        }

        /// <summary>
        /// Upsamples image by a factor of (2^n).
        /// </summary>
        public static Mat UpSampleImage(Mat image, int nUpSampling)
        {
            if (nUpSampling < 0)
                throw new ArgumentException("Up-sampling step count must be non-negative.");

            if (nUpSampling == 0)
                return image.Clone();

            Mat current = image.Clone();

            for (int i = 0; i < nUpSampling; i++)
            {
                Mat next = new Mat();
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
        public static (int rows, int cols) GetAmtImages(
            int imageHeight,
            int imageWidth,
            int sliceSize,
            bool withBorderPadding)
        {
            if (sliceSize <= 0)
                throw new ArgumentException("Slice size must be > 0.", nameof(sliceSize));

            if (withBorderPadding)
            {
                // Round UP → include partial slices
                int rows = (imageHeight + sliceSize - 1) / sliceSize;
                int cols = (imageWidth + sliceSize - 1) / sliceSize;
                return (rows, cols);
            }
            else
            {
                // Round DOWN → only full slices
                int rows = imageHeight / sliceSize;
                int cols = imageWidth / sliceSize;
                return (rows, cols);
            }
        }

        public static void CopyMatWithClipping(Mat smallImage, Mat largeImage, int x, int y)
        {
            // Calculate the intersection between the small image bounds and large image bounds
            int srcX = Math.Max(0, -x);
            int srcY = Math.Max(0, -y);
            int dstX = Math.Max(0, x);
            int dstY = Math.Max(0, y);

            int copyWidth = Math.Min(smallImage.Width - srcX, largeImage.Width - dstX);
            int copyHeight = Math.Min(smallImage.Height - srcY, largeImage.Height - dstY);

            // Check if there's any valid region to copy
            if (copyWidth > 0 && copyHeight > 0)
            {
                // Define the source ROI (part of small image to copy)
                Rect srcRoi = new Rect(srcX, srcY, copyWidth, copyHeight);

                // Define the destination ROI (where to place it in large image)
                Rect dstRoi = new Rect(dstX, dstY, copyWidth, copyHeight);

                // Copy the valid region
                smallImage[srcRoi].CopyTo(largeImage[dstRoi]);
            }
        }

        public static void DisposeTiles(Mat[] tiles)
        {
            if (tiles == null)
                return;

            for (int i = 0; i < tiles.Length; i++)
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
                catch
                {


                }

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
                catch
                {


                }

            }

            matsDic.Clear();
        }

    }
}
