using AnnotationTool.Ai.Geometry;
using OpenCvSharp;
using System;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;

namespace AnnotationTool.Ai.Processing
{
    /// <summary>
    /// Executes the forward segmentation preprocessing pipeline:
    ///
    /// Original → ROI → DownSample → slice
    ///
    /// Symmetry with SegmentationPostprocessor.
    /// </summary>
    public sealed class SegmentationPreprocessor
    {
        private readonly SegmentationImageSpace space;

        public SegmentationPreprocessor(SegmentationImageSpace space)
        {
            this.space = space ?? throw new ArgumentNullException(nameof(space));
        }

        /// <summary>
        /// Preprocesses an input image/maskGt into tiles that should be converted to tensor and fed to the model.
        /// </summary>
        public Mat[] ProcessImage(Mat original)
        {
            using (var roi = space.ExtractRoi(original))
            using (var down = DownSampleImage(roi, space.DownSample))
            {
                var tiles = SliceImage(down, space.SliceSize, space.BorderPadding, space.TileRows, space.TileCols);

                for (var i = 0; i < tiles.Length; i++)
                {
                    var owned = tiles[i].Clone();
                    tiles[i].Dispose();
                    tiles[i] = owned;
                }

                return tiles;
            }
        }
    }
}