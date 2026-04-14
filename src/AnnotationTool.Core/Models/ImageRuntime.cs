using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// Holds all runtime buffers for a single image.
    /// Owns all bitmaps stored inside (FullImage, Annotation, Heatmap).
    /// Encapsulates bitmaps with (thread) safe locking access.
    ///
    /// Heatmaps:
    /// - Store RAW certainty maps as 8-bit grayscale (0..255) per feature key.
    /// - Generate Turbo-colored heatmap bitmap on demand with a threshold.
    /// - Below-threshold pixels become black (renderer can color-key them to transparent).
    /// </summary>
    public sealed class ImageRuntime : IDisposable
    {
        private Bitmap fullImage;
        private Bitmap annotation;
        private LabelMask mask;
        private Bitmap heatmap;
        private readonly Dictionary<string, HeatmapRaw> heatmapRawByFeature = new Dictionary<string, HeatmapRaw>(StringComparer.OrdinalIgnoreCase);

        private readonly object annotationLock = new object();
        private readonly object maskLock = new object();
        private readonly object heatmapLock = new object();
        private readonly object heatmapDataLock = new object();

        private bool annotationLoadedOnce;
        private bool maskLoadedOnce;
        private bool disposed;

        public ImageRuntime()
        {
        }

        public bool AnnotationLoadedOnce
        {
            get
            {
                EnsureNotDisposed();
                lock (annotationLock)
                    return annotationLoadedOnce;
            }
        }

        public bool MaskLoadedOnce
        {
            get
            {
                EnsureNotDisposed();
                lock (maskLock)
                    return maskLoadedOnce;
            }
        }

        /// <summary>
        /// Dirty flags indicate in-memory edits that have not yet been persisted (saved).
        /// </summary>
        public bool IsAnnotationDirty { get; private set; }

        /// <summary>
        /// Dirty flags indicate in-memory edits that have not yet been persisted (saved).
        /// </summary>
        public bool IsMaskDirty { get; private set; }

        /// <summary>
        /// Threshold used for generating heatmaps from raw certainty. Range 0..255.
        /// </summary>
        public int HeatmapThresholdByte { get; private set; } = 0;


        public Bitmap FullImage
        {
            get
            {
                EnsureNotDisposed();

                if (this.fullImage == null)
                    throw new InvalidOperationException("FullImage not initialized.");

                return this.fullImage;
            }
        }

        public LabelMask Mask
        {
            get
            {
                EnsureNotDisposed();

                if (this.mask == null)
                    throw new InvalidOperationException("Mask not initialized.");

                return this.mask;
            }
        }

        public bool HasFullImage => this.fullImage != null;

        public bool HasHeatmap
        {
            get
            {
                lock (heatmapLock)
                    return heatmap != null;
            }
        }

        public void SetFullImage(Bitmap bitmap)
        {
            EnsureNotDisposed();
            if (fullImage != null)
                throw new InvalidOperationException("FullImage can only be set once.");

            fullImage = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }

        /// <summary>
        /// Read-only access to the annotation bitmap.
        /// </summary>
        public void ReadAnnotation(Action<Bitmap> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (annotationLock)
            {
                if (annotation == null)
                    throw new InvalidOperationException("Annotation bitmap not initialized.");

                action(annotation);
            }
        }

        /// <summary>
        /// Mutating access to the annotation bitmap.
        /// Marks IsAnnotationDirty=true.
        /// </summary>
        public void MutateAnnotation(Action<Bitmap> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (annotationLock)
            {
                if (annotation == null)
                    throw new InvalidOperationException("Annotation bitmap not initialized.");

                action(annotation);
                IsAnnotationDirty = true;
            }
        }

        /// <summary>
        /// Read-only access to the mask.
        /// </summary>
        public void ReadMask(Action<LabelMask> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (maskLock)
            {
                if (mask == null)
                    throw new InvalidOperationException("Mask not initialized.");

                action(mask);
            }
        }

        /// <summary>
        /// Mutating access to the mask.
        /// Marks IsMaskDirty=true.
        /// </summary>
        public void MutateMask(Action<LabelMask> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (maskLock)
            {
                if (mask == null)
                    throw new InvalidOperationException("Mask not initialized.");

                action(mask);
                IsMaskDirty = true;
            }
        }

        /// <summary>
        /// Read-only access to the heatmap bitmap.
        /// </summary>
        public void ReadHeatmap(Action<Bitmap> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (heatmapLock)
            {
                if (heatmap == null)
                    throw new InvalidOperationException("Heatmap not initialized.");

                action(heatmap);
            }
        }

        public void MutateHeatmap(Action<Bitmap> action)
        {
            EnsureNotDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (heatmapLock)
            {
                if (heatmap == null)
                    throw new InvalidOperationException("Heatmap bitmap not initialized.");

                action(heatmap);
            }
        }

        public void ClearHeatmap()
        {
            EnsureNotDisposed();

            lock (heatmapLock)
            {
                heatmap?.Dispose();
                heatmap = null;
            }
        }

        public void MarkAnnotationLoaded()
        {
            EnsureNotDisposed();

            lock (annotationLock)
            {
                annotationLoadedOnce = true;
            }
        }

        public void MarkMaskLoaded()
        {
            EnsureNotDisposed();

            lock (maskLock)
            {
                maskLoadedOnce = true;
            }
        }

        public void EnsureAnnotation(int width, int height)
        {
            EnsureNotDisposed();

            lock (annotationLock)
            {
                if (annotation == null)
                    annotation = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            }
        }

        public void EnsureMask(int width, int height)
        {
            EnsureNotDisposed();

            lock (maskLock)
            {
                if (mask == null)
                    mask = new LabelMask(width, height);
            }
        }

        public void EnsureHeatmap(int width, int height)
        {
            EnsureNotDisposed();

            lock (heatmapLock)
            {
                if (heatmap != null)
                    return;

                heatmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }
        }

        public void MarkAnnotationClean()
        {
            EnsureNotDisposed();
            IsAnnotationDirty = false;
        }

        public void MarkMaskClean()
        {
            EnsureNotDisposed();
            IsMaskDirty = false;
        }

        public void MarkClean()
        {
            EnsureNotDisposed();
            IsAnnotationDirty = false;
            IsMaskDirty = false;
        }

        public void SetHeatmapThreshold(int thresholdByte)
        {
            EnsureNotDisposed();
            HeatmapThresholdByte = ClampByte(thresholdByte);
        }

        public void SetHeatmapCertainty(string featureKey, byte[] certainty8u, int width, int height)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(featureKey)) throw new ArgumentException("Feature key required.", nameof(featureKey));
            if (certainty8u == null) throw new ArgumentNullException(nameof(certainty8u));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Invalid heatmap size.");
            if (certainty8u.Length != width * height)
                throw new ArgumentException("certainty8u length must equal width*height.");

            // Store a copy to avoid caller accidentally mutating the array.
            var copy = new byte[certainty8u.Length];
            Buffer.BlockCopy(certainty8u, 0, copy, 0, copy.Length);

            lock (heatmapDataLock)
            {
                heatmapRawByFeature[featureKey] = new HeatmapRaw(width, height, copy);
            }
        }

        public bool HasHeatmapCertainty(string featureKey)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(featureKey)) return false;

            lock (heatmapDataLock)
                return heatmapRawByFeature.ContainsKey(featureKey);
        }

        public void ClearHeatmapCertainty(string featureKey)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(featureKey)) return;

            lock (heatmapDataLock)
                heatmapRawByFeature.Remove(featureKey);
        }

        public void ClearAllHeatmapCertainty()
        {
            EnsureNotDisposed();
            lock (heatmapDataLock)
                heatmapRawByFeature.Clear();
        }

        /// <summary>
        /// Generates the Turbo heatmap bitmap (ARGB) from cached raw certainty for the given feature
        /// using HeatmapThresholdByte. Below threshold -> black.
        /// </summary>
        public void RegenerateHeatmapTurbo(string featureKey)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(featureKey)) return;

            HeatmapRaw raw;
            var thr = HeatmapThresholdByte;

            lock (heatmapDataLock)
            {
                if (!heatmapRawByFeature.TryGetValue(featureKey, out raw))
                    return;
            }

            EnsureHeatmap(raw.Width, raw.Height);

            // Write pixels efficiently.
            MutateHeatmap(bmp => WriteTurboHeatmapIntoBitmap(bmp, raw, thr));
        }

        private static unsafe void WriteTurboHeatmapIntoBitmap(Bitmap bmp, HeatmapRaw raw, int thresholdByte)
        {
            // bmp is 32bpp ARGB
            if (bmp.Width != raw.Width || bmp.Height != raw.Height)
                throw new InvalidOperationException("Heatmap bitmap size mismatch.");

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                var dst0 = (byte*)data.Scan0;
                var stride = data.Stride;

                var src = raw.Data; // 0..255
                var w = raw.Width;
                var h = raw.Height;

                fixed (byte* src0 = src)
                {
                    var s = src0;
                    for (var y = 0; y < h; y++)
                    {
                        var row = dst0 + y * stride;
                        var baseIdx = y * w;

                        for (var x = 0; x < w; x++)
                        {
                            var v = s[baseIdx + x];

                            if (v < thresholdByte)
                            {
                                // black
                                row[x * 4 + 0] = 0;   // B
                                row[x * 4 + 1] = 0;   // G
                                row[x * 4 + 2] = 0;   // R
                                row[x * 4 + 3] = 255; // A
                            }
                            else
                            {
                                // Turbo colormap (returns RGB in 0..255)
                                TurboColor(v, out byte r, out byte g, out byte b);
                                row[x * 4 + 0] = b;
                                row[x * 4 + 1] = g;
                                row[x * 4 + 2] = r;
                                row[x * 4 + 3] = 255;
                            }
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static void TurboColor(byte value, out byte r, out byte g, out byte b)
        {
            // Polynomial approximation of Turbo
            var x = value / 255f;

            // Coefficients for r,g,b (6 terms each)
            var rr =
                0.13572138f +
                x * (4.61539260f +
                x * (-42.66032258f +
                x * (132.13108234f +
                x * (-152.94239396f +
                x * 59.28637943f))));

            var gg =
                0.09140261f +
                x * (2.19418839f +
                x * (4.84296658f +
                x * (-14.18503333f +
                x * (4.27729857f +
                x * 2.82956604f))));

            var bb =
                0.10667330f +
                x * (12.64194608f +
                x * (-60.58204836f +
                x * (110.36276771f +
                x * (-89.90310912f +
                x * 27.34824973f))));

            // Clamp 0..1
            rr = rr < 0 ? 0 : (rr > 1 ? 1 : rr);
            gg = gg < 0 ? 0 : (gg > 1 ? 1 : gg);
            bb = bb < 0 ? 0 : (bb > 1 ? 1 : bb);

            r = (byte)(rr * 255f + 0.5f);
            g = (byte)(gg * 255f + 0.5f);
            b = (byte)(bb * 255f + 0.5f);
        }

        private static int ClampByte(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            lock (annotationLock)
            {
                annotation?.Dispose();
                annotation = null;
            }

            lock (heatmapLock)
            {
                heatmap?.Dispose();
                heatmap = null;
            }

            lock (maskLock)
            {
                mask = null;
            }

            lock (heatmapDataLock)
            {
                heatmapRawByFeature.Clear();
            }

            fullImage?.Dispose();
            fullImage = null;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ImageRuntime));
        }

        private sealed class HeatmapRaw
        {
            public int Width { get; }
            public int Height { get; }
            public byte[] Data { get; } // length = Width*Height

            public HeatmapRaw(int width, int height, byte[] data)
            {
                this.Width = width;
                this.Height = height;
                this.Data = data ?? throw new ArgumentNullException(nameof(data));
                if (data.Length != width * height)
                    throw new ArgumentException("Data length must equal width*height.");
            }
        }
    }

}
