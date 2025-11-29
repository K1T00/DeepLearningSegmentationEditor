using System;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Utils.TensorProcessing
{
	public static class TensorComputations
	{

		/// <summary>
		/// Replacement for torchvision.functional.affine using core TorchSharp ops.
		/// Works for 3D [C,H,W] or 4D [N,C,H,W].
		/// </summary>
		public static Tensor SafeAffine(Tensor img,
			float angle,
			int[] translate,
			float scale,
			float[] shear,
			InterpolationMode mode,
			float fill)
		{
			var device = img.device;
			if (img.shape.Length == 3)
				img = img.unsqueeze(0);  // [1,C,H,W]
			if (!img.is_floating_point())
				img = img.to_type(ScalarType.Float32);

			var rad = angle * Math.PI / 180.0;
			var sx = shear.Length > 0 ? shear[0] * Math.PI / 180.0 : 0.0;
			var sy = shear.Length > 1 ? shear[1] * Math.PI / 180.0 : 0.0;

			var cosA = Math.Cos(rad);
			var sinA = Math.Sin(rad);

			// normalized translation: pixels → [-1,1] space
			var txNorm = translate[0] / (float)(img.shape[3] / 2.0);
			var tyNorm = translate[1] / (float)(img.shape[2] / 2.0);

			var theta = new float[,]
			{
				{ (float)( scale * cosA + Math.Tan(sy) * sinA ),
					(float)(-scale * sinA + Math.Tan(sx) * cosA ),
					txNorm },
				{ (float)( scale * sinA ),
					(float)( scale * cosA ),
					tyNorm }
			};

			var thetaT = tensor(theta, dtype: ScalarType.Float32, device: device).reshape(1, 2, 3);

			var grid = functional.affine_grid(thetaT, img.shape, align_corners: false);

			var outImg = functional.grid_sample(
				img, grid,
				mode: mode == InterpolationMode.Bilinear
					? GridSampleMode.Bilinear
					: GridSampleMode.Nearest,
				padding_mode: GridSamplePaddingMode.Zeros,
				align_corners: false);

			outImg = outImg.squeeze(0);  // remove batch dim
			return outImg.to(device);
		}


	}
}
