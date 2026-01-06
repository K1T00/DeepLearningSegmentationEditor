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
        public static void GenerateTrainingTiles(
            IProjectPresenter project,
            string outputImageDir,
            string outputMaskDir,
            IProgress<int> progress,
            CancellationToken ct)
        {
            Mat[] imgTiles = null;
            Mat[] maskTiles = null;

            try
            {
                Directory.CreateDirectory(outputImageDir);
                Directory.CreateDirectory(outputMaskDir);

                int processed = 0;
                int total = project.Project.Images.Count;

                foreach (var img in project.Project.Images)
                {
                    ct.ThrowIfCancellationRequested();

                    var space = new SegmentationImageSpace(
                        new OpenCvSharp.Size(img.ImageSize.Width, img.ImageSize.Height),
                        new OpenCvSharp.Rect(img.Roi.X, img.Roi.Y, img.Roi.Width, img.Roi.Height),
                        project.Project.Settings.PreprocessingSettings.SliceSize,
                        project.Project.Settings.PreprocessingSettings.DownSample,
                        project.Project.Settings.PreprocessingSettings.BorderPadding);

                    var preProc = new SegmentationPreprocessor(space);

                    using (var image = LoadInputImage(img.Path, project))
                    using (var maskGt = Cv2.ImRead(img.MaskPath, ImreadModes.Grayscale))
                    {
                        imgTiles = preProc.ProcessImage(image);
                        maskTiles = preProc.ProcessImage(maskGt);

                        for (int i = 0; i < imgTiles.Length; i++)
                        {
                            if (project.Project.Settings.PreprocessingSettings.TrainOnlyFeatures)
                            {
                                if (!TileContainsForeground(maskTiles[i]))
                                    continue;
                            }
                            string baseName = $"{img.Guid}_{i:D4}";
                            Cv2.ImWrite(Path.Combine(outputImageDir, baseName + ".png"), imgTiles[i]);
                            Cv2.ImWrite(Path.Combine(outputMaskDir, baseName + ".png"), maskTiles[i]);
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

        private static Mat LoadInputImage(string path, IProjectPresenter project)
        {
            return project.Project.Settings.PreprocessingSettings.TrainAsGreyscale
                ? Cv2.ImRead(path, ImreadModes.Grayscale)
                : Cv2.ImRead(path, ImreadModes.Color);
        }

        private static bool TileContainsForeground(Mat maskTile)
        {
            // any non-zero pixel ?
            return Cv2.CountNonZero(maskTile) > 0;
        }
    }
}
