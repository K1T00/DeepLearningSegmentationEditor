using OpenCvSharp;
using System.Diagnostics;
using System.Runtime;

namespace AnnotationTool.Tests
{
    public static class Utils
    {
        public static Mat CreateTestImage(OpenCvSharp.Size size)
        {
            var img = new Mat(size, MatType.CV_8UC1);
            Cv2.Randu(img, 0, 255);
            return img;
        }

        /// <summary>
        /// Creates a deterministic, non-uniform image.
        /// Uniform images can hide tiling bugs.
        /// </summary>
        public static Mat CreateTestPattern(Size size)
        {
            var img = new Mat(size, MatType.CV_8UC1);

            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    img.Set(y, x, (byte)((x * 31 + y * 17) & 0xFF));
                }
            }
            return img;
        }

        public static Mat CreateImageWithNonZeroRoi(Size size, Rect roi, byte value = 255)
        {
            var img = new Mat(size, MatType.CV_8UC1, Scalar.All(0));

            using (var roiMat = new Mat(img, roi))
            {
                roiMat.SetTo(value);
            }

            return img;
        }

        public static long GetManagedMemory()
        {
            // After ForceGc(), this is a good proxy for managed heap size.
            return GC.GetTotalMemory(forceFullCollection: false);
        }

        public static void ForceGc()
        {
            // Compact LOH once (helps a lot with float[] churn)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        public static long GetPrivateMemory()
        {
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }
    }
}
