using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using TorchSharp;
using static TorchSharp.torch;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageConversion;

namespace AnnotationTool.Ai.Utils.TensorProcessing
{
	public static class TensorConversion
	{
		// RgbImageToTensor
		public static unsafe Tensor RgbMatToNormalizedTensor(Mat image, Device device, ScalarType precision, NormalizationSettings norm)
		{
			int h = image.Rows;
			int w = image.Cols;
			int count = h * w;

			// Allocate buffer: [R | G | B]
			float[] buffer = new float[count * 3];

			int rOff = 0;
			int gOff = count;
			int bOff = count * 2;

			const float inv255 = 1f / 255f;

			float meanR = norm.Mean[0];
			float meanG = norm.Mean[1];
			float meanB = norm.Mean[2];

			float invStdR = 1f / norm.Std[0];
			float invStdG = 1f / norm.Std[1];
			float invStdB = 1f / norm.Std[2];

			// Get pointer to raw Mat data (BGRBGRBGR…)
			byte* src = (byte*)image.DataPointer;

			fixed (float* dst = buffer)
			{
				float* rPtr = dst + rOff;
				float* gPtr = dst + gOff;
				float* bPtr = dst + bOff;

				int stride = w * 3; // 3 bytes per pixel

				for (int y = 0; y < h; y++)
				{
					byte* row = src + y * stride;

					for (int x = 0; x < w; x++)
					{
						int i = x * 3;

						// Load BGR pixel
						float vb = row[i] * inv255;
						float vg = row[i + 1] * inv255;
						float vr = row[i + 2] * inv255;

						// Store normalized channels
						*rPtr++ = (vr - meanR) * invStdR;
						*gPtr++ = (vg - meanG) * invStdG;
						*bPtr++ = (vb - meanB) * invStdB;
					}
				}
			}
			// Create tensor [3, H, W]
			return torch.tensor(buffer, dtype: precision, device: device)
						 .reshape(3, h, w);
		}

		// GreyImageToTensor
		public static unsafe Tensor GreyMatToNormalizedTensor(Mat image, Device device, ScalarType precision, NormalizationSettings norm)
		{
			int h = image.Rows;
			int w = image.Cols;
			int count = w * h;

			float[] buffer = new float[count];

			const float inv255 = 1f / 255f;

			float mean = norm.Mean[0];
			float invStd = 1f / norm.Std[0];

			byte* src = (byte*)image.DataPointer;

			fixed (float* dst = buffer)
			{
				float* gPtr = dst;

				// For CV_8UC1, stride == w in most cases,
				// but Step() is ALWAYS correct and handles alignment.

				int stride = (int)image.Step();

				for (int y = 0; y < h; y++)
				{
					byte* row = src + y * stride;

					for (int x = 0; x < w; x++)
					{
						float v = row[x] * inv255;
						*gPtr++ = (v - mean) * invStd;
					}
				}
			}
			return torch.tensor(buffer, dtype: precision, device: device).reshape(1, h, w);
		}

		/// <summary>
		/// Works for binary masks only ToDo: Non-binary masks/multi-class case
		/// </summary>
		public static unsafe Tensor MaskToTensor(Mat img, Device device, ScalarType precision)
		{
			if (img.Type() != MatType.CV_8UC1)
				throw new ArgumentException("Mask must be CV_8UC1 (single channel 8-bit)");

			int h = img.Rows;
			int w = img.Cols;
			int count = h * w;

			float[] buffer = new float[count];

			// Raw pointer to grayscale mask (1 byte per pixel)
			byte* src = (byte*)img.DataPointer;

			fixed (float* dst = buffer)
			{
				float* pDst = dst;

				int stride = (int)img.Step();  // Safe to cast for typical image sizes

				for (int y = 0; y < h; y++)
				{
					byte* row = src + y * stride;

					for (int x = 0; x < w; x++)
					{
						// Convert {0,255} → {0f,1f} //ToDo: Non-binary masks/multi-class case
						*pDst++ = row[x] > 127 ? 1f : 0f;
					}
				}
			}

			// [1, H, W]
			return torch.tensor(buffer, dtype: precision, device: device).reshape(1, h, w);
		}

		// Tensor of [A x B x H x W] to tensor of [A][B][H x W]
		public static Tensor[][] TensorTo2DArray(Tensor tens)
		{
			if (tens.Dimensions != 4)
				throw new ArgumentException("Tensor must have shape [A, B, H, W]");

			long A = tens.shape[0];
			long B = tens.shape[1];

			Tensor[][] result = new Tensor[A][];

			for (int a = 0; a < A; a++)
			{
				result[a] = new Tensor[B];

				Tensor sliceA = tens[a]; // [B, H, W]

				for (int b = 0; b < B; b++)
				{
					result[a][b] = sliceA[b].contiguous(); // [H, W]
				}
			}
			return result;
		}

		// Images[] -> Tensor:[Images.Length x 1 x Images.Width x Images.Height]
		public static Tensor SlicedImageToTensor(Mat[] images, bool trainImagesAsGreyscale, Device toDevice, NormalizationSettings norm, ScalarType precision)
		{
			int N = images.Length;

			// No parallel tasks needed here.
			Tensor[] outputs = new Tensor[N];

			for (int i = 0; i < N; i++)
			{
				if (trainImagesAsGreyscale)
				{
					// shape: [1, H, W]
					outputs[i] = GreyMatToNormalizedTensor(images[i], toDevice, precision, norm)
									.unsqueeze(0);
				}
				else
				{
					// shape: [3, H, W]
					outputs[i] = RgbMatToNormalizedTensor(images[i], toDevice, precision, norm)
									.unsqueeze(0);
				}
			}

			// Stack along batch dimension → [N, C, H, W]
			return torch.cat(outputs, 0);
		}

		// Result prediction tensor is always of type grey and needs to be converted back to an image
		public static Mat[] SlicedImageTensorToImage(Tensor[][] slicedImageTensor)
		{
			int N = slicedImageTensor.Length;
			Mat[] results = new Mat[N];

			for (int i = 0; i < N; i++)
			{
				// pred[i][0] is a [H x W] tensor for the mask
				results[i] = TensorToGreyImage(slicedImageTensor[i][0]);
			}

			return results;
		}

	}
}
