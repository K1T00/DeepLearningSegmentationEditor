using AnnotationTool.Core.Services;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageAnalysis;

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

		// ToDo: Alternative implementation
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

		public static unsafe (Mat superImposed, Mat heatmap) ImageToHeatmap(Mat img, Mat msk, int threshold)
		{
			// Convert grayscale to RGB if needed
			Mat image = new Mat();
			if (img.Type() == MatType.CV_8UC1)
				Cv2.CvtColor(img, image, ColorConversionCodes.GRAY2RGB);
			else
				img.CopyTo(image);

			// Threshold mask: pixels > threshold remain, else 0
			Cv2.Threshold(msk, msk, threshold, 255, ThresholdTypes.Tozero);

			// Create color heatmap from mask
			Mat heatmap = new Mat();
			Cv2.ApplyColorMap(msk, heatmap, ColormapTypes.Turbo);

			int h = msk.Rows;
			int w = msk.Cols;

			Mat imgHeatRgb = new Mat(h, w, MatType.CV_8UC3);

			byte* pMask = (byte*)msk.DataPointer;
			byte* pHeat = (byte*)heatmap.DataPointer;
			byte* pOut = (byte*)imgHeatRgb.DataPointer;

			int maskStride = (int)msk.Step();
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

		public static Task SliceTrainImagesAsync(IProjectPresenter project, string slicedImgDir, string slicedMaskDir, IProgress<int> progress, CancellationToken ct)
		{
			return Task.Run(() =>
			{
				var settings = project.Project.Settings.PreprocessingSettings;
				int total = project.Project.Images.Count;
				int done = 0;

				int sliceSize = settings.SliceSize;
				int down = settings.DownSample;
				int scaledSliceSize = sliceSize * (1 << down);

				foreach (var item in project.Project.Images)
				{
					ct.ThrowIfCancellationRequested();

					using (Mat img = Cv2.ImRead(item.Path, settings.TrainAsGreyscale ? ImreadModes.Grayscale : ImreadModes.Unchanged))
					using (Mat mask = Cv2.ImRead(item.MaskPath, ImreadModes.Grayscale))
					{
						// Binary mask
						Cv2.Threshold(mask, mask, 0, 255, ThresholdTypes.Binary);

						// Compute ROI large enough for downsampled slice
						int roiW = Math.Max(item.Roi.Width, scaledSliceSize);
						int roiH = Math.Max(item.Roi.Height, scaledSliceSize);
						var roi = new Rect(item.Roi.X, item.Roi.Y, roiW, roiH);

						// Extract ROI
						using (Mat roiImage = new Mat(img, roi).Clone())
						using (Mat roiMask = new Mat(mask, roi).Clone())
						{
							// Downsample ROI
							using (Mat imageDs = DownSampleImage(roiImage, down))
							using (Mat maskDs = DownSampleImage(roiMask, down))
							{
								// Determine how many tiles
								var (tilesY, tilesX) = GetAmtImages(
									imageDs.Height,
									imageDs.Width,
									sliceSize,
									settings.BorderPadding);

								// Prebuild file name prefix
								string guidPrefix = item.Guid.ToString();

								for (int py = 0; py < tilesY; py++)
								{
									for (int px = 0; px < tilesX; px++)
									{
										ct.ThrowIfCancellationRequested();

										int sx = px * sliceSize;
										int sy = py * sliceSize;

										using (Mat imgSub = GetPaddedRoi(imageDs, sx, sy, sliceSize, sliceSize, Scalar.Black))
										using (Mat maskSub = GetPaddedRoi(maskDs, sx, sy, sliceSize, sliceSize, Scalar.Black))
										{
											bool saveSlice = true;

											if (settings.TrainOnlyFeatures)
												saveSlice = IsBlobInImage(maskSub);

											if (saveSlice)
											{
												string name = guidPrefix + "_" + py + "_" + px + ".png";

												Cv2.ImWrite(Path.Combine(slicedImgDir, name), imgSub);
												Cv2.ImWrite(Path.Combine(slicedMaskDir, name), maskSub);
											}
										}
									}
								}
							}
						}
					}

					done++;
					progress?.Report(done * 100 / total);
				}
			}, ct);
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


	}
}
