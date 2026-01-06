using OpenCvSharp;
using Xunit;

namespace AnnotationTool.InferenceSdk.Tests
{
    public class SegmentationInferenceSessionSdkTests
    {
        private const string modelTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Models\Model_2026_01_06_09_28_28.bin";
        private const string settingsTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Models\Trainingsettings_2026_01_06_09_28_28.json";
        private const string imageTestFile = @"D:\_DEV\Ai\TestDeepLearningSdk\TestData\Images\27b4b6d4-4833-411c-a02f-75153221e913.png";


        [Fact]
        public void LoadAndRun_DoesNotThrow()
        {
            using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);
            using var image = Cv2.ImRead(imageTestFile);
            var result = session.Run(image);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);

            session.Dispose();
            session.Dispose(); // should not throw
        }

        [Fact]
        public void Run_MultipleTimes_DoesNotLeak()
        {
            using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);
            using var image = Cv2.ImRead(imageTestFile);

            for (int i = 0; i < 100; i++)
            {
                var result = session.Run(image);

                foreach (var mat in result.Values)
                    mat.Dispose();
            }
        }


        //[Fact]
        //public void Run_ReturnsFullResolutionMasks()
        //{
        //    using var session = SegmentationInferenceSession.Load(modelTestFile, settingsTestFile, InferenceDevice.Cpu);
        //    using var image = Cv2.ImRead(imageTestFile);
        //    var result = session.Run(image);

        //    foreach (var mat in result.Values)
        //    {
        //        mat.Rows.Should().Be(image.Rows);
        //        mat.Cols.Should().Be(image.Cols);
        //        mat.Type().Should().Be(MatType.CV_32FC1);
        //    }
        //}

        //[Fact]
        //public void Run_WithRoi_OnlyWritesInsideRoi()
        //{
        //    using var session = LoadSession();
        //    using var image = LoadTestImage();

        //    var roi = new Rect(16, 16, 32, 32);
        //    var result = session.Run(image, roi);

        //    foreach (var mat in result.Values)
        //    {
        //        var outside = mat.Clone();
        //        outside[roi].SetTo(0);

        //        Cv2.CountNonZero(outside).Should().Be(0);
        //    }
        //}

        //[Fact]
        //public void Multiclass_ReturnsOneMaskPerClass()
        //{
        //    using var session = LoadMulticlassSession();
        //    using var image = LoadTestImage();

        //    var result = session.Run(image);

        //    result.Keys.Should().BeEquivalentTo(new[] { 1, 2 });
        //}







    }
}
