using AnnotationTool.Core.Models;
using System;
using System.Collections.Generic;
using static TorchSharp.torch;
using static TorchSharp.torchvision.transforms.functional;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorComputations;

namespace AnnotationTool.Ai.Utils
{
	/// <summary>
	/// 
	/// Interface for paired image and mask transformations.
	/// Wrappers are needed because we may need to set up different transforms for images and masks.
	/// 
	/// </summary>
	public interface IPairedTransform
	{
		(Tensor image, Tensor mask) Apply(Tensor image, Tensor mask);
	}

	public class ComposePairedTransforms : IPairedTransform
	{
		private readonly List<IPairedTransform> transforms;
		public ComposePairedTransforms(List<IPairedTransform> transforms) => this.transforms = transforms;

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			foreach (var t in transforms)
				(image, mask) = t.Apply(image, mask);
			return (image, mask);
		}
	}

	public static class ImageAugmentations
	{
		/// <summary>
		/// Returns a composition of paired augmentations for training.
		/// </summary>
		public static IPairedTransform BuildAugmentations(AugmentationSettings s)
		{
			var list = new List<IPairedTransform>();

			// --- Geometric (paired) ---
			if (s.FlipHorizontal) list.Add(new PairedHorizontalFlip(0.5));
			if (s.FlipVertical) list.Add(new PairedVerticalFlip(0.5));
			if (s.Rotation != 0 || s.RelativeTranslation != 0 ||
				s.MinScale > 0 || s.MaxScale > 0 ||
				s.HorizontalShear != 0 || s.VerticalShear != 0)
				list.Add(new PairedRandomAffine(s));

			// --- Photometric (image only) ---
			if (s.Brightness > 0) list.Add(new AdjustBrightness(s.Brightness));
			if (s.Contrast > 0) list.Add(new AdjustContrast(s.Contrast));
			if (s.Luminance > 0) list.Add(new AdjustSaturation(s.Luminance));
			if (s.Noise > 0) list.Add(new AddNoise(s.Noise));
			if (s.GaussianBlur > 0) list.Add(new GaussianBlurImageOnly(s.GaussianBlur));
			//if (s.Gamma > 0) list.Add(new AdjustGamma(s.Gamma)); // ToDo
			//if (s.Hue > 0) list.Add(new AdjustHue(s.Hue)); // ToDo

			return new ComposePairedTransforms(list);
		}
	}

	#region Geometric transforms (paired image and mask)

	public class PairedHorizontalFlip : IPairedTransform
	{
		private readonly double p;
		private readonly Random rng = new Random();

		public PairedHorizontalFlip(double probability)
		{
			p = probability;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			if (rng.NextDouble() < p)
			{
				image = hflip(image);
				mask = hflip(mask);
			}
			return (image, mask);
		}
	}

	public class PairedVerticalFlip : IPairedTransform
	{
		private readonly double p;
		private readonly Random rng = new Random();

		public PairedVerticalFlip(double probability)
		{
			p = probability;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			if (rng.NextDouble() < p)
			{
				image = vflip(image);
				mask = vflip(mask);
			}
			return (image, mask);
		}
	}

	public class PairedRandomAffine : IPairedTransform
	{
		private readonly int maxDeg, maxTrans, minScale, maxScale, shearX, shearY;
		private readonly Random rng = new Random();

		public PairedRandomAffine(AugmentationSettings s)
		{
			maxDeg = s.Rotation;
			maxTrans = s.RelativeTranslation;
			minScale = s.MinScale > 0 ? s.MinScale : 100;
			maxScale = s.MaxScale > 0 ? s.MaxScale : 100;
			shearX = s.HorizontalShear;
			shearY = s.VerticalShear;
		}

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			// --- sample random params ---
			var angle = (float)((rng.NextDouble() * 2 - 1) * maxDeg);
			var scale = (float)((rng.Next(minScale, maxScale + 1)) / 100.0);
			int tx = 0, ty = 0;
			if (maxTrans > 0)
			{
				tx = (int)((rng.NextDouble() * 2 - 1) * maxTrans / 100.0 * image.shape[2]);
				ty = (int)((rng.NextDouble() * 2 - 1) * maxTrans / 100.0 * image.shape[1]);
			}
			var sx = (float)((rng.NextDouble() * 2 - 1) * shearX);
			var sy = (float)((rng.NextDouble() * 2 - 1) * shearY);

			// --- apply to both image and mask ---
			image = SafeAffine(image, angle, new int[] { tx, ty }, scale, new float[] { sx, sy },
				InterpolationMode.Bilinear, 0);
			mask = SafeAffine(mask, angle, new int[] { tx, ty }, scale, new float[] { sx, sy },
				InterpolationMode.Bilinear, 0);

			return (image, mask);
		}
	}

	#endregion

	#region Photometric transforms (image only)


	public class AdjustBrightness : IPairedTransform
	{
		private readonly int value;
		private readonly Random rng = new Random();

		public AdjustBrightness(int value) { this.value = value; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			var factor = 1.0 + (rng.NextDouble() * 2 - 1) * (value / 100.0);
			image = adjust_brightness(image, factor);
			return (image, mask);
		}
	}

	public class AdjustContrast : IPairedTransform
	{
		private readonly int value;
		private readonly Random rng = new Random();

		public AdjustContrast(int value) { this.value = value; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			var factor = 1.0 + (rng.NextDouble() * 2 - 1) * (value / 100.0);
			image = adjust_contrast(image, factor);
			return (image, mask);
		}
	}

	public class AdjustSaturation : IPairedTransform
	{
		private readonly int value;
		private readonly Random rng = new Random();

		public AdjustSaturation(int value) { this.value = value; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			var factor = 1.0 + (rng.NextDouble() * 2 - 1) * (value / 100.0);
			image = adjust_saturation(image, factor);
			return (image, mask);
		}
	}

	public class AdjustGamma : IPairedTransform
	{
		private readonly int value;
		private readonly Random rng = new Random();

		public AdjustGamma(int value) { this.value = value; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			var gamma = 1.0 + (rng.NextDouble() * 2 - 1) * (value / 100.0);
			image = adjust_gamma(image, gamma);
			return (image, mask);
		}
	}

	public class AdjustHue : IPairedTransform
	{
		private readonly int value;
		private readonly Random rng = new Random();

		public AdjustHue(int value) { this.value = value; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			var hueFactor = (rng.NextDouble() * 2 - 1) * (value / 100.0);
			image = adjust_hue(image, hueFactor);
			return (image, mask);
		}
	}

	public class AddNoise : IPairedTransform
	{
		private readonly int std;
		public AddNoise(int std) { this.std = std; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			if (std <= 0) return (image, mask);
			var noise = randn_like(image) * (std / 100.0f);
			image = (image + noise).clamp(0, 1);
			return (image, mask);
		}
	}

	public class GaussianBlurImageOnly : IPairedTransform
	{
		private readonly int sigma;
		private readonly Random rng = new Random();

		public GaussianBlurImageOnly(int sigma) { this.sigma = sigma; }

		public (Tensor image, Tensor mask) Apply(Tensor image, Tensor mask)
		{
			const long kernel = 5;
			var sig = (float)Math.Max(0.1, rng.NextDouble() * sigma);
			image = gaussian_blur(image, kernel, sig);
			return (image, mask);
		}
	}

	#endregion

}
