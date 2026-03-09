using AnnotationTool.Core.Models;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AnnotationTool.Core.Models.LabelMask;
using static AnnotationTool.Core.Utils.BitmapIO;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.Core.Services
{
    public sealed class ImageRuntimeLoader : IImageRuntimeLoader
    {
        public Task<Bitmap> CreateThumbnailAsync(string imagePath, int controlWidth)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path is required.", nameof(imagePath));

            return Task.Run(() =>
            {
                using (var originalImage = LoadBitmapUnlocked(imagePath))
                {
                    return CreateThumbnail(originalImage, controlWidth);
                }
            });
        }

        public Task<Size> ReadImageSizeAsync(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path is required.", nameof(imagePath));

            return Task.Run(() =>
            {
                using (var originalImage = LoadBitmapUnlocked(imagePath))
                {
                    return originalImage.Size;
                }
            });
        }

        public Task<ImageRuntime> EnsureFullImageLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (imagesRepo == null) throw new ArgumentNullException(nameof(imagesRepo));
            if (projectPresenter == null) throw new ArgumentNullException(nameof(projectPresenter));

            return Task.Run(() =>
            {
                var rt = imagesRepo.GetRuntime(item);

                if (!rt.HasFullImage)
                {
                    var imgPath = projectPresenter.ResolveImagePath(item.Guid);
                    var bmp = LoadBitmapUnlocked(imgPath);

                    try
                    {
                        rt.SetFullImage(bmp);
                    }
                    catch (InvalidOperationException)
                    {
                        bmp.Dispose();
                    }
                }

                return rt;
            });
        }

        public Task EnsureAnnotationAndMaskLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (imagesRepo == null) throw new ArgumentNullException(nameof(imagesRepo));
            if (projectPresenter == null) throw new ArgumentNullException(nameof(projectPresenter));

            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(projectPresenter.ProjectPath))
                    return;

                var rt = imagesRepo.GetRuntime(item);
                if (rt.AnnotationLoadedOnce)
                    return;

                var paths = projectPresenter.Paths;
                var annPng = Path.Combine(paths.Annotations, item.Guid + paths.ImagesExt);
                var maskPng = Path.Combine(paths.Masks, item.Guid + paths.ImagesExt);

                if (File.Exists(annPng))
                {
                    using (var fs = File.OpenRead(annPng))
                    using (var tmp = Image.FromStream(fs))
                    {
                        rt.MutateAnnotation(bmp =>
                        {
                            using (var g = Graphics.FromImage(bmp))
                            {
                                g.DrawImageUnscaled(tmp, 0, 0);
                            }
                        });

                        rt.MarkAnnotationClean();
                    }
                }

                LabelMask lm;
                if (File.Exists(maskPng) && TryLoadPng8(maskPng, out lm))
                {
                    rt.MutateMask(m => m.CopyFrom(lm));
                    rt.MarkMaskClean();
                }

                rt.AnnotationLoadedOnce = true;
            });
        }

        public Task EnsureHeatmapLoadedAsync(ImageItem item, ImageRepository imagesRepo, IProjectPresenter projectPresenter, string featureName, int thresholdPercent)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (imagesRepo == null) throw new ArgumentNullException(nameof(imagesRepo));
            if (projectPresenter == null) throw new ArgumentNullException(nameof(projectPresenter));

            if (string.IsNullOrWhiteSpace(featureName))
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(projectPresenter.ProjectPath))
                    return;

                var rt = imagesRepo.GetRuntime(item);
                var paths = projectPresenter.Paths;

                var suffixes = item.SegmentationStats.Keys.Select(k => k.ToString()).ToList();
                var featureSubfolder = FindMatchingFolder(paths.MasksHeatmaps, featureName, suffixes);

                if (string.IsNullOrEmpty(featureSubfolder))
                {
                    rt.ClearHeatmap();
                    return;
                }

                var rawHeatPng = Path.Combine(featureSubfolder, item.Guid + paths.ImagesExt);
                if (!File.Exists(rawHeatPng))
                {
                    rt.ClearHeatmap();
                    return;
                }

                if (!rt.HasHeatmapCertainty(featureName))
                {
                    var certainty = LoadGrayscalePngToByteArray(
                        rawHeatPng,
                        item.ImageSize.Width,
                        item.ImageSize.Height);

                    rt.SetHeatmapCertainty(featureName, certainty, item.ImageSize.Width, item.ImageSize.Height);
                }

                var thrByte = (int)Math.Round(thresholdPercent * 255.0 / 100.0);
                rt.SetHeatmapThreshold(thrByte);
                rt.RegenerateHeatmapTurbo(featureName);
            });
        }

 
    }
}