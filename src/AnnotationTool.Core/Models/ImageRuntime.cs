using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// Holds all runtimes for a single image.
    /// Encapsulates bitmaps with safe locking access.
    /// </summary>
    public sealed class ImageRuntime : IDisposable
    {
        private Bitmap fullImage { get; set; }
        private Bitmap annotation { get; set; }
        private LabelMask mask { get; set; }
        private Bitmap heatmap { get; set; }

        private readonly object annotationLock = new object();
        private readonly object heatmapLock = new object();
        private bool disposed;



        // Full image (read-only, single-threaded usage)
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

        /// <summary>
        /// Sets the full image once. Must be called before rendering starts.
        /// </summary>
        public void SetFullImage(Bitmap bitmap)
        {
            EnsureNotDisposed();

            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            if (fullImage != null)
                throw new InvalidOperationException("FullImage can only be set once.");

            fullImage = bitmap;
        }

        public void EnsureMask(int width, int height)
        {
            EnsureNotDisposed();

            if (mask == null || mask.Width != width || mask.Height != height)
            {
                mask = new LabelMask(width, height);
            }
        }

        public void EnsureAnnotation(int width, int height)
        {
            EnsureNotDisposed();

            lock (annotationLock)
            {
                if (annotation == null || annotation.Width != width || annotation.Height != height)
                {
                    annotation?.Dispose();
                    annotation = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                }
            }
        }

        public void WithAnnotation(Action<Bitmap> action)
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

        public Bitmap CloneAnnotation()
        {
            EnsureNotDisposed();

            lock (annotationLock)
            {
                return annotation == null ? null : (Bitmap)annotation.Clone();
            }
        }

        public void SetHeatmap(Bitmap bitmap)
        {
            EnsureNotDisposed();

            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            lock (heatmapLock)
            {
                heatmap?.Dispose();
                heatmap = bitmap;
            }
        }

        public void WithHeatmap(Action<Bitmap> action)
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

        public Bitmap CloneHeatmap()
        {
            EnsureNotDisposed();

            lock (heatmapLock)
            {
                return heatmap == null ? null : (Bitmap)heatmap.Clone();
            }
        }

        public bool HasFullImage => fullImage != null;

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
            fullImage?.Dispose();
            fullImage = null;
            mask = null;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ImageRuntime));
        }

    }
}
