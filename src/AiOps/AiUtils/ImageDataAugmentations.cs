using System;
using System.Collections.Generic;
using AiOps.AiModels.UNet;
using OpenCvSharp;
using static TorchSharp.torch;
using static TorchSharp.torchvision;
using static TorchSharp.torchvision.transforms;

namespace AiOps.AiUtils
{
	/// <summary>
	/// ToDo: !!!!!!!!!!!!! WIP !!!!!!!!!!!!!
	/// </summary>
	public static class UNetAugmentations
	{
		/// <summary>
		/// Returns a composition of paired augmentations for training.
		/// </summary>
		public static IPairedTransform BuildTrainingAugmentations(UNetModelParameter modelParams)
		{
			//return new ComposePairedTransforms(new List<IPairedTransform>
			//{
			//	new PairedRandomHorizontalFlip(0.5),
			//	new GaussianBlurImageOnly(0.3, kernelSize: 5, sigma: 1.0),
			//	new NormalizeImageOnly(new double[] { 0.5, 0.5, 0.5 }, new double[] { 0.5, 0.5, 0.5 }),
			//});

			return new ComposePairedTransforms(new List<IPairedTransform> { });
		}

		/// <summary>
		/// Returns a simpler transformation for validation/inference.
		/// </summary>
		public static IPairedTransform BuildValidationAugmentations(UNetModelParameter modelParams)
		{
			//return new ComposePairedTransforms(new List<IPairedTransform>
			//{
			//	new NormalizeImageOnly(new double[] { 0.5, 0.5, 0.5 }, new double[] { 0.5, 0.5, 0.5 })
			//});

			return new ComposePairedTransforms(new List<IPairedTransform> { });
		}
	}



	/// <summary>
	/// Interface for paired image and mask transformations.
	/// 
	/// Wrappers are needed because we may need to set up different transforms for images and masks.
	/// </summary>
	public interface IPairedTransform
	{
		(Tensor image, Tensor mask) Apply(Tensor image, Tensor mask);
	}

	public class ComposePairedTransforms : IPairedTransform
	{
		private readonly List<IPairedTransform> transforms;

		public ComposePairedTransforms(List<IPairedTransform> transforms)
		{
			this.transforms = transforms;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			foreach (var transform in transforms)
			{
				(image, mask) = transform.Apply(image, mask);
			}
			return (image, mask);
		}
	}



	/// <summary>
	/// Image data augmentations for paired image and mask tensors.
	/// </summary>


	public class NormalizeImageOnly : IPairedTransform
	{
		private readonly ITransform transform;

		public NormalizeImageOnly(double[] means, double[] stdevs)
		{
			// TEST
			//means = new double[] { 0.5, 0.5, 0.5 };
			//stdevs = new double[] { 0.5, 0.5, 0.5 };

			var transformNormalized = Normalize(means, stdevs);
			transform = Compose(transformNormalized);
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			return (transform.call(image), mask);
		}
	}

	public class PairedRandomHorizontalFlip : IPairedTransform
	{
		private readonly Random rand = new Random();
		private readonly double probability;

		public PairedRandomHorizontalFlip(double probability = 0.5)
		{
			this.probability = probability;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			if (rand.NextDouble() < probability)
			{
				image = image.flip(new long[] { 2 }); // width
				mask = mask.flip(new long[] { 2 });
			}
			return (image, mask);
		}
	}

	public class ResizeImageOnly : IPairedTransform
	{
		private readonly ITransform resize;

		public ResizeImageOnly(int width, int height)
		{
			resize = Resize(width, height);
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			return (resize.call(image), mask);
		}
	}

	public class GaussianBlurImageOnly : IPairedTransform
	{
		private readonly double probability;
		private readonly int kernelSize;
		private readonly double sigma;
		private readonly Random rand = new Random();

		public GaussianBlurImageOnly(double probability = 0.5, int kernelSize = 5, double sigma = 1.0)
		{
			this.probability = probability;
			this.kernelSize = kernelSize;
			this.sigma = sigma;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			if (rand.NextDouble() >= probability)
				return (image, mask);

			using (var mat = TensorToMat(image))
			{

				var blurred = new Mat();
				Cv2.GaussianBlur(mat, blurred, new OpenCvSharp.Size(kernelSize, kernelSize), sigma);

				var blurredTensor = MatToTensor(blurred, image.device, image.dtype);
				image.Dispose();
				return (blurredTensor, mask);
				
			}

		}

		private Mat TensorToMat(Tensor tensor)
		{
			var cpu = tensor.cpu();
			var dims = tensor.shape;
			if (dims.Length != 3) throw new Exception("Expecting 3D tensor (C,H,W)");

			var channels = (int)dims[0];
			var height = (int)dims[1];
			var width = (int)dims[2];
			var mat = new Mat(height, width, channels == 1 ? MatType.CV_32FC1 : MatType.CV_32FC3);

			var data = cpu.data<float>().ToArray();
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (channels == 1)
					{
						float val = data[0 * height * width + y * width + x];
						mat.Set(y, x, val);
					}
					else
					{
						float r = data[0 * height * width + y * width + x];
						float g = data[1 * height * width + y * width + x];
						float b = data[2 * height * width + y * width + x];
						mat.Set(y, x, new Vec3f(r, g, b));
					}
				}
			}
			return mat;
		}

		private Tensor MatToTensor(Mat mat, Device device, ScalarType dtype)
		{
			int height = mat.Rows;
			int width = mat.Cols;
			int channels = mat.Channels();

			var data = new float[channels, height, width];
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (channels == 1)
					{
						data[0, y, x] = mat.At<float>(y, x);
					}
					else
					{
						var v = mat.At<Vec3f>(y, x);
						data[0, y, x] = v.Item0;
						data[1, y, x] = v.Item1;
						data[2, y, x] = v.Item2;
					}
				}
			}

			return tensor(data, dtype: dtype, device: device);
		}
	}



}
