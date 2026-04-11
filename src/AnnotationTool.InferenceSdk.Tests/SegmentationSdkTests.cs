using AnnotationTool.InferenceSdk;
using OpenCvSharp;
using Xunit;
using static AnnotationTool.Tests.Utils;

namespace AnnotationTool.Tests
{
    public class SegmentationSdkTests
    {
        private const string modelTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Models\Model_2026_04_11_15_16_26.bin";
        private const string settingsTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Models\Trainingsettings_2026_04_11_15_16_26.json";
        private const string imageTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Images\27b4b6d4-4833-411c-a02f-75153221e913.png";



        [Fact]
        public void LoadAndRun_DoesNotThrow()
        {
            using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);
            Assert.NotNull(session);
        }

        [Fact]
        public void Load_InvalidSettingsJson_Throws()
        {
            var badJson = Path.GetTempFileName();
            File.WriteAllText(badJson, "{ not valid json }");

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    SegmentationInferenceSession.Load(
                        modelTestFile,
                        badJson,
                        InferenceDevice.Cpu));
            }
            finally
            {
                File.Delete(badJson);
            }
        }

        [Fact]
        public void Run_ReturnsAtLeastOneClass()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            var result = session.Run(image);

            // binary → { 1 }
            // multiclass → { 1..N }
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Run_OutputMasksMatchImageSize()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            var result = session.Run(image);

            foreach (var mask in result.Values)
            {
                Assert.Equal(image.Width, mask.Width);
                Assert.Equal(image.Height, mask.Height);
                mask.Dispose();
            }
        }

        [Fact]
        public void Run_SameImageTwice_ProducesConsistentKeysAndSizes()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            var r1 = session.Run(image);
            var r2 = session.Run(image);

            Assert.Equal(r1.Keys, r2.Keys);

            foreach (var k in r1.Keys)
            {
                Assert.Equal(r1[k].Size(), r2[k].Size());
                r1[k].Dispose();
                r2[k].Dispose();
            }
        }

        [Fact]
        public void Run_EmptyImage_Throws()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var empty = new Mat();

            Assert.Throws<ArgumentException>(() => session.Run(empty));
        }

        [Fact]
        public void Run_InvalidRoi_Throws()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            var badRoi = new Rect(-10, 0, 50, 50);

            Assert.Throws<ArgumentException>(() => session.Run(image, badRoi));
        }

        [Fact]
        public void Run_Repeatedly_DoesNotThrowOrDegrade()
        {
            using var session = SegmentationInferenceSession.Load(
                modelTestFile,
                settingsTestFile,
                InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            for (int i = 0; i < 50; i++)
            {
                var result = session.Run(image);
                foreach (var mat in result.Values)
                    mat.Dispose();
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);

            session.Dispose();
            session.Dispose(); // should not throw
        }

        [Fact]
        public void Run_MultipleTimes_DoesNotLeakMemory()
        {
            const int iterations = 100;

            // - Torch CPU allocator
            // - OpenCV internal buffers
            // - one-time warmup
            const long allowedApproxNativeGrowthBytes = 150 * 1024 * 1024; // 150 MB

            using var session =
                SegmentationInferenceSession.Load(
                    modelTestFile,
                    settingsTestFile,
                    InferenceDevice.Cpu);

            using var image = Cv2.ImRead(imageTestFile);

            ForceGc();
            long privateBefore = GetPrivateMemory();
            long managedBefore = GetManagedMemory();

            // ---------- Act ----------
            for (int i = 0; i < iterations; i++)
            {
                var result = session.Run(image);

                foreach (var mat in result.Values)
                    mat.Dispose();

                if (i % 10 == 0)
                    ForceGc();
            }

            ForceGc();
            long privateAfter = GetPrivateMemory();
            long managedAfter = GetManagedMemory();

            // ---------- Assert ----------
            long privateGrowth = privateAfter - privateBefore;
            long managedGrowth = managedAfter - managedBefore;

            long approxNativeGrowth = privateGrowth - managedGrowth;

            Assert.True(
                approxNativeGrowth < allowedApproxNativeGrowthBytes,
                $"SDK inference leaked memory above threshold.\n" +
                $"Approx native growth: {approxNativeGrowth / (1024 * 1024)} MB\n" +
                $"Private growth: {privateGrowth / (1024 * 1024)} MB\n" +
                $"Managed growth: {managedGrowth / (1024 * 1024)} MB");
        }

    }
}
