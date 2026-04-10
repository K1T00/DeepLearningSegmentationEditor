using OpenCvSharp;
using System;

namespace AnnotationTool.Ai.Utils.ImageProcessing
{
    public static class ImageAnalysis
    {
        // Even faster than IsBlobInImage for just checking presence of non-zero pixels
        public static unsafe bool IsPixelValueInImage(Mat image)
        {
            if (image.Type() != MatType.CV_8UC1)
                throw new ArgumentException("Mask must be single-channel CV_8UC1");

            var h = image.Rows;
            var w = image.Cols;

            var ptr = image.DataPointer;
            var stride = (int)image.Step();

            for (var y = 0; y < h; y++)
            {
                var row = ptr + y * stride;

                for (var x = 0; x < w; x++)
                {
                    if (row[x] != 0)
                        return true;   // found blob pixel
                }
            }

            return false;
        }

        //Faster than SimpleBlobDetector for binary masks
        public static bool IsBlobInImage(Mat image)
        {
            if (image.Type() != MatType.CV_8UC1)
                throw new ArgumentException("Mask must be single-channel CV_8UC1");

            var labels = new Mat();
            var count = Cv2.ConnectedComponents(image, labels);

            return count > 1; // label 0 = background, label >=1 = blobs
        }

    }
}