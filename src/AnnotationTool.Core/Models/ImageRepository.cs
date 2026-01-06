using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// 
    /// Runtime service/cache (what’s in memory right now) for full-resolution images and masks, keyed by image Guid.
    /// Owns disposable resources (full-res Bitmaps, GDI), mask buffers (LabelMask) and tools (MaskCanvas).
    /// Deals with file I/O for loading images, caching, and lifecycle(Dispose).
    /// Can add thumbnails, cache eviction, thread safety, async loading later—without touching the JSON model.
    /// 
    /// </summary>
    public sealed class ImageRepository : IDisposable
    {

        public sealed class ImageRuntime
        {
            public Bitmap FullImage { get; set; }
            public Bitmap Annotation { get; set; }
            public LabelMask Mask { get; set; }
            public Bitmap Heatmap { get; set; }

            // Synchronization locks for resources that may be accessed concurrently
            public readonly object AnnotationLock = new object();
            public readonly object HeatmapLock = new object();
        }

        private readonly Dictionary<Guid, ImageRuntime> rtRepo = new Dictionary<Guid, ImageRuntime>();


        // Get (or create) the runtime bucket for an image id.
        public ImageRuntime GetRuntime(Guid id)
        {
            if (!rtRepo.TryGetValue(id, out var rt))
            {
                rt = new ImageRuntime();
                rtRepo[id] = rt;
            }
            return rt;
        }

        /// <summary>
        /// Load and cache the full-res bitmap for <paramref name="id"/> from <paramref name="absolutePath"/>.
        /// Returns an unlocked 32bpp clone and lazily ensures a LabelMask + Annotation sized to the image.
        /// </summary>
        public Bitmap EnsureFull(Guid id, string absolutePath)
        {
            var rt = GetRuntime(id);

            if (rt.FullImage != null)
                return rt.FullImage;

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Image not found", absolutePath);




            using (var tmp = Image.FromFile(absolutePath))
            {
                rt.FullImage = new Bitmap(tmp);
            }

            // Ensure annotation + mask exist
            EnsureAnnotationAndMask(rt);


            return rt.FullImage;
        }

        private static void EnsureAnnotationAndMask(ImageRuntime rt)
        {
            if (rt.FullImage == null)
                return;

            int w = rt.FullImage.Width;
            int h = rt.FullImage.Height;

            if (rt.Mask == null)
                rt.Mask = CreateEmptyMask(w, h);

            if (rt.Annotation == null)
            {
                lock (rt.AnnotationLock)
                {
                    if (rt.Annotation == null)
                        rt.Annotation = CreateAnnotationBitmap(w, h);
                }
            }
        }

        /// <summary>
        /// Ensure a mask and annotation(for visualization) exists for <paramref name="id"/> with the given size and a bound MaskCanvas.
        /// </summary>
        public (LabelMask, Bitmap) GetOrCreateAnnotationMask(Guid id, int width, int height)
        {
            var rt = GetRuntime(id);
            if (rt.Mask == null || rt.Mask.Width != width || rt.Mask.Height != height)
                rt.Mask = new LabelMask(width, height);

            if (rt.Annotation == null)
            {
                rt.Annotation = CreateAnnotationBitmap(width, height);
            }
            return (rt.Mask, rt.Annotation);
        }

        private static Bitmap CreateAnnotationBitmap(int width, int height)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        private static LabelMask CreateEmptyMask(int w, int h)
        {
            return new LabelMask(w, h);
        }

        public void SetHeatmap(Guid imageGuid, Bitmap newHeatmap)
        {
            var rt = GetRuntime(imageGuid);

            lock (rt.HeatmapLock)
            {
                rt.Heatmap?.Dispose();
                rt.Heatmap = newHeatmap;
            }
        }

        /// <summary>Dispose and forget runtime resources for a single image id.</summary>
        public void DisposeRuntime(Guid imageGuid)
        {
            ImageRuntime rt;
            if (!rtRepo.TryGetValue(imageGuid, out rt))
                return;

            lock (rt.AnnotationLock)
            {
                rt.Annotation?.Dispose();
                rt.Annotation = null;
            }

            lock (rt.HeatmapLock)
            {
                rt.Heatmap?.Dispose();
                rt.Heatmap = null;
            }

            rt.FullImage?.Dispose();
            rt.FullImage = null;

            rt.Mask = null;

            rtRepo.Remove(imageGuid);
        }

        public void Dispose()
        {
            foreach (var kv in rtRepo)
            {
                var rt = kv.Value;

                lock (rt.AnnotationLock)
                {
                    rt.Annotation?.Dispose();
                }

                lock (rt.HeatmapLock)
                {
                    rt.Heatmap?.Dispose();
                }

                rt.FullImage?.Dispose();
            }

            rtRepo.Clear();
        }
    }
}
