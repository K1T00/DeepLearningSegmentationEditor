using OpenCvSharp;
using System;

namespace AnnotationTool.Ai.Utils.ImageProcessing
{
	public static class ImageComposition
	{

		// Images need to be sorted !!!
		public static Mat MergeImages(Mat[] images, int nRowImages, int nColumnImages)
		{
			int expected = nRowImages * nColumnImages;
			if (images.Length != expected)
				throw new ArgumentException("Amount of images does not match grid size.");

			// Validate all images have the same size & type
			var w = images[0].Cols;
			var h = images[0].Rows;
			var type = images[0].Type();

			for (int k = 1; k < images.Length; k++)
			{
				if (images[k].Cols != w || images[k].Rows != h || images[k].Type() != type)
					throw new ArgumentException("All images must have the same dimensions and type.");
			}

			// Build rows and concatenate horizontally
			Mat[] rows = new Mat[nRowImages];
			int index = 0;

			for (int r = 0; r < nRowImages; r++)
			{
				Mat[] rowImgs = new Mat[nColumnImages];

				for (int c = 0; c < nColumnImages; c++)
				{
					// Use Clone() if you don't want aliasing; otherwise images[index] is fine
					rowImgs[c] = images[index];
					index++;
				}

				rows[r] = new Mat();
				Cv2.HConcat(rowImgs, rows[r]);
			}

			// Concatenate vertically to form final grid
			Mat result = new Mat();
			Cv2.VConcat(rows, result);

			return result;
		}

	}
}
