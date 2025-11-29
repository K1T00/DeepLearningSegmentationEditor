using AnnotationTool.Core.Models;
using System;
using TorchSharp;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Utils
{
	/// <summary>
	/// Mat image and Tensor related computations
	/// </summary>
	public static class AiUtils
	{

		public static Device ResolveDevice(DeepLearningSettings settings)
		{
			switch (settings.TrainModelSettings.Device)
			{
				case ComputeDevice.Cpu:
					return new Device(DeviceType.CPU);
				case ComputeDevice.Gpu:
					return new Device(DeviceType.CUDA);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Estimate micro-batch size for U-Net training by summing approximate activation memory across encoder, bottleneck, and decoder.
		/// </summary>
		public static int EstimateBatchSize(
			int imageWidth,
			int imageHeight,
			int channels,
			long availableMemoryBytes,
			int unetDepth,
			int firstFilter,
			ScalarType trainPrecision,
			bool keepSkipBuffers,
			double safetyFraction,
			double bwdMultiplier)
		{
			if (imageWidth <= 0 || imageHeight <= 0 || channels <= 0 || availableMemoryBytes <= 0)
				return 1;

			// bytes per element based on dtype
			var bpe = 4;
			if (trainPrecision == ScalarType.Float16 || trainPrecision == ScalarType.BFloat16)
				bpe = 2;

			// ---- Sum activations per sample across the whole U-Net ----
			var convAct = SumConvActivationsUNet(imageHeight, imageWidth, unetDepth, firstFilter, keepSkipBuffers);

			// Account for backprop storage, BN/IN stats, workspace, etc.
			var activationsBytesPerImage = convAct * bpe * bwdMultiplier;

			// Leave headroom
			var usable = availableMemoryBytes * safetyFraction;

			// Final estimate
			var batch = (int)Math.Max(1.0, Math.Floor(usable / activationsBytesPerImage));

			// Smoothing to nearby convenient numbers
			if (batch >= 16)
				batch -= batch % 4;
			else if (batch >= 8)
				batch -= batch % 2;

			return Math.Max(1, batch);
		}

		private static double SumConvActivationsUNet(int h, int w, int unetDepth, int firstFilter, bool keepSkipBuffers)
		{
			var sum = 0.0;
			var curH = h;
			var curW = w;

			// Encoder
			for (var i = 0; i < unetDepth; i++)
			{
				var outC = firstFilter << i; // firstFilter * 2^i

				// two conv outputs at this resolution
				sum += 2.0 * curH * curW * outC;

				// one skip buffer kept until decoder uses it
				if (keepSkipBuffers)
					sum += 1.0 * curH * curW * outC;

				// next level resolution halves
				curH = Math.Max(1, (curH + 1) / 2);
				curW = Math.Max(1, (curW + 1) / 2);
			}

			// Bottleneck
			{
				var outC = firstFilter << unetDepth;
				sum += 2.0 * curH * curW * outC;
			}

			// Decoder (mirror)
			for (var i = unetDepth - 1; i >= 0; i--)
			{
				curH = curH * 2;
				curW = curW * 2;

				var outC = firstFilter << i;
				sum += 2.0 * curH * curW * outC;
			}

			return sum;
		}

	}
}