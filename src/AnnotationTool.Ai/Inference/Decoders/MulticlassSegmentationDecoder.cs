using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Inference.Decoders
{
    /// <summary>
    /// Multiclass segmentation decoder.
    ///
    /// Responsibilities:
    /// - Decode logits into per-class probability tiles (softmax)
    /// - Skip background channel
    /// - Compute multiclass metrics from reconstructed probability maps
    ///
    /// Visualization and saving are handled elsewhere.
    /// </summary>
    public sealed class MulticlassSegmentationDecoder : ISegmentationDecoder
    {
        private readonly int numClasses; // includes background (class 0)

        public MulticlassSegmentationDecoder(int numClasses)
        {
            if (numClasses < 2) // Binary case should use BinarySegmentationDecoder
                throw new ArgumentOutOfRangeException(nameof(numClasses));

            this.numClasses = numClasses;
        }

        /// <summary>
        /// Decodes logits into probability tiles (working space).
        /// Returns one Mat[] per feature class (background skipped).
        /// </summary>
        public Dictionary<int, Mat[]> Decode(Tensor logits)
        {
            using (var scope = NewDisposeScope())
            {
                // logits: [N, C, H, W]
                using (var probs = softmax(logits, dim: 1).cpu())
                {
                    var result = new Dictionary<int, Mat[]>();

                    // Skip background class 0
                    for (int classId = 1; classId < numClasses; classId++)
                    {
                        // Select probability map for classId → [N,H,W] with unsqueeze for [N,1,H,W]
                        using (var classProb = probs.select(1, classId).unsqueeze(1))
                        {
                            var predSlices = TensorTo2DArray(classProb);
                            result.Add(classId, SlicedImageTensorToImage(predSlices));
                        }
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Computes multiclass segmentation metrics from full-size probability maps.
        /// </summary>
        public Dictionary<int, SegmentationStats> ComputeMetrics(Dictionary<int, Mat> fullMaskPredictions, Mat groundTruth)
        {

            var result = new Dictionary<int, SegmentationStats>();

            foreach (var kv in fullMaskPredictions)
            {
                var classId = kv.Key;      // 1..N
                var prediction = kv.Value;

                // Build binary GT mask for this class
                using (var gtBinary = new Mat())
                {
                    Cv2.Compare(groundTruth, new OpenCvSharp.Scalar(classId), gtBinary, CmpType.EQ);

                    // gtBinary is 255 for classId, 0 otherwise
                    result[classId] = ComputeBinaryMetrics(prediction, gtBinary);
                }
            }

            return result;
        }

        public void Dispose() { }
    }
}
