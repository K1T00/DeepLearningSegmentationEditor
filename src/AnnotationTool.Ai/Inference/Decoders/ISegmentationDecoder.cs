using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Inference.Decoders
{

    public interface ISegmentationDecoder : IDisposable
    {
        /// <summary>
        /// Decodes logits into probability tiles in WORKING space.
        ///
        /// Returns one Mat[] per semantic channel.
        /// </summary>
        Dictionary<int, Mat[]> Decode(Tensor logits);

        /// <summary>
        /// Compute segmentation metrics.
        /// </summary>
        Dictionary<int, SegmentationStats> ComputeMetrics(Dictionary<int, Mat> fullMaskPredictions, Mat groundTruth);
    }

}
