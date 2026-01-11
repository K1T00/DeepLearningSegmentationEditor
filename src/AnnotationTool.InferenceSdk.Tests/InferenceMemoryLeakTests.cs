using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Inference.Decoders;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Ai.Utils.ImageProcessing;
using AnnotationTool.Ai.Utils.TensorProcessing;
using AnnotationTool.Core.Models;
using OpenCvSharp;
using TorchSharp;
using Xunit;
using static TorchSharp.torch;
using static AnnotationTool.Tests.Utils;


namespace AnnotationTool.Tests
{
    public class InferenceMemoryLeakTests
    {
        [Fact]
        public void InferencePipeline_WithNormalization_DoesNotLeakMemory()
        {
            const int iterations = 200;

            // Torch allocator + cuDNN-style caches
            const long allowedApproxNativeGrowthBytes = 100 * 1024 * 1024; // 100 MB

            var imageSize = new OpenCvSharp.Size(1024, 1024);
            var roi = new Rect(0, 0, 1024, 1024);

            var space = new SegmentationImageSpace(
                imageSize,
                roi,
                sliceSize: 256,
                downSample: 0,
                borderPadding: true);

            var pre = new SegmentationPreprocessor(space);
            var post = new SegmentationPostprocessor(space);
            var decoder = new BinarySegmentationDecoder();

            var normalization = new NormalizationSettings
            {
                Mean = new[] { 0.5f },
                Std = new[] { 0.25f }
            };

            using var input = CreateTestImage(imageSize);
            using var model = nn.Identity();
            var device = CPU;

            ForceGc();
            long privateBefore = GetPrivateMemory();
            long managedBefore = GetManagedMemory();

            // ---------- Act ----------
            for (int i = 0; i < iterations; i++)
            {
                // One dispose scope per inference iteration
                using (var scope = torch.NewDisposeScope())
                {
                    var tiles = pre.ProcessImage(input);

                    using var batch = TensorConversion.SlicedImageToTensor(
                        tiles,
                        trainImagesAsGreyscale: true,
                        toDevice: device,
                        norm: normalization,
                        precision: ScalarType.Float32);

                    using var logits = model.forward(batch);
                    var decodedTiles = decoder.Decode(logits);

                    foreach (var kv in decodedTiles)
                    {
                        using var full = post.ProcessImageTiles(kv.Value);
                    }

                    ImageUtils.DisposeTiles(tiles);
                    ImageUtils.DisposePreds(decodedTiles);
                }

                if (i % 20 == 0)
                    ForceGc();
            }

            ForceGc();
            long privateAfter = GetPrivateMemory();
            long managedAfter = GetManagedMemory();

            // ---------- Assert ----------
            long privateGrowth = privateAfter - privateBefore;
            long managedGrowth = managedAfter - managedBefore;

            // Approximate native (Torch + OpenCV) growth
            long approxNativeGrowth = privateGrowth - managedGrowth;

            Assert.True(
                approxNativeGrowth < allowedApproxNativeGrowthBytes,
                $"Approx native memory leak detected: +" +
                $"{approxNativeGrowth / (1024 * 1024)} MB " +
                $"(private +{privateGrowth / (1024 * 1024)} MB, " +
                $"managed +{managedGrowth / (1024 * 1024)} MB)");
        }

    }
}