using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace AnnotationTool.Core.Models
{
	/// <summary>
	/// 
	/// runtime service/cache (what’s in memory right now).
	/// Owns disposable stuff (full-res Bitmaps), mask buffers (LabelMask) and tools (MaskCanvas).
	/// Deals with file I/O for loading images, caching, and lifecycle(Dispose).
	/// Can add thumbnails, cache eviction, thread safety, async loading later—without touching the JSON model.
	/// Runtime cache for full-resolution images and masks, keyed by image Guid.
	/// Owns disposable GDI resources; callers supply absolute file paths.
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
		}

        private readonly Dictionary<Guid, ImageRuntime> rtRepo = new Dictionary<Guid, ImageRuntime>();


		// Get (or create) the runtime bucket for an image id.
		public ImageRuntime GetRuntime(Guid id)
		{
			if (!rtRepo.TryGetValue(id, out var r))
			{
				r = new ImageRuntime();
				rtRepo[id] = r;
			}
			return r;
		}

		/// <summary>
		/// Load and cache the full-res bitmap for <paramref name="id"/> from <paramref name="absolutePath"/>.
		/// Returns an unlocked 32bpp clone and lazily ensures a LabelMask + Annotation sized to the image.
		/// </summary>
		public Bitmap EnsureFull(Guid id, string absolutePath)
		{
			var rt = GetRuntime(id);
			if (rt.FullImage == null)
			{
                using (var src = Image.FromFile(absolutePath))
                {
                    var clone = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(clone))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height));
                    }

                    rt.FullImage = clone;

                    if (rt.Mask == null)
                    {
                        rt.Mask = new LabelMask(src.Width, src.Height);
                    }

                    if (rt.Annotation == null)
                    {
                        rt.Annotation = new Bitmap(src.Width, src.Height);
                    }
                }
			}
			return rt.FullImage;
		}

		/// <summary>
		/// Ensure a mask exists for <paramref name="id"/> with the given size and a bound MaskCanvas.
		/// </summary>
		public (LabelMask, Bitmap) GetOrCreateAnnotationMask(Guid id, int width, int height)
		{
			var rt = GetRuntime(id);
			if (rt.Mask == null || rt.Mask.Width != width || rt.Mask.Height != height)
				rt.Mask = new LabelMask(width, height);

            if (rt.Annotation == null)
            {
                rt.Annotation = new Bitmap(width, height);
            }
			return (rt.Mask, rt.Annotation);
		}

		/// <summary>Dispose and forget runtime resources for a single image id.</summary>
		public void DisposeRuntime(Guid id)
		{
			if (rtRepo.TryGetValue(id, out var rt))
			{
				rt.FullImage?.Dispose();
				rtRepo.Remove(id);
			}
		}

		public void Dispose()
		{
			foreach (var rt in rtRepo)
				rt.Value.FullImage?.Dispose();
			rtRepo.Clear();
		}
	}
}
