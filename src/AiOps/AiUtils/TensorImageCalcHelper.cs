using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using AiOps.AiModels.UNet;
using static AiOps.AiUtils.HelperFunctions;


namespace AiOps.AiUtils
{
	public static class TensorImageCalcHelper
	{


		/// <summary>
		///     Convert Mat to Tensor
		/// </summary>
		/// <param name="mat"></param>
		/// <returns></returns>
		public static Tensor ToTensor(Mat mat)
		{
			var dims = GetDims(mat);
			mat.GetArray(out float[] d);
			var original = from_array(d);
			var final = original.reshape(dims);
			return final;
		}

		public static long[] GetDims(Mat mat)
		{
			mat.GetArray(out float[] d);
			var dims = Enumerable.Range(0, mat.Dims)
				.Select(a => (long)mat.Size(a))
				.ToArray();
			return dims;
		}

		public static string ToFormattedString(this Tensor tensor)
		{
			var builder = new StringBuilder();

			builder.AppendLine();

			// Determine tensor type and appropriate conversion
			Func<torch.Tensor, string> getItem;
			switch (tensor.dtype)
			{
				case torch.ScalarType.Float32:
					getItem = t => t.item<float>().ToString("F2");
					break;
				case torch.ScalarType.Int64:
					getItem = t => t.item<long>().ToString();
					break;
				case torch.ScalarType.Int32:
					getItem = t => t.item<int>().ToString();
					break;
				// Add other types as needed
				default:
					getItem = t => t.ToString();
					break;
			}

			if (tensor.dim() == 1)
			{
				builder.Append("[");
				for (long i = 0; i < tensor.size(0); i++)
				{
					builder.Append(getItem(tensor[i]));
					if (i < tensor.size(0) - 1)
						builder.Append(", ");
				}
				builder.Append("]");
			}
			else if (tensor.dim() == 2)
			{
				builder.Append("[");
				for (long i = 0; i < tensor.size(0); i++)
				{
					builder.Append("[");
					for (long j = 0; j < tensor.size(1); j++)
					{
						builder.Append(getItem(tensor[i, j]));
						if (j < tensor.size(1) - 1)
							builder.Append(", ");
					}
					builder.Append("]");
					if (i < tensor.size(0) - 1)
						builder.AppendLine(",");
					else if (i != tensor.size(0) - 1)
						builder.AppendLine();
				}
				builder.Append("]");
			}
			else if (tensor.dim() == 3)
			{
				builder.Append("[");
				for (long i = 0; i < tensor.size(0); i++)
				{
					builder.AppendLine($"Slice {i}:");
					builder.Append("[");
					for (long j = 0; j < tensor.size(1); j++)
					{
						builder.Append("[");
						for (long k = 0; k < tensor.size(2); k++)
						{
							builder.Append(getItem(tensor[i, j, k]));
							if (k < tensor.size(2) - 1)
								builder.Append(", ");
						}
						builder.Append("]");
						if (j < tensor.size(1) - 1)
							builder.AppendLine(",");
						else if (j != tensor.size(0) - 1)
							builder.AppendLine();
					}
					builder.Append("]");
					if (i < tensor.size(0) - 1)
						builder.AppendLine(",");
				}
				builder.AppendLine("]");
			}
			else
			{
				return "Tensors with more than 3 dimensions are not supported in this method.";
			}

			builder.AppendLine();

			return builder.ToString();
		}

		public static Tensor Softmax(this Tensor input, int dim = -1)
		{
			// Compute e^x_i for each element x_i in the input tensor.
			var expTensor = input.exp();

			// Sum the exponentiated values along the specified dimension, returns single value.
			var sumExp = expTensor.sum(dim, true);

			// Divide each exponentiated value by the sum of all exponentiated values.
			return expTensor / sumExp;
		}

		/// <summary>
		/// Converts an OpenCV.Mat of type 8UC1 to tensor (and also normalizing the values)
		/// </summary>
		/// <param name="image"></param>
		/// <param name="device"></param>
		/// <param name="usePrecision"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Tensor GreyImageToTensor(Mat image, Device device, ScalarType usePrecision)
		{
			if (image.Type() != MatType.CV_8UC1)
			{
				throw new ArgumentException("Invalid image type");
			}
			Tensor tens = null;

			var matVec = new Mat<Vec3b>(image);
			var indexer = matVec.GetIndexer();
			var imgCvChan = new float[image.Height * image.Width];

			if (usePrecision == float16)
			{
				//var imgCvChanNorm = new Half[image.Height * image.Width];

				//for (var y = 0; y < image.Height; y++)
				//{
				//	for (var x = 0; x < image.Width; x++)
				//	{
				//		imgCvChan[y * image.Width + x] = indexer[y, x].Item0;
				//	}
				//}

				//var mean = imgCvChan.Average();
				//var stdDevR = Math.Sqrt(imgCvChan.Average(r => Math.Pow(r - mean, 2)));

				//for (var i = 0; i < imgCvChan.Length; i++)
				//{
				//	imgCvChanNorm[i] = (Half)Convert.ToSingle((imgCvChan[i] - mean) / stdDevR);
				//}

				//tens = cat(new[]
				//{
				//	tensor(imgCvChanNorm, new long[] { 1, image.Width, image.Height }).to_type(bfloat16)
				//}, 0);
			}
			if (usePrecision == float32)
			{
				var imgCvChanNorm = new float[image.Height * image.Width];

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						imgCvChan[y * image.Width + x] = indexer[y, x].Item0;
					}
				}

				var mean = imgCvChan.Average();
				var stdDevR = Math.Sqrt(imgCvChan.Average(r => Math.Pow(r - mean, 2)));

				for (var i = 0; i < imgCvChan.Length; i++)
				{
					imgCvChanNorm[i] = Convert.ToSingle((imgCvChan[i] - mean) / stdDevR);
				}

				tens = cat(new[]
				{
					tensor(imgCvChanNorm, new long[] { 1, image.Width, image.Height }).to_type(float32)
				}, 0);
			}
			return tens?.to(device);
		}

		/// <summary>
		/// Converts an OpenCV.Mat of type 8UC3 to tensor (and also normalizing the values)
		/// </summary>
		/// <param name="image"></param>
		/// <param name="device"></param>
		/// <param name="usePrecision"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Tensor RgbImageToTensor(Mat image, Device device, ScalarType usePrecision)
		{
			if (image.Type() != MatType.CV_8UC3)
			{
				throw new ArgumentException("Invalid image type");
			}

			Tensor tens = null;

			var matVec = new Mat<Vec3b>(image);
			var indexer = matVec.GetIndexer();
			var imgCvChanR = new float[image.Height * image.Width];
			var imgCvChanG = new float[image.Height * image.Width];
			var imgCvChanB = new float[image.Height * image.Width];

			if (usePrecision == float16)
			{
				//var imgCvChanRNorm = new Half[image.Height * image.Width];
				//var imgCvChanGNorm = new Half[image.Height * image.Width];
				//var imgCvChanBNorm = new Half[image.Height * image.Width];

				//for (var y = 0; y < image.Height; y++)
				//{
				//	for (var x = 0; x < image.Width; x++)
				//	{
				//		imgCvChanR[y * image.Width + x] = indexer[y, x].Item2;
				//		imgCvChanG[y * image.Width + x] = indexer[y, x].Item1;
				//		imgCvChanB[y * image.Width + x] = indexer[y, x].Item0;
				//	}
				//}

				//var meanR = imgCvChanR.Average();
				//var meanG = imgCvChanG.Average();
				//var meanB = imgCvChanB.Average();
				//var stdDevR = Math.Sqrt(imgCvChanR.Average(r => Math.Pow(r - meanR, 2)));
				//var stdDevG = Math.Sqrt(imgCvChanG.Average(g => Math.Pow(g - meanG, 2)));
				//var stdDevB = Math.Sqrt(imgCvChanB.Average(b => Math.Pow(b - meanB, 2)));

				//for (var i = 0; i < imgCvChanR.Length; i++)
				//{
				//	imgCvChanRNorm[i] = (Half)Convert.ToSingle((imgCvChanR[i] - meanR) / stdDevR);
				//	imgCvChanGNorm[i] = (Half)Convert.ToSingle((imgCvChanG[i] - meanG) / stdDevG);
				//	imgCvChanBNorm[i] = (Half)Convert.ToSingle((imgCvChanB[i] - meanB) / stdDevB);
				//}

				//tens = cat(new[]
				//{
				//	tensor(imgCvChanRNorm, new long[] { 1, image.Width, image.Height }).to_type(bfloat16),
				//	tensor(imgCvChanGNorm, new long[] { 1, image.Width, image.Height }).to_type(bfloat16),
				//	tensor(imgCvChanBNorm, new long[] { 1, image.Width, image.Height }).to_type(bfloat16)
				//}, 0);
			}
			if (usePrecision == float32)
			{
				var imgCvChanRNorm = new float[image.Height * image.Width];
				var imgCvChanGNorm = new float[image.Height * image.Width];
				var imgCvChanBNorm = new float[image.Height * image.Width];

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						imgCvChanR[y * image.Width + x] = indexer[y, x].Item2;
						imgCvChanG[y * image.Width + x] = indexer[y, x].Item1;
						imgCvChanB[y * image.Width + x] = indexer[y, x].Item0;
					}
				}

				var meanR = imgCvChanR.Average();
				var meanG = imgCvChanG.Average();
				var meanB = imgCvChanB.Average();
				var stdDevR = Math.Sqrt(imgCvChanR.Average(r => Math.Pow(r - meanR, 2)));
				var stdDevG = Math.Sqrt(imgCvChanG.Average(g => Math.Pow(g - meanG, 2)));
				var stdDevB = Math.Sqrt(imgCvChanB.Average(b => Math.Pow(b - meanB, 2)));

				for (var i = 0; i < imgCvChanR.Length; i++)
				{
					imgCvChanRNorm[i] = Convert.ToSingle((imgCvChanR[i] - meanR) / stdDevR);
					imgCvChanGNorm[i] = Convert.ToSingle((imgCvChanG[i] - meanG) / stdDevG);
					imgCvChanBNorm[i] = Convert.ToSingle((imgCvChanB[i] - meanB) / stdDevB);
				}

				tens = cat(new[]
				{
					tensor(imgCvChanRNorm, new long[] { 1, image.Width, image.Height }).to_type(float32),
					tensor(imgCvChanGNorm, new long[] { 1, image.Width, image.Height }).to_type(float32),
					tensor(imgCvChanBNorm, new long[] { 1, image.Width, image.Height }).to_type(float32)
				}, 0);
			}
			return tens?.to(device);
		}

		/// <summary>
		/// Converts an image mask of type 8UC1 to tensor (not normalizing the values)
		/// </summary>
		/// <param name="img"></param>
		/// <param name="device"></param>
		/// <param name="usePrecision"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Tensor MaskToTensor(Mat img, Device device, ScalarType usePrecision)
		{
			if (img.Type() != MatType.CV_8UC1)
			{
				throw new ArgumentException("Invalid image type");
			}

			Tensor tens = null;
			var matVec = new Mat<Vec3b>(img);
			var indexer = matVec.GetIndexer();

			if (usePrecision == float16)
			{
				//var imgCvChan = new Half[img.Height * img.Width];

				//for (var y = 0; y < img.Height; y++)
				//{
				//	for (var x = 0; x < img.Width; x++)
				//	{
				//		imgCvChan[y * img.Width + x] = (Half)(indexer[y, x].Item0 / 255.0f);
				//	}
				//}

				//tens = cat(new[]
				//{
				//	tensor(imgCvChan, new long[] { 1, img.Width, img.Height }).to_type(bfloat16)
				//}, 0);
			}
			if (usePrecision == float32)
			{
				var imgCvChan = new float[img.Height * img.Width];

				for (var y = 0; y < img.Height; y++)
				{
					for (var x = 0; x < img.Width; x++)
					{
						imgCvChan[y * img.Width + x] = indexer[y, x].Item0 / 255.0f;
					}
				}

				tens = cat(new[]
				{
					tensor(imgCvChan, new long[] { 1, img.Width, img.Height }).to_type(float32)
				}, 0);
			}
			return tens?.to(device);
		}

		/// <summary>
		/// Converts a tensor to a Mat image of type 8UC1 (with standard normalization of *255)
		/// Tensor of [H x W] expected
		/// </summary>
		/// <param name="tens"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Mat TensorToGreyImage(Tensor tens)
		{
			if (tens.Dimensions != 2)
			{
				throw new ArgumentException("Invalid tensor dimensions");
			}

			var imgGrey = new Mat((int)tens.shape[0], (int)tens.shape[1], MatType.CV_8UC1);
			var tensFloatArray = tens.data<float>().ToArray();

			var a = 0;
			for (var x = 0; x < (int)tens.shape[0]; x++)
			{
				for (var y = 0; y < (int)tens.shape[1]; y++)
				{
					imgGrey.At<Vec3b>(x, y) =
						new Vec3b((byte)(tensFloatArray[a] * 255.0), (byte)(tensFloatArray[a] * 255.0), (byte)(tensFloatArray[a] * 255.0));

					a++;
				}
			}
			return imgGrey;
		}

		/// <summary>
		/// Converts a tensor to a Mat image of type 8UC1 (with standard normalization of *255)
		/// Tensor of [3 x H x W] expected
		/// </summary>
		/// <param name="tens"></param>
		/// <returns></returns>
		public static Mat TensorToRgbImage(Tensor tens)
		{

			var imgRgb = new Mat((int)tens[0].shape[0], (int)tens[0].shape[1], MatType.CV_8UC3);

			var tensSigR = functional.sigmoid(tens[0]);
			var tensSigG = functional.sigmoid(tens[1]);
			var tensSigB = functional.sigmoid(tens[2]);

			var tensFloatArrayR = tensSigR.data<float>().ToArray();
			var tensFloatArrayG = tensSigG.data<float>().ToArray();
			var tensFloatArrayB = tensSigB.data<float>().ToArray();

			var a = 0;
			for (var x = 0; x < (int)tens[0].shape[0]; x++)
			{
				for (int y = 0; y < (int)tens[0].shape[1]; y++)
				{
					imgRgb.At<Vec3b>(x, y) =
						new Vec3b((byte)(tensFloatArrayR[a] * 255.0), (byte)(tensFloatArrayG[a] * 255.0), (byte)(tensFloatArrayB[a] * 255.0));

					a++;
				}
			}
			return imgRgb;
		}

		/// <summary>
		/// Converts the first two dimensions of a tensor into arrays:
		/// Tensor of [A x B x H x W] to tensor of [A][B][H x W]
		/// </summary>
		/// <param name="tens"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Tensor[][] TensorTo2DArray(Tensor tens)
		{
			if (tens.Dimensions != 4)
			{
				throw new ArgumentException("Invalid tensor dimensions");
			}

			var dim0 = tens.shape[0];
			var dim1 = tens.shape[1];
			var tensArr = new Tensor[dim0][];

			// Split first dim to array
			var tens1Dim = tens.clone().split(1);

			for (var a = 0; a < dim0; a++)
			{
				// Split second dim to array
				var tens2Dim = tens1Dim[a].clone().squeeze(0).split(1);

				tensArr[a] = new Tensor[dim1];

				for (var b = 0; b < dim1; b++)
				{
					// Should always be [H x W]
					tensArr[a][b] = tens2Dim[b].clone().squeeze(0);
				}
			}
			return tensArr;
		}

		/// <summary>
		/// Converts tensor to multi dim array
		/// </summary>
		/// <param name="tensor"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static Array TensorToArray(Tensor tensor)
		{
			switch (tensor.dtype)
			{
				case ScalarType.Byte:
					return tensor.data<sbyte>().ToNDArray();

				case ScalarType.Int8:
					return tensor.data<sbyte>().ToNDArray();

				case ScalarType.Int16:
					return tensor.data<short>().ToNDArray();

				case ScalarType.Int32:
					return tensor.data<int>().ToNDArray();

				case ScalarType.Int64:
					return tensor.data<long>().ToNDArray();

				case ScalarType.Float16:
				//return tensor.data<Half>().ToNDArray();

				case ScalarType.BFloat16:


				case ScalarType.Float32:
					return tensor.data<float>().ToNDArray();

				case ScalarType.Float64:
					return tensor.data<double>().ToNDArray();

				case ScalarType.Bool:
					return tensor.data<bool>().ToNDArray();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Converts an array of Mat images to a tensor which has a first dimension size of the amount of images:
		/// Images[] -> Tensor:[Images.Length x 1 x Images.Width x Images.Height]
		/// This makes calculations later on much more efficient
		/// </summary>
		/// <param name="images"></param>
		/// <param name="trainImagesAsGreyscale"></param>
		/// /// <param name="toDevice"></param>
		/// /// <param name="precision"></param>
		/// <returns></returns>
		public static async Task<Tensor> SlicedImageToTensor(Mat[] images, bool trainImagesAsGreyscale, Device toDevice, ScalarType precision)
		{
			var tasks = new Task<Tensor>[images.Length];

			var t = 0;
			foreach (var image in images)
			{
				if (trainImagesAsGreyscale)
				{
					tasks[t] = Task.Run(() => GreyImageToTensor(image, toDevice, precision).unsqueeze(1));
				}
				else
				{
					tasks[t] = Task.Run(() => RgbImageToTensor(image, toDevice, precision).unsqueeze(0));
				}
				t++;
			}
			return cat(await Task.WhenAll(tasks), 0);
		}

		/// <summary>
		/// Result prediction tensor is always of type grey and needs to be converted back to an image
		/// </summary>
		/// <param name="slicedImageTensor"></param>
		/// <returns></returns>
		public static async Task<Mat[]> SlicedImageTensorToImage(Tensor[][] slicedImageTensor)
		{
			var tasks = new Task<Mat>[slicedImageTensor.Length];
			var t = 0;
			foreach (var pred in slicedImageTensor)
			{
				tasks[t] = Task.Run(() => TensorToGreyImage(pred[0]));
				t++;
			}
			return await Task.WhenAll(tasks);
		}


		/// <summary>
		/// Hopefully images are sorted ...
		/// </summary>
		/// <param name="images">All the images, that should be merged</param>
		/// <param name="nRowImages">Amount of images in the columns direction</param>
		/// <param name="nColumnImages">Amount of images in the rows direction</param>
		/// <returns>Merged image</returns>
		/// <exception cref="ArgumentException"></exception>
		public static Mat MergeImages(Mat[] images, int nRowImages, int nColumnImages)
		{
			if (images.Length % (nRowImages * nColumnImages) != 0)
			{
				throw new ArgumentException("Amount of images do not fit");
			}

			var concatImages = new Mat();

			var concatRows = new Mat[nRowImages];

			var i = 0;
			for (var row = 0; row < nRowImages; row++)
			{
				var concatColumns = new Mat[nColumnImages];


				for (var col = 0; col < nColumnImages; col++)
				{
					concatColumns[col] = images[i];
					i++;
				}
				concatRows[row] = new Mat();
				Cv2.HConcat(concatColumns, concatRows[row]);
			}


			Cv2.VConcat(concatRows, concatImages);

			return concatImages;
		}

		/// <summary>
		/// Check whether there is a feature in the image (mask)
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public static bool IsBlobInImage(Mat image)
		{
			var imgWithBorder = new Mat();
			//Cv2.BitwiseNot(img, img);

			// Touching border blobs are not detected, therefore draw frame
			Cv2.CopyMakeBorder(image, imgWithBorder, 10, 10, 10, 10, BorderTypes.Constant, OpenCvSharp.Scalar.Black);

			var blobParams = new SimpleBlobDetector.Params
			{
				FilterByColor = false,
				FilterByArea = false,
				FilterByCircularity = false,
				FilterByConvexity = false,
				FilterByInertia = false
			};

			var blobDetector = SimpleBlobDetector.Create(blobParams);

			return blobDetector.Detect(imgWithBorder).Length > 0;
		}

		/// <summary>
		/// Get roi from image and apply padding if it falls outside the image
		/// </summary>
		/// <param name="input"></param>
		/// <param name="roiTopLeftX"></param>
		/// <param name="roiTopLeftY"></param>
		/// <param name="roiWidth"></param>
		/// <param name="roiHeight"></param>
		/// <param name="paddingColor"></param>
		/// <returns></returns>
		public static Mat GetPaddedRoi(Mat input, int roiTopLeftX, int roiTopLeftY, int roiWidth, int roiHeight, OpenCvSharp.Scalar paddingColor)
		{
			Mat output;
			var bottomRightX = roiTopLeftX + roiWidth;
			var bottomRightY = roiTopLeftY + roiHeight;

			// Border padding will be required
			if (roiTopLeftX < 0 || roiTopLeftY < 0 || bottomRightX > input.Cols || bottomRightY > input.Rows)
			{
				var paddingBorderLeft = 0;
				var paddingBorderRight = 0;
				var paddingBorderTop = 0;
				var paddingBorderBottom = 0;
				var newImgTopLeftX = 0;
				var newImgTopLeftY = 0;

				// Check where padding is needed and adjust new image roi values
				if (roiTopLeftX < 0)
				{
					paddingBorderLeft = roiTopLeftX * -1;
				}
				if (roiTopLeftX > 0)
				{
					newImgTopLeftX += roiTopLeftX;
				}
				if (roiTopLeftY < 0)
				{
					paddingBorderTop = -1 * roiTopLeftY;
				}
				if (roiTopLeftY > 0)
				{
					newImgTopLeftY += roiTopLeftY;
				}
				if (bottomRightX > input.Cols)
				{
					paddingBorderRight = bottomRightX - input.Cols;
				}
				if (bottomRightY > input.Rows)
				{
					paddingBorderBottom = bottomRightY - input.Rows;
				}
				var padded = new Mat();

				Cv2.CopyMakeBorder(input, padded, paddingBorderTop, paddingBorderBottom, paddingBorderLeft, paddingBorderRight, BorderTypes.Constant, paddingColor);

				output = padded.SubMat(newImgTopLeftY, roiHeight + newImgTopLeftY, newImgTopLeftX, newImgTopLeftX + roiWidth);

				padded.Dispose();
			}
			else // No border padding required
			{
				output = input.SubMat(roiTopLeftY, roiHeight + roiTopLeftY, roiTopLeftX, roiWidth + roiTopLeftX);
			}
			return output;
		}

		/// <summary>
		/// Slice images into smaller ones (optionally with border padding) and save the results
		/// </summary>
		/// <param name="dataParameter"></param>
		/// <param name="filterByBlobs"></param>
		/// <param name="convertToGreyscale"></param>
		/// <param name="withBorderPadding"></param>
		/// <returns></returns>
		public static int SliceTrainImages(UNetData dataParameter, bool filterByBlobs, bool convertToGreyscale, bool withBorderPadding)
		{
			var withMasks = dataParameter.DatasetMasks.Count > 0;
			var imRoi = 1;
			var img = new Mat();
			var msk = new Mat();
			var imgSub = new Mat();
			var mskSub = new Mat();
			var normMsk = new Mat();

			for (var i = 0; i < dataParameter.DatasetImages.Count; i++)
			{
				// Load image
				if (convertToGreyscale)
				{
					Cv2.ImRead(dataParameter.DatasetImages[i].Path, ImreadModes.Grayscale).CopyTo(img);
				}
				else
				{
					Cv2.ImRead(dataParameter.DatasetImages[i].Path, ImreadModes.Unchanged).CopyTo(img);
				}
				// Load mask
				if (withMasks)
				{
					Cv2.ImRead(dataParameter.DatasetMasks[i].Path, ImreadModes.Unchanged).CopyTo(msk);
					msk.ConvertTo(msk, MatType.CV_8UC1);
					normMsk = new Mat(msk.Rows, msk.Cols, MatType.CV_8UC1);
					Cv2.ExtractChannel(msk, normMsk, 0);
					Cv2.Normalize(normMsk, normMsk, 0, 255, NormTypes.MinMax);
				}
				var imageDs = DownSampleImage(img, dataParameter.DownSampling);
				var maskDs = DownSampleImage(normMsk, dataParameter.DownSampling);

				var amtImages = GetAmtImages(imageDs.Height, imageDs.Width, dataParameter.SliceRoi, withBorderPadding);

				for (var pY = 0; pY < amtImages.Item1; pY++)
				{
					for (var pX = 0; pX < amtImages.Item2; pX++)
					{
						imgSub = GetPaddedRoi(imageDs, pX * dataParameter.SliceRoi, pY * dataParameter.SliceRoi, dataParameter.SliceRoi, dataParameter.SliceRoi, OpenCvSharp.Scalar.Black);
						if (withMasks) mskSub = GetPaddedRoi(maskDs, pX * dataParameter.SliceRoi, pY * dataParameter.SliceRoi, dataParameter.SliceRoi, dataParameter.SliceRoi, OpenCvSharp.Scalar.Black);

						if (filterByBlobs)
						{
							if (IsBlobInImage(mskSub))
							{
								Cv2.ImWrite(Path.Combine(dataParameter.TrainGrabsPrePath, imRoi + ".bmp"), imgSub);
								if (withMasks) Cv2.ImWrite(Path.Combine(dataParameter.TrainMasksPrePath, imRoi + ".bmp"), mskSub);
								imRoi++;
							}
						}
						else
						{
							Cv2.ImWrite(Path.Combine(dataParameter.TrainGrabsPrePath, imRoi + ".bmp"), imgSub);
							if (withMasks) Cv2.ImWrite(Path.Combine(dataParameter.TrainMasksPrePath, imRoi + ".bmp"), mskSub);
							imRoi++;
						}
					}
				}
			}
			img.Dispose();
			msk.Dispose();
			imgSub.Dispose();
			normMsk.Dispose();
			mskSub.Dispose();
			GC.Collect();

			return imRoi;
		}

		public static int GenerateMasks(UNetData dataParameter, int featureSize)
		{

			var csvFiles = LoadCsvFiles(dataParameter.DatasetLocations.Select(t => t.Path).ToArray());

			var locationsList = GetLocations(csvFiles);

			// Assume all images have the same size, so we load just the first to get the dimensions
			var img = Cv2.ImRead(dataParameter.DatasetImages[0].Path, ImreadModes.Grayscale);
			var msk = new Mat(img.Rows, img.Cols, MatType.CV_8UC1).SetTo(0);

			var feature = new Mat(featureSize, featureSize, MatType.CV_8UC1).SetTo(255);

			var featureHalfSize = Convert.ToInt16(Math.Round(featureSize / 2.0));

			var i = 0;
			foreach (var locations in locationsList)
			{
				msk = new Mat(img.Rows, img.Cols, MatType.CV_8UC1).SetTo(0);

				foreach (var location in locations)
				{
					// Draw image at location
					msk[location.Y - featureHalfSize, location.Y + featureHalfSize, location.X - featureHalfSize, location.X + featureHalfSize] = feature;
				}
				Cv2.ImWrite(Path.Combine(dataParameter.TrainMasksPath, dataParameter.DatasetLocations[i].ImageIndex + ".bmp"), msk);
				i++;
			}

			img.Dispose();
			msk.Dispose();
			GC.Collect();

			return locationsList.Count;
		}

		public static Mat ImageToHeatmap(Mat img, Mat msk, int threshold)
		{
			var image = new Mat();
			var heatmap = new Mat();
			var superImposedImage = new Mat();
			var imgHeatRgb = new Mat(img.Height, img.Width, MatType.CV_8UC3);

			if (img.Type() == MatType.CV_8UC1)
			{
				Cv2.CvtColor(img, image, ColorConversionCodes.GRAY2RGB);
			}
			else
			{
				Cv2.CopyTo(img, image);
			}

			Cv2.Threshold(msk, msk, threshold, 0, ThresholdTypes.Tozero);
			Cv2.ApplyColorMap(msk, heatmap, ColormapTypes.Turbo);

			var vecMsk = new Mat<Vec3b>(msk);
			var vecHm = new Mat<Vec3b>(heatmap);
			var indexerMsk = vecMsk.GetIndexer();
			var indexerHm = vecHm.GetIndexer();

			for (var y = 0; y < msk.Height; y++)
			{
				for (var x = 0; x < msk.Width; x++)
				{
					if (indexerMsk[y, x].Item0 > 0)
					{
						imgHeatRgb.At<Vec3b>(y, x) =
							new Vec3b(indexerHm[y, x].Item0, indexerHm[y, x].Item1, indexerHm[y, x].Item2);
					}
					else
					{
						imgHeatRgb.At<Vec3b>(y, x) = new Vec3b(0, 0, 0);
					}
				}
			}
			Cv2.AddWeighted(imgHeatRgb, 0.75, image, 1, 0, superImposedImage);

			return superImposedImage;
		}

		/// <summary>
		/// Split image into smaller ones
		/// </summary>
		/// <param name="image"></param>
		/// <param name="roiSize"></param>
		/// <param name="withBorderPadding"></param>
		/// <param name="nImagesRow"></param>
		/// <param name="nImagesColumn"></param>
		/// <returns></returns>
		public static Mat[] SliceImage(Mat image, int roiSize, bool withBorderPadding, int nImagesRow, int nImagesColumn)
		{
			var imagesSplit = new Mat[nImagesRow * nImagesColumn];

			var im = 0;
			for (var pY = 0; pY < nImagesRow; pY++)
			{
				for (var pX = 0; pX < nImagesColumn; pX++)
				{
					var imgSub = GetPaddedRoi(image, pX * roiSize, pY * roiSize, roiSize, roiSize, OpenCvSharp.Scalar.Black);

					imagesSplit[im] = imgSub;
					im++;
				}
			}
			return imagesSplit;
		}
		/// <summary>
		/// Reduce resolution by factor 2^n
		/// </summary>
		/// <param name="image"></param>
		/// <param name="nDownSampling"></param>
		/// <returns></returns>
		public static Mat DownSampleImage(Mat image, int nDownSampling)
		{
			var imageDs = image.Clone();

			for (var ds = 0; ds < nDownSampling; ds++)
			{
				imageDs.Resize(new OpenCvSharp.Size(0, 0), 0.5, 0.5, InterpolationFlags.Nearest).CopyTo(imageDs);
			}

			return imageDs;
		}
		/// <summary>
		/// Apply up-sampling to an image
		/// </summary>
		/// <param name="image"></param>
		/// <param name="nUpSampling"></param>
		/// <returns></returns>
		public static Mat UpSampleImage(Mat image, int nUpSampling)
		{
			var imageDs = image.Clone();

			for (var ds = 0; ds < nUpSampling; ds++)
			{
				imageDs.Resize(new OpenCvSharp.Size(0, 0), 2.0, 2.0, InterpolationFlags.Nearest).CopyTo(imageDs);
			}

			return imageDs;
		}

	}
}
