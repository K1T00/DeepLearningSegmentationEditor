using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Core.Services;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;

namespace AnnotationTool.Ai.Training
{
    public static class TrainingTileGenerator
    {
        public static void GenerateTrainingTiles(IProjectPresenter project, IProgress<int> progress, CancellationToken ct)
        {
            Mat[] imgTiles = null;
            Mat[] maskTiles = null;
            var paths = project.Paths;

            try
            {
                var processed = 0;
                var total = project.Project.Images.Count;

                foreach (var img in project.Project.Images)
                {
                    ct.ThrowIfCancellationRequested();

                    var imagePath = Path.Combine(paths.Images, img.Guid + paths.ImagesExt);
                    var maskPath = Path.Combine(paths.Masks, img.Guid + paths.ImagesExt);

                    var space = new SegmentationImageSpace(
                        new Size(img.ImageSize.Width, img.ImageSize.Height),
                        new Rect(img.Roi.X, img.Roi.Y, img.Roi.Width, img.Roi.Height),
                        project.Project.Settings.PreprocessingSettings.SliceSize,
                        project.Project.Settings.PreprocessingSettings.DownSample,
                        project.Project.Settings.PreprocessingSettings.BorderPadding);

                    var preProc = new SegmentationPreprocessor(space);

                    using (var image = LoadInputImage(imagePath, project.Project.Settings.PreprocessingSettings.TrainAsGreyscale))
                    using (var maskGt = Cv2.ImRead(maskPath, ImreadModes.Grayscale))
                    {
                        imgTiles = preProc.ProcessImage(image);
                        maskTiles = preProc.ProcessImage(maskGt);

                        for (var i = 0; i < imgTiles.Length; i++)
                        {
                            if (project.Project.Settings.PreprocessingSettings.TrainOnlyFeatures)
                            {
                                if (!TileContainsForeground(maskTiles[i]))
                                    continue;
                            }
                            var baseName = $"{img.Guid}_{i:D4}";
                            Cv2.ImWrite(Path.Combine(paths.SlicedImages, baseName + paths.ImagesExt), imgTiles[i]);
                            Cv2.ImWrite(Path.Combine(paths.SlicedMasks, baseName + paths.ImagesExt), maskTiles[i]);
                        }
                    }
                    DisposeTiles(imgTiles);
                    DisposeTiles(maskTiles);

                    processed++;
                    progress?.Report((int)(100.0 * processed / total));
                }
            }
            finally
            {
                DisposeTiles(imgTiles);
                DisposeTiles(maskTiles);
            }
        }

        private static Mat LoadInputImage(string path, bool loadAsGreyscale)
        {
            return loadAsGreyscale
                ? Cv2.ImRead(path, ImreadModes.Grayscale)
                : Cv2.ImRead(path, ImreadModes.Color);
        }

        private static bool TileContainsForeground(Mat maskTile)
        {
            // Check for any non-zero pixel
            return Cv2.CountNonZero(maskTile) > 0;
        }
    }
}
