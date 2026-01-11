using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Ai.Utils.ImageProcessing;
using OpenCvSharp;
using Xunit;
using static AnnotationTool.Tests.Utils;


namespace AnnotationTool.Tests
{
    public class SegmentationPrePostProcessorTests
    {
        [Fact]
        public void Preprocess_Then_Postprocess_ReconstructsOriginalInsideRoi()
        {
            // ---------- Arrange ----------
            var imageSize = new Size(512, 384);
            var roi = new Rect(64, 32, 320, 256);

            var space = new SegmentationImageSpace(
                imageSize,
                roi,
                sliceSize: 64,
                downSample: 0,
                borderPadding: true);

            var pre = new SegmentationPreprocessor(space);
            var post = new SegmentationPostprocessor(space);

            using var original = CreateTestPattern(imageSize);

            // ---------- Act ----------
            var tiles = pre.ProcessImage(original);

            var reconstructed = post.ProcessImageTiles(tiles);

            // ---------- Assert ----------
            // Outside ROI must be zero
            using (var outside = reconstructed.Clone())
            {
                outside[roi].SetTo(0);
                Assert.Equal(0, Cv2.CountNonZero(outside));
            }

            // Inside ROI must match original
            using (var origRoi = new Mat(original, roi))
            using (var reconRoi = new Mat(reconstructed, roi))
            {
                using var diff = new Mat();
                Cv2.Absdiff(origRoi, reconRoi, diff);

                Assert.Equal(0, Cv2.CountNonZero(diff));
            }

            ImageUtils.DisposeTiles(tiles);
            reconstructed.Dispose();
        }
    }
}