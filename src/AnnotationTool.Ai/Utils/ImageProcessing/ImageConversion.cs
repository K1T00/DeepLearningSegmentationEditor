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
                var t32 = tens.to_type(ScalarType.Float32);
                src = t32.data<float>().ToArray();
            }
            else
            {
                src = tens.data<float>().ToArray();
            }

            var h = (int)tens.shape[0];
            var w = (int)tens.shape[1];
            var count = w * h;

            var img = new Mat(h, w, MatType.CV_8UC1);

            var dst = (byte*)img.DataPointer;
            var stride = (int)img.Step();

            fixed (float* pSrc = src)
            {
                var p = pSrc;

                for (var y = 0; y < h; y++)
                {
                    var row = dst + y * stride;

                    for (var x = 0; x < w; x++)
                    {
                        var v = *p++;
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
            var h = (int)tens.shape[1];
            var w = (int)tens.shape[2];
            var count = w * h;

            // Allocate OpenCV Mat in BGR format
            var img = new Mat(h, w, MatType.CV_8UC3);

            float[] rSrc;
            float[] gSrc;
            float[] bSrc;

            if (tens.dtype != ScalarType.Float32)
            {
                // Apply sigmoid once per channel
                var r = functional.sigmoid(tens[0]).to_type(ScalarType.Float32);
                var g = functional.sigmoid(tens[1]).to_type(ScalarType.Float32);
                var b = functional.sigmoid(tens[2]).to_type(ScalarType.Float32);

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

            var dst = img.DataPointer;
            var stride = (int)img.Step(); // bytes per row

            fixed (float* pR = rSrc)
            fixed (float* pG = gSrc)
            fixed (float* pB = bSrc)
            {
                var rPtr = pR;
                var gPtr = pG;
                var bPtr = pB;

                for (var y = 0; y < h; y++)
                {
                    var row = dst + y * stride;

                    for (var x = 0; x < w; x++)
                    {
                        // Read float in [0..1]
                        var rf = *rPtr++;
                        var gf = *gPtr++;
                        var bf = *bPtr++;

                        // Clamp for safety
                        if (rf < 0f) rf = 0f; else if (rf > 1f) rf = 1f;
                        if (gf < 0f) gf = 0f; else if (gf > 1f) gf = 1f;
                        if (bf < 0f) bf = 0f; else if (bf > 1f) bf = 1f;

                        // Write BGR (OpenCV format)
                        var px = row + x * 3;
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
            using (var cpuTensor = tens.to_type(ScalarType.Float32).cpu().contiguous())
            {
                var h = (int)cpuTensor.shape[0];
                var w = (int)cpuTensor.shape[1];
                var count = h * w;
                var mean = norm.Mean[0];
                var std = norm.Std[0];
                var src = cpuTensor.data<float>().ToArray();
                var img = new Mat(h, w, MatType.CV_8UC1);
                var dst = (byte*)img.DataPointer;
                var stride = (int)img.Step();

                fixed (float* pSrc = src)
                {
                    var p = pSrc;

                    for (var y = 0; y < h; y++)
                    {
                        var row = dst + y * stride;

                        for (var x = 0; x < w; x++)
                        {
                            // De-normalize
                            var v = (*p++ * std + mean) * 255f;

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
            using (var cpuTensor = tensor.to_type(ScalarType.Float32).cpu().contiguous())
            {
                var h = (int)cpuTensor.shape[1];
                var w = (int)cpuTensor.shape[2];
                var count = h * w;

                // Extract tensor data
                var buffer = cpuTensor.data<float>().ToArray();

                var rOffset = 0;
                var gOffset = count;
                var bOffset = count * 2;

                var meanR = norm.Mean[0];
                var meanG = norm.Mean[1];
                var meanB = norm.Mean[2];

                var stdR = norm.Std[0];
                var stdG = norm.Std[1];
                var stdB = norm.Std[2];

                var mat = new Mat(h, w, MatType.CV_8UC3);

                var dstBase = (byte*)mat.DataPointer;
                var rowStride = w * 3;

                fixed (float* src = buffer)
                {
                    var rPtr = src + rOffset;
                    var gPtr = src + gOffset;
                    var bPtr = src + bOffset;

                    for (var y = 0; y < h; y++)
                    {
                        var row = dstBase + y * rowStride;

                        for (var x = 0; x < w; x++)
                        {
                            var r = (*rPtr++ * stdR + meanR) * 255f;
                            var g = (*gPtr++ * stdG + meanG) * 255f;
                            var b = (*bPtr++ * stdB + meanB) * 255f;

                            // Manual clamp (no Math.Clamp in netstandard2.0)
                            if (r < 0f) r = 0f;
                            else if (r > 255f) r = 255f;

                            if (g < 0f) g = 0f;
                            else if (g > 255f) g = 255f;

                            if (b < 0f) b = 0f;
                            else if (b > 255f) b = 255f;

                            var i = x * 3;

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
            var n = (int)t.shape[0];
            var result = new Mat[n];

            for (var i = 0; i < n; i++)
                result[i] = TensorToGreyImage(t[i].to_type(ScalarType.Byte));

            return result;
        }

    }
}
