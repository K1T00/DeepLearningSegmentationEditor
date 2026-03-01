using System;
using System.Collections.Generic;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// Runtime cache for ImageRuntime instances keyed by image Guid.
    /// Fixed-size FIFO cache: when capacity is exceeded, the oldest inserted entry is evicted and disposed.
    /// Thread-safe via lock.
    /// 
    /// IMPORTANT: Includes event that raises before disposing an evicted ImageRuntime, allowing subscribers to save any pending changes before eviction.
    /// </summary>
    public sealed class ImageRepository : IDisposable
    {
        private readonly int capacity;
        private readonly Dictionary<Guid, ImageRuntime> runtimeRepo;
        private readonly LinkedList<Guid> fifoOrder = new LinkedList<Guid>();
        private readonly Dictionary<Guid, LinkedListNode<Guid>> fifoNodes;
        private readonly object repoLock = new object();
        private bool disposed;

        /// <summary>
        /// Raised immediately before a runtime is disposed due to eviction/remove/clear.
        /// Raised OUTSIDE the repo lock.
        /// Subscriber (e.g. ProjectPresenter) can persist dirty mask/annotation here.
        /// </summary>
        public event Action<Guid, ImageRuntime> RuntimeEvicting;

        public ImageRepository(int capacity = 200)
        {
            this.capacity = capacity;
            this.runtimeRepo = new Dictionary<Guid, ImageRuntime>(capacity);
            this.fifoNodes = new Dictionary<Guid, LinkedListNode<Guid>>(capacity);
        }

        /// <summary>
        /// Gets (or creates) the ImageRuntime for the given image item DTO.
        /// Ensures mask and annotation exist.
        /// </summary>
        public ImageRuntime GetRuntime(ImageItem imageItem)
        {
            if (imageItem == null) throw new ArgumentNullException(nameof(imageItem));

            return GetOrCreate(imageItem.Guid, () =>
            {
                var rt = new ImageRuntime();

                rt.EnsureMask(imageItem.ImageSize.Width, imageItem.ImageSize.Height);
                rt.EnsureAnnotation(imageItem.ImageSize.Width, imageItem.ImageSize.Height);

                return rt;
            });
        }

        public bool TryGetRuntime(Guid id, out ImageRuntime runtime)
        {
            ThrowIfDisposed();

            lock (repoLock)
            {
                return runtimeRepo.TryGetValue(id, out runtime);
            }
        }

        public bool Remove(Guid id)
        {
            if (disposed)
                return false;

            ImageRuntime rt = null;

            lock (repoLock)
            {
                if (!runtimeRepo.TryGetValue(id, out rt))
                    return false;

                runtimeRepo.Remove(id);

                if (fifoNodes.TryGetValue(id, out var node))
                {
                    fifoNodes.Remove(id);
                    fifoOrder.Remove(node);
                }
            }

            // Notify + Dispose outside lock
            NotifyEvicting(id, rt);
            rt.Dispose();
            return true;
        }

        public void Clear()
        {
            if (disposed)
                return;

            List<(Guid id, ImageRuntime rt)> toDispose;

            lock (repoLock)
            {
                toDispose = new List<(Guid, ImageRuntime)>(runtimeRepo.Count);
                foreach (var kvp in runtimeRepo)
                    toDispose.Add((kvp.Key, kvp.Value));

                runtimeRepo.Clear();
                fifoOrder.Clear();
                fifoNodes.Clear();
            }

            // Notify + Dispose outside lock
            foreach (var e in toDispose)
            {
                NotifyEvicting(e.id, e.rt);
                e.rt.Dispose();
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
        }

        /// <summary>
        /// Gets an existing runtime or creates/inserts one if missing.
        /// If insertion exceeds capacity, evicts the oldest inserted runtime.
        /// </summary>
        private ImageRuntime GetOrCreate(Guid id, Func<ImageRuntime> factory)
        {
            ThrowIfDisposed();
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            // Fast path: check existing
            lock (repoLock)
            {
                if (runtimeRepo.TryGetValue(id, out var existing))
                    return existing;
            }

            // Create outside lock
            ImageRuntime created = null;
            try
            {
                created = factory();
            }
            catch
            {
                created?.Dispose();
                throw;
            }

            ImageRuntime result;
            List<(Guid id, ImageRuntime rt)> toDispose = null;

            lock (repoLock)
            {
                // Another thread might have created it while we were outside the lock.
                if (runtimeRepo.TryGetValue(id, out var existing))
                {
                    result = existing;
                }
                else
                {
                    result = created;
                    created = null;

                    runtimeRepo[id] = result;
                    var node = fifoOrder.AddLast(id);
                    fifoNodes[id] = node;

                    while (runtimeRepo.Count > capacity)
                    {
                        var oldestNode = fifoOrder.First;
                        if (oldestNode == null)
                            break;

                        var oldestId = oldestNode.Value;

                        fifoOrder.RemoveFirst();
                        fifoNodes.Remove(oldestId);

                        if (runtimeRepo.TryGetValue(oldestId, out var evicted))
                        {
                            runtimeRepo.Remove(oldestId);

                            if (toDispose == null) toDispose = new List<(Guid, ImageRuntime)>(1);
                            toDispose.Add((oldestId, evicted));
                        }
                    }
                }
            }

            // If we lost the race and didn't insert created, dispose it
            created?.Dispose();

            // Notify + dispose evicted runtimes outside lock
            if (toDispose != null)
            {
                foreach (var e in toDispose)
                {
                    NotifyEvicting(e.id, e.rt);
                    e.rt.Dispose();
                }
            }

            return result;
        }

        private void NotifyEvicting(Guid id, ImageRuntime rt)
        {
            try
            {
                RuntimeEvicting?.Invoke(id, rt);
            }
            catch
            {
                // Important: eviction must not be blocked by subscriber failures.
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ImageRepository));
        }


    }
}
