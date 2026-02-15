using AnnotationTool.Ai.Inference.Decoders;
using AnnotationTool.Core.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace AnnotationTool.InferenceSdk
{
    public static class Utils
    {
        public static Mat BitmapToMat(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                bmp = bmp.Clone(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    PixelFormat.Format24bppRgb);
            }

            return BitmapConverter.ToMat(bmp);
        }

        public static Bitmap MatToBitmap(Mat mat)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));

            if (mat.Empty())
                throw new ArgumentException("Mat is empty", nameof(mat));

            // Handle float probability maps [0..1]
            if (mat.Type() == MatType.CV_32FC1)
            {
                using (var tmp = new Mat())
                {
                    mat.ConvertTo(tmp, MatType.CV_8UC1, 255.0);
                    return BitmapConverter.ToBitmap(tmp);
                }
            }

            // Handle byte grayscale
            if (mat.Type() == MatType.CV_8UC1)
            {
                return BitmapConverter.ToBitmap(mat);
            }

            // Handle BGR color
            if (mat.Type() == MatType.CV_8UC3)
            {
                return BitmapConverter.ToBitmap(mat);
            }

            throw new NotSupportedException(
                $"Unsupported Mat type: {mat.Type()}");
        }

        public static ISegmentationDecoder CreateDecoder(SegmentationMode mode, int numClasses)
        {
            if (mode == SegmentationMode.Binary)
            {
                return new BinarySegmentationDecoder();
            }
            return new MulticlassSegmentationDecoder(numClasses); // Background allready included
        }

        public static Rect ValidateOrDefaultRoi(Mat image, Rect? roi)
        {
            if (!roi.HasValue)
                return new Rect(0, 0, image.Width, image.Height);

            var r = roi.Value;

            if (r.Width <= 0 || r.Height <= 0)
                throw new ArgumentException("ROI must have positive width and height.", nameof(roi));

            if (r.X < 0 || r.Y < 0 ||
                r.Right > image.Width ||
                r.Bottom > image.Height)
                throw new ArgumentException("ROI is outside image bounds.", nameof(roi));

            return r;
        }

        public static Rect? ConvertRoi(Rectangle? roi)
        {
            if (!roi.HasValue)
                return null;

            Rectangle r = roi.Value;
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        public static Mat ImageBufferToMat(CogImageBuffer img)
        {
            if (img.Data == IntPtr.Zero)
                throw new ArgumentException("ImageBuffer.Data is null");

            switch (img.PixelFormat)
            {
                case ImagePixelFormat.Gray8:
                    return Mat.FromPixelData(
                        img.Height,
                        img.Width,
                        MatType.CV_8UC1,
                        img.Data,
                        img.StrideBytes);

                case ImagePixelFormat.Bgr24:
                    return Mat.FromPixelData(
                        img.Height,
                        img.Width,
                        MatType.CV_8UC3,
                        img.Data,
                        img.StrideBytes);

                case ImagePixelFormat.Rgb24:
                    {
                        // Wrap RGB buffer (no copy yet)
                        using (var rgb = Mat.FromPixelData(
                            img.Height,
                            img.Width,
                            MatType.CV_8UC3,
                            img.Data,
                            img.StrideBytes))
                        {
                            // Convert to OpenCV-native BGR (this allocates)
                            var bgr = new Mat();
                            Cv2.CvtColor(rgb, bgr, ColorConversionCodes.RGB2BGR);
                            return bgr;
                        }
                    }

                default:
                    throw new NotSupportedException(
                        $"Unsupported ImagePixelFormat: {img.PixelFormat}");
            }
        }
    }
}