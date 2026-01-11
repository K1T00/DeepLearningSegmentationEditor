using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Utils.ImageProcessing
{
    public class ImageConversion
    {
        // Tensor of [H x W] expected
        public static unsafe Mat TensorToGreyImage(Tensor tens)
        {
            if (tens.Dimensions != 2)
                throw new ArgumentException("Tensor must have shape [H, W]");

            float[] src;

            if (tens.dtype != ScalarType.Float32)
            {
                Tensor t32 = tens.to_type(ScalarType.Float32);
                src = t32.data<float>().ToArray();
            }
            else
            {
                src = tens.data<float>().ToArray();
            }

            int h = (int)tens.shape[0];
            int w = (int)tens.shape[1];
            int count = w * h;

            Mat img = new Mat(h, w, MatType.CV_8UC1);

            byte* dst = (byte*)img.DataPointer;
            int stride = (int)img.Step();

            fixed (float* pSrc = src)
            {
                float* p = pSrc;

                for (int y = 0; y < h; y++)
                {
                    byte* row = dst + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        float v = *p++;
                        // clamp to avoid overflows
                        if (v < 0f) v = 0f;
                        if (v > 1f) v = 1f;

                        row[x] = (byte)(v * 255f);
                    }
                }
            }

            return img;
        }

        // Tensor of [3 x H x W] expected
        public static unsafe Mat TensorToRgbImage(Tensor tens)
        {
            int h = (int)tens.shape[1];
            int w = (int)tens.shape[2];
            int count = w * h;

            // Allocate OpenCV Mat in BGR format
            Mat img = new Mat(h, w, MatType.CV_8UC3);

            float[] rSrc;
            float[] gSrc;
            float[] bSrc;

            if (tens.dtype != ScalarType.Float32)
            {
                // Apply sigmoid once per channel
                Tensor r = functional.sigmoid(tens[0]).to_type(ScalarType.Float32);
                Tensor g = functional.sigmoid(tens[1]).to_type(ScalarType.Float32);
                Tensor b = functional.sigmoid(tens[2]).to_type(ScalarType.Float32);

                rSrc = r.data<float>().ToArray();
                gSrc = g.data<float>().ToArray();
                bSrc = b.data<float>().ToArray();
            }
            else
            {
                // Apply sigmoid once per channel
                rSrc = functional.sigmoid(tens[0]).data<float>().ToArray();
                gSrc = functional.sigmoid(tens[1]).data<float>().ToArray();
                bSrc = functional.sigmoid(tens[2]).data<float>().ToArray();
            }

            byte* dst = (byte*)img.DataPointer;
            int stride = (int)img.Step(); // bytes per row

            fixed (float* pR = rSrc)
            fixed (float* pG = gSrc)
            fixed (float* pB = bSrc)
            {
                float* rPtr = pR;
                float* gPtr = pG;
                float* bPtr = pB;

                for (int y = 0; y < h; y++)
                {
                    byte* row = dst + y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        // Read float in [0..1]
                        float rf = *rPtr++;
                        float gf = *gPtr++;
                        float bf = *bPtr++;

                        // Clamp for safety
                        if (rf < 0f) rf = 0f; else if (rf > 1f) rf = 1f;
                        if (gf < 0f) gf = 0f; else if (gf > 1f) gf = 1f;
                        if (bf < 0f) bf = 0f; else if (bf > 1f) bf = 1f;

                        // Write BGR (OpenCV format)
                        byte* px = row + x * 3;
                        px[0] = (byte)(bf * 255f); // B
                        px[1] = (byte)(gf * 255f); // G
                        px[2] = (byte)(rf * 255f); // R
                    }
                }
            }
            return img;
        }

        // Tensor of [H x W] expected (normalized)
        public static unsafe Mat NormalizedTensorToGreyImage(Tensor tens, NormalizationSettings norm)
        {
            if (tens.Dimensions != 2)
                throw new ArgumentException("Tensor must have shape [H, W]");

            // Ensure CPU + float32 + contiguous
            using (var cpuTensor = tens
                .to_type(ScalarType.Float32)
                .cpu()
                .contiguous())
            {
                int h = (int)cpuTensor.shape[0];
                int w = (int)cpuTensor.shape[1];
                int count = h * w;

                float mean = norm.Mean[0];
                float std = norm.Std[0];

                float[] src = cpuTensor.data<float>().ToArray();

                Mat img = new Mat(h, w, MatType.CV_8UC1);

                byte* dst = (byte*)img.DataPointer;



                int stride = (int)img.Step();

                fixed (float* pSrc = src)
                {
                    float* p = pSrc;

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = dst + y * stride;

                        for (int x = 0; x < w; x++)
                        {
                            // De-normalize
                            float v = (*p++ * std + mean) * 255f;

                            // Manual clamp (netstandard2.0-safe)
                            if (v < 0f) v = 0f;
                            else if (v > 255f) v = 255f;

                            row[x] = (byte)v;
                        }
                    }
                }

                return img;
            }
        }


        public static unsafe Mat NormalizedTensorToRgbMat(Tensor tensor, NormalizationSettings norm)
        {
            if (tensor.Dimensions != 3 || tensor.shape[0] != 3)
                throw new ArgumentException("Tensor must have shape [3, H, W]");

            // Ensure CPU + float32 + contiguous memory
            using (var cpuTensor = tensor
                .to_type(ScalarType.Float32)
                .cpu()
                .contiguous())
            {
                int h = (int)cpuTensor.shape[1];
                int w = (int)cpuTensor.shape[2];
                int count = h * w;

                // Extract tensor data
                float[] buffer = cpuTensor.data<float>().ToArray();

                int rOffset = 0;
                int gOffset = count;
                int bOffset = count * 2;

                float meanR = norm.Mean[0];
                float meanG = norm.Mean[1];
                float meanB = norm.Mean[2];

                float stdR = norm.Std[0];
                float stdG = norm.Std[1];
                float stdB = norm.Std[2];

                Mat mat = new Mat(h, w, MatType.CV_8UC3);

                byte* dstBase = (byte*)mat.DataPointer;
                int rowStride = w * 3;

                fixed (float* src = buffer)
                {
                    float* rPtr = src + rOffset;
                    float* gPtr = src + gOffset;
                    float* bPtr = src + bOffset;

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = dstBase + y * rowStride;

                        for (int x = 0; x < w; x++)
                        {
                            float r = (*rPtr++ * stdR + meanR) * 255f;
                            float g = (*gPtr++ * stdG + meanG) * 255f;
                            float b = (*bPtr++ * stdB + meanB) * 255f;

                            // Manual clamp (no Math.Clamp in netstandard2.0)
                            if (r < 0f) r = 0f;
                            else if (r > 255f) r = 255f;

                            if (g < 0f) g = 0f;
                            else if (g > 255f) g = 255f;

                            if (b < 0f) b = 0f;
                            else if (b > 255f) b = 255f;

                            int i = x * 3;

                            // OpenCV uses BGR order
                            row[i] = (byte)b;
                            row[i + 1] = (byte)g;
                            row[i + 2] = (byte)r;
                        }
                    }
                }

                return mat;
            }
        }

        public static Mat[] ClassIndexTensorToImages(Tensor t)
        {
            int n = (int)t.shape[0];
            Mat[] result = new Mat[n];

            for (int i = 0; i < n; i++)
                result[i] = TensorToGreyImage(t[i].to_type(ScalarType.Byte));

            return result;
        }

    }
}
