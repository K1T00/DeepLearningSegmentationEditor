using AnnotationTool.Ai.Geometry;
using OpenCvSharp;
using System;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageComposition;

namespace AnnotationTool.Ai.Processing
{
    /// <summary>
    /// Executes the reverse segmentation pipeline:
    ///
    /// tiles → merge → upsample → paste ROI → full image
    ///
    /// Symmetry with SegmentationPreprocessor.
    /// </summary>
    public sealed class SegmentationPostprocessor
    {
        private readonly SegmentationImageSpace space;

        public SegmentationPostprocessor(SegmentationImageSpace space)
        {
            this.space = space ?? throw new ArgumentNullException(nameof(space));
        }

        /// <summary>
        /// Reconstructs a full-size prediction from model-space tiles.
        /// Caller owns the returned Mat and must dispose it.
        /// </summary>
        public Mat ProcessImageTiles(Mat[] predTiles)
        {
            using (var predMerged = MergeImages(predTiles, space.TileRows, space.TileCols))
            {
                return space.ReconstructToOriginal(predMerged);
            }
        }
    }
}
