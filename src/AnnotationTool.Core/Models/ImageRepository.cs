using System;
using System.Collections.Generic;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// 
    /// Runtime service/cache (what’s in memory right now) for full-resolution images and masks, keyed by image Guid.
    /// Owns disposable resources (full-res Bitmaps, GDI), mask buffers (LabelMask) and tools (MaskCanvas).
    /// Deals with file I/O for loading images, caching, and lifecycle(Dispose).
    /// Can add thumbnails, cache eviction, thread safety, async loading later—without touching the JSON model.
    /// 
    ///  Owns and manages ImageRuntime instances.
    /// One ImageRuntime per logical image (Guid).
    /// Thread-safe.
    /// </summary>
    /// 
    public sealed class ImageRepository : IDisposable
    {
        private readonly Dictionary<Guid, ImageRuntime> runtimeRepo = new Dictionary<Guid, ImageRuntime>();

        private readonly object repoLock = new object();
        private bool disposed;

        /// <summary>
        /// Gets (or creates) the ImageRuntime for the given image id.
        /// Thread-safe.
        /// </summary>
        public ImageRuntime GetRuntime(Guid id)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ImageRepository));

            lock (repoLock)
            {
                if (!runtimeRepo.TryGetValue(id, out var rt))
                {
                    rt = new ImageRuntime();
                    runtimeRepo[id] = rt;
                }

                return rt;
            }
        }

        /// <summary>
        /// Removes and disposes the ImageRuntime for the given image id.
        /// Call when an image is closed or deleted.
        /// </summary>
        public bool Remove(Guid id)
        {
            if (disposed)
                return false;

            ImageRuntime rt;

            lock (repoLock)
            {
                if (!runtimeRepo.TryGetValue(id, out rt))
                    return false;

                runtimeRepo.Remove(id);
            }

            // Dispose outside lock
            rt.Dispose();
            return true;
        }

        /// <summary>
        /// Clears all ImageRuntimes and disposes them.
        /// </summary>
        public void Clear()
        {
            if (disposed)
                return;

            List<ImageRuntime> toDispose;

            lock (repoLock)
            {
                toDispose = new List<ImageRuntime>(runtimeRepo.Values);
                runtimeRepo.Clear();
            }

            // Dispose outside lock
            foreach (var rt in toDispose)
                rt.Dispose();
        }

        /// <summary>
        /// Disposes the repository and all managed ImageRuntimes.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Clear();
        }
    }

}
