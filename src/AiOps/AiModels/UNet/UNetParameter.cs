using System;
using TorchSharp;
using static TorchSharp.torch;
using AiOps.AiUtils;

namespace AiOps.AiModels.UNet
{
	[Serializable]
	public class UNetModelParameter : ICloneable, IHyperParameter
	{
		public string UNeModelParameterFile = "ModelParameter.xml";

		/// <summary>
		/// At what epoch the training should stop
		/// </summary>
		public int MaxEpochs { get; set; } = 0;

		/// <summary>
		/// Learning rate
		/// </summary>
		public double LearningRate { get; set; } = 0.05;

		/// <summary>
		/// Size of one batch should depend on the GPU memory available
		/// </summary>
		public int BatchSize { get; set; } = 1;

		/// <summary>
		/// Amount of classes
		/// </summary>
		public int Features { get; set; } = 1;

		/// <summary>
		/// At which loss the model should stop training
		/// </summary>
		public float StopAtLoss { get; set; } = 0.01f;

		/// <summary>
		/// Train RGB images as greyscale or not
		/// </summary>
		public bool TrainImagesAsGreyscale { get; set; } = true;

		/// <summary>
		/// The percentage split between train and validation data
		/// </summary>
		public float SplitTrainValidationSet { get; set; } = 0.8f;

		/// <summary>
		/// Precision used for image tensors
		/// </summary>
		public ScalarType TrainPrecision { get; set; } = ScalarType.Float32;

		/// <summary>
		/// Train device: CPU or CUDA
		/// </summary>
		public DeviceType TrainOnDevice { get; set; } = DeviceType.CPU;

		/// <summary>
		/// Filter size of first layer
		/// </summary>
		public int FirstFilterSize { get; set; } = 64; //64

		/// <summary>
		/// Amount of layers in the model
		/// </summary>
		public int Depth { get; set; } = 4;

		public bool UsePooling { get; set; } = true;
		public bool UseStridedConv { get; set; } = false;
		public bool UseDropout { get; set; } = false;
		public bool UseInstanceNorm { get; set; } = false;
		public bool UseInterpolationUp { get; set; } = false;
		public bool UseInterpolationDown { get; set; } = false;
		public bool UseChannelAttention { get; set; } = false;
		public bool UseAdaptiveAttentionFusion { get; set; } = false; // ToDo: Not working at the moment

		public object Clone()
		{
			return new UNetModelParameter
			{
				MaxEpochs = this.MaxEpochs,
				LearningRate = this.LearningRate,
				BatchSize = this.BatchSize,
				Features = this.Features,
				StopAtLoss = this.StopAtLoss,
				TrainImagesAsGreyscale = this.TrainImagesAsGreyscale,
				SplitTrainValidationSet = this.SplitTrainValidationSet,
				TrainPrecision = this.TrainPrecision,
				TrainOnDevice = this.TrainOnDevice,
				FirstFilterSize = this.FirstFilterSize,
				Depth = this.Depth,
				UsePooling = this.UsePooling,
				UseStridedConv = this.UseStridedConv,
				UseDropout = this.UseDropout,
				UseInstanceNorm = this.UseInstanceNorm,
				UseInterpolationUp = this.UseInterpolationUp,
				UseInterpolationDown = this.UseInterpolationDown,
				UseChannelAttention = this.UseChannelAttention,
				UseAdaptiveAttentionFusion = this.UseAdaptiveAttentionFusion
			};
		}
	}
}
