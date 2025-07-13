using TorchSharp;
using static TorchSharp.torch;

namespace AiOps.AiUtils
{
    /// <summary>
    /// Hyper-parameter
    /// </summary>
    public interface IHyperParameter
    {
        int MaxEpochs { get; set; }
        double LearningRate { get; set; }
        int BatchSize { get; set; }
        int Features { get; set; }
        float StopAtLoss { get; set; }
        bool TrainImagesAsGreyscale { get; set; }
        float SplitTrainValidationSet { get; set; }
        ScalarType TrainPrecision { get; set; }
        DeviceType TrainOnDevice { get; set; }
        int FirstFilterSize { get; set; }
		int Depth { get; set; }
        bool UsePooling { get; set; }
        bool UseStridedConv { get; set; }
        bool UseDropout { get; set; }
        bool UseInstanceNorm { get; set; }
        bool UseInterpolationUp { get; set; }
        bool UseInterpolationDown { get; set; }
        bool UseChannelAttention { get; set; }
        bool UseAdaptiveAttentionFusion { get; set; }

		
	}
}

