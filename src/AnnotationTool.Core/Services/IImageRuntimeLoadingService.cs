using AnnotationTool.Core.Models;
using System.Drawing;
using System.Threading.Tasks;

namespace AnnotationTool.Core.Services
{
    public interface IImageRuntimeLoader
    {
        Task<Bitmap> CreateThumbnailAsync(string imagePath, int controlWidth);
        Task<Size> ReadImageSizeAsync(string imagePath);
        Task<ImageRuntime> EnsureFullImageLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter);
        Task EnsureAnnotationAndMaskLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter);
        Task EnsureHeatmapLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter, string featureName, int thresholdPercent);
    }
}
