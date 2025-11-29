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

			int h = image.Rows;
			int w = image.Cols;

			byte* ptr = (byte*)image.DataPointer;
			int stride = (int)image.Step();

			for (int y = 0; y < h; y++)
			{
				byte* row = ptr + y * stride;

				for (int x = 0; x < w; x++)
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

			Mat labels = new Mat();
			int count = Cv2.ConnectedComponents(image, labels);

			return count > 1; // label 0 = background, label >=1 = blobs
		}

	}
}
