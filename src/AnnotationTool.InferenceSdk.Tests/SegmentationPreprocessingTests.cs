using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Ai.Utils.ImageProcessing;
using OpenCvSharp;
using static AnnotationTool.Tests.Utils;
using Xunit;

namespace AnnotationTool.Tests
{
    [CollectionDefinition("NonParallelMemoryTests", DisableParallelization = true)]
    public class SegmentationPreprocessingTests
    {
        [Fact]
        public void PreprocessingAndSlicing_DoesNotLeakNativeMemory()
        {
            const int iterations = 10;
            const long allowedGrowthBytes = 10 * 1024 * 1024; // 10 MB tolerance

            var imageSize = new Size(8000, 8000);
            var roi = new Rect(512, 512, 7000, 7000);

            var space = new SegmentationImageSpace(
                imageSize,
                roi,
                sliceSize: 128,
                downSample: 0,
                borderPadding: true);

            var preprocessor = new SegmentationPreprocessor(space);

            using var input = new Mat(imageSize, MatType.CV_8UC3);
            input.SetTo(Scalar.All(127));

            ForceGc();
            long memBefore = GetPrivateMemory();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var tiles = preprocessor.ProcessImage(input);

                // Simulate training pipeline usage
                foreach (var tile in tiles)
                {
                    // touch data to force allocation paths
                    Cv2.Mean(tile);
                }

                // Must fully release native memory
                ImageUtils.DisposeTiles(tiles);

                if (i % 20 == 0)
                    ForceGc();
            }

            ForceGc();
            long memAfter = GetPrivateMemory();

            //  Assert
            long growth = memAfter - memBefore;

            Assert.True(
                growth < allowedGrowthBytes,
                $"Native memory leak detected: grew by {growth / (1024 * 1024)} MB");
        }
    }
}
