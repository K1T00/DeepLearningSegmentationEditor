using AnnotationTool.Core.Utils;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;


namespace AnnotationTool.Core.Services
{
	public class ImageService : IImageService, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, Image> thumbCache = new ConcurrentDictionary<Guid, Image>();
		private readonly int thumbWidth;
		private readonly int thumbHeight;

		public ImageService(int thumbWidth = 200, int thumbHeight = 200)
		{
			this.thumbWidth = thumbWidth;
			this.thumbHeight = thumbHeight;
		}

		public Image GetCachedThumbnail(Guid id)
			=> thumbCache.TryGetValue(id, out var img) ? img : null;

		public void DropThumbnail(Guid id)
		{
			if (thumbCache.TryRemove(id, out var img))
			{
				img.Dispose();
			}
		}

		public async Task<Image> EnsureThumbnailAsync(Guid id, string path)
		{
			if (thumbCache.TryGetValue(id, out var existing))
				return existing;

			// Do not block caller; generate on thread pool
			var bmp = await Task.Run(() =>
			{

                using (var src = BitmapIO.LoadBitmapUnlocked(path))
                {
                    var thumb = CreateThumbnail(src, thumbWidth, thumbHeight);
                    return (Image)thumb;
                }
			}).ConfigureAwait(true);

			// store in cache (replace rare race safely)
			var cached = thumbCache.GetOrAdd(id, bmp);
			if (!ReferenceEquals(cached, bmp))
			{
				// another thread already added one; discard ours
				bmp.Dispose();
			}
			return cached;
		}

		private static Bitmap CreateThumbnail(Bitmap src, int w, int h)
		{
			var dest = new Bitmap(w, h);

            using (var g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(src, 0, 0, w, h);
                return dest;
            }
		}

		public void Dispose()
		{
			foreach (var kv in thumbCache)
			{
				kv.Value.Dispose();
			}
			thumbCache.Clear();
		}
	}
}
