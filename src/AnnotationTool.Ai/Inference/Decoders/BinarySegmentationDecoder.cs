using AnnotationTool.Core.Models;
using OpenCvSharp;
using static TorchSharp.torch;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using System.Collections.Generic;
using System.Linq;

namespace AnnotationTool.Ai.Inference.Decoders
{
    /// <summary>
    /// Binary segmentation decoder.
    ///
    /// Responsibilities:
    /// - Decode logits into probability tiles (sigmoid)
    /// - Compute per-feature (binary) segmentation metrics
    ///
    /// Background is implicit.
    /// </summary>
    public sealed class BinarySegmentationDecoder : ISegmentationDecoder
    {
        // Binary case → single foreground feature (id = 1)
        const int featureId = 1;

        public BinarySegmentationDecoder()
        {
        }

        /// <summary>
        /// Decodes logits into probability tiles (working space).
        /// Returns exactly one entry:
        ///   key = featureId (usually 1)
        ///   value = Mat[] tiles
        /// </summary>
        public Dictionary<int, Mat[]> Decode(Tensor logits)
        {
            // logits: [N,1,H,W]
            using (var probs = sigmoid(logits).cpu())
            {
                var predSlices = TensorTo2DArray(probs);
                return new Dictionary<int, Mat[]>() { { featureId, SlicedImageTensorToImage(predSlices) } };
            }
        }

        public Dictionary<int, SegmentationStats> ComputeMetrics(Dictionary<int, Mat> fullMaskPredictions, Mat groundTruth)
        {
            return
                new Dictionary<int, SegmentationStats>() { {
                        featureId,
                        ComputeBinaryMetrics(fullMaskPredictions.FirstOrDefault().Value, groundTruth) } };
        }

        public void Dispose() { }

    }
}
