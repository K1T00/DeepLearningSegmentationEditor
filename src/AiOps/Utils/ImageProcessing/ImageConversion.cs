using OpenCvSharp;
using System;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Utils.ImageProcessing
{
	public class ImageConversion
	{
		// Tensor of [H x W] expected
		// ToDo: SIMD optimization
		public static unsafe Mat TensorToGreyImage(Tensor tens)
		{
			if (tens.Dimensions != 2)
				throw new ArgumentException("Tensor must have shape [H, W]");

			int h = (int)tens.shape[0];
			int w = (int)tens.shape[1];
			int count = w * h;

			float[] src = tens.data<float>().ToArray();

			Mat img = new Mat(h, w, MatType.CV_8UC1);

			byte* dst = (byte*)img.DataPointer;
			int stride = (int)img.Step();

			fixed (float* pSrc = src)
			{
				float* p = pSrc;

				for (int y = 0; y < h; y++)
				{
					byte* row = dst + y * stride;

					for (int x = 0; x < w; x++)
					{
						float v = *p++;
						// clamp to avoid overflows
						if (v < 0f) v = 0f;
						if (v > 1f) v = 1f;

						row[x] = (byte)(v * 255f);
					}
				}
			}

			return img;
		}

		// Tensor of [3 x H x W] expected
		// ToDo: SIMD optimization
		public static unsafe Mat TensorToRgbImage(Tensor tens)
		{
			int h = (int)tens.shape[1];
			int w = (int)tens.shape[2];
			int count = w * h;

			// Allocate OpenCV Mat in BGR format
			Mat img = new Mat(h, w, MatType.CV_8UC3);

			// Apply sigmoid once per channel
			float[] rSrc = functional.sigmoid(tens[0]).data<float>().ToArray();
			float[] gSrc = functional.sigmoid(tens[1]).data<float>().ToArray();
			float[] bSrc = functional.sigmoid(tens[2]).data<float>().ToArray();

			byte* dst = (byte*)img.DataPointer;
			int stride = (int)img.Step(); // bytes per row

			fixed (float* pR = rSrc)
			fixed (float* pG = gSrc)
			fixed (float* pB = bSrc)
			{
				float* rPtr = pR;
				float* gPtr = pG;
				float* bPtr = pB;

				for (int y = 0; y < h; y++)
				{
					byte* row = dst + y * stride;

					for (int x = 0; x < w; x++)
					{
						// Read float in [0..1]
						float rf = *rPtr++;
						float gf = *gPtr++;
						float bf = *bPtr++;

						// Clamp for safety
						if (rf < 0f) rf = 0f; else if (rf > 1f) rf = 1f;
						if (gf < 0f) gf = 0f; else if (gf > 1f) gf = 1f;
						if (bf < 0f) bf = 0f; else if (bf > 1f) bf = 1f;

						// Write BGR (OpenCV format)
						byte* px = row + x * 3;
						px[0] = (byte)(bf * 255f); // B
						px[1] = (byte)(gf * 255f); // G
						px[2] = (byte)(rf * 255f); // R
					}
				}
			}

			return img;
		}


	}
}
