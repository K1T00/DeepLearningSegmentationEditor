using System;
using System.Drawing;
using System.Threading.Tasks;

namespace AnnotationTool.Core.Services
{
	/// <summary>
	/// Centralized image-loading & thumbnail caching service.
	/// </summary>
	public interface IImageService : IDisposable
	{
		/// <summary>
		/// Ensures a thumbnail exists for the image; returns it (may run on background thread).
		/// </summary>
		Task<Image> EnsureThumbnailAsync(Guid id, string path);

		/// <summary>
		/// Obtains cached thumbnail if present, otherwise null.
		/// </summary>
		Image GetCachedThumbnail(Guid id);

		/// <summary>
		/// Removes a thumbnail from cache (e.g. when an image is deleted).
		/// </summary>
		void DropThumbnail(Guid id);
	}
}
