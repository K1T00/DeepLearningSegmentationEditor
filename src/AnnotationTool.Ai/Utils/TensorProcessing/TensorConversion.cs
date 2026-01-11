using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using TorchSharp;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageConversion;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Utils.TensorProcessing
{
    public static class TensorConversion
    {

        public static unsafe Tensor RgbMatToNormalizedTensor(Mat image, Device device, ScalarType dtype, NormalizationSettings norm)
        {
            if (image.Type() != MatType.CV_8UC3)
                throw new ArgumentException("Expected CV_8UC3 Mat");

            int h = image.Rows;
            int w = image.Cols;
            int count = h * w;

            float meanR = norm.Mean[0];
            float meanG = norm.Mean[1];
            float meanB = norm.Mean[2];

            float invStdR = 1f / norm.Std[0];
            float invStdG = 1f / norm.Std[1];
            float invStdB = 1f / norm.Std[2];

            float inv255 = 1f / 255f;

            // Allocate managed buffer: [R | G | B] planes
            float[] buffer = new float[count * 3];

            byte* srcBase = (byte*)image.DataPointer;

            // Use OpenCV row stride
            int stride = (int)image.Step();

            fixed (float* dst = buffer)
            {
                float* rPtr = dst;
                float* gPtr = dst + count;
                float* bPtr = dst + count * 2;

                for (int y = 0; y < h; y++)
                {
                    byte* row = srcBase + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 3;

                        // OpenCV is BGR
                        float vb = row[i] * inv255;
                        float vg = row[i + 1] * inv255;
                        float vr = row[i + 2] * inv255;

                        *rPtr++ = (vr - meanR) * invStdR;
                        *gPtr++ = (vg - meanG) * invStdG;
                        *bPtr++ = (vb - meanB) * invStdB;
                    }
                }
            }
            using (var scope = NewDisposeScope())
            {
                // Create tensor [3, H, W]
                return tensor(buffer, dtype: dtype).reshape(3, h, w).to(device).MoveToOuterDisposeScope();
            }
        }


        public static unsafe Tensor GreyMatToNormalizedTensor(Mat image, Device device, ScalarType precision, NormalizationSettings norm)
        {
            int h = image.Rows;
            int w = image.Cols;
            int count = w * h;

            float[] buffer = new float[count];

            const float inv255 = 1f / 255f;

            float mean = norm.Mean[0];
            float invStd = 1f / norm.Std[0];

            byte* src = (byte*)image.DataPointer;

            fixed (float* dst = buffer)
            {
                float* gPtr = dst;

                // For CV_8UC1, stride == w in most cases,
                // but Step() is ALWAYS correct and handles alignment.

                int stride = (int)image.Step();

                for (int y = 0; y < h; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        float v = row[x] * inv255;
                        *gPtr++ = (v - mean) * invStd;
                    }
                }
            }

            using (var scope = NewDisposeScope())
            {
                return tensor(buffer, dtype: precision, device: device).reshape(1, h, w).MoveToOuterDisposeScope();
            }
        }

        // Each pixel is either 0 or 1: Bernoulli probability: p(y = 1) ∈ {0.0, 1.0}
        public static unsafe Tensor BinaryMaskToTensor(Mat img, Device device)
        {
            if (img.Type() != MatType.CV_8UC1)
                throw new ArgumentException("Mask must be CV_8UC1 (single channel 8-bit)");

            int h = img.Rows;
            int w = img.Cols;
            int count = h * w;

            float[] buffer = new float[count];

            // Raw pointer to grayscale mask (1 byte per pixel)
            byte* src = (byte*)img.DataPointer;

            fixed (float* dst = buffer)
            {
                float* pDst = dst;

                int stride = (int)img.Step();  // Safe to cast for typical image sizes

                for (int y = 0; y < h; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        // Convert {0,1} → {0f,1f}
                        *pDst++ = row[x] > 0 ? 1f : 0f;
                    }
                }
            }
            using (var scope = NewDisposeScope())
            {
                // [1, H, W]
                return tensor(buffer, dtype: ScalarType.Float32, device: device).reshape(1, h, w).MoveToOuterDisposeScope();
            }
        }

        // Each pixel is class index: Categorical decision: class_id ∈ {0, 1, 2, …, C−1}
        public static unsafe Tensor MulticlassMaskToTensor(Mat img, Device device)
        {
            if (img.Type() != MatType.CV_8UC1)
                throw new ArgumentException("Mask must be CV_8UC1 (single channel 8-bit)");

            int h = img.Rows;
            int w = img.Cols;
            int count = h * w;

            long[] buffer = new long[count];

            // Raw pointer to grayscale mask (1 byte per pixel)
            byte* src = (byte*)img.DataPointer;

            fixed (long* dst = buffer)
            {
                long* pDst = dst;

                int stride = (int)img.Step();  // Safe to cast for typical image sizes

                for (int y = 0; y < h; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        // Class ids
                        *pDst++ = row[x];
                    }
                }
            }
            using (var scope = NewDisposeScope())
            {
                // [H, W]
                return tensor(buffer, ScalarType.Int64, device: device).reshape(h, w).MoveToOuterDisposeScope();
            }
        }

        // Tensor of [A x B x H x W] to tensor of [A][B][H x W]
        public static Tensor[][] TensorTo2DArray(Tensor tens)
        {
            if (tens.Dimensions != 4)
                throw new ArgumentException("Tensor must have shape [A, B, H, W]");

            using (var scope = NewDisposeScope())
            {
                long A = tens.shape[0];
                long B = tens.shape[1];

                Tensor[][] result = new Tensor[A][];

                for (int a = 0; a < A; a++)
                {
                    result[a] = new Tensor[B];

                    Tensor sliceA = tens[a]; // [B, H, W]

                    for (int b = 0; b < B; b++)
                    {
                        result[a][b] = sliceA[b].contiguous().MoveToOuterDisposeScope(); // [H, W]
                    }
                }
                return result;
            }
        }

        // Images[] -> Tensor:[Images.Length x 1 x Images.Width x Images.Height]
        public static Tensor SlicedImageToTensor(Mat[] images, bool trainImagesAsGreyscale, Device toDevice, NormalizationSettings norm, ScalarType precision)
        {
            using (var scope = NewDisposeScope())
            {
                var N = images.Length;

                // No parallel tasks needed here.
                Tensor[] outputs = new Tensor[N];

                for (int i = 0; i < N; i++)
                {
                    if (trainImagesAsGreyscale)
                    {
                        // shape: [1, H, W]
                        outputs[i] = GreyMatToNormalizedTensor(images[i], toDevice, precision, norm).unsqueeze(0);
                    }
                    else
                    {
                        // shape: [3, H, W]
                        outputs[i] = RgbMatToNormalizedTensor(images[i], toDevice, precision, norm).unsqueeze(0);
                    }
                }

                // Stack along batch dimension → [N, C, H, W]
                return cat(outputs, 0).MoveToOuterDisposeScope();
            }
        }

        // Result prediction tensor is always of type grey and needs to be converted back to an image
        public static Mat[] SlicedImageTensorToImage(Tensor[][] slicedImageTensor)
        {
            int N = slicedImageTensor.Length;
            Mat[] results = new Mat[N];

            for (int i = 0; i < N; i++)
            {
                // pred[i][0] is a [H x W] tensor for the mask
                results[i] = TensorToGreyImage(slicedImageTensor[i][0]);
            }

            return results;
        }

    }
}
