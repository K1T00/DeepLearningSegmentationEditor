using AnnotationTool.Core.Models;
using ILGPU;
using ILGPU.Runtime;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;


namespace AnnotationTool.Core.Utils
{
    public static class CoreUtils
    {

        /// <summary>
        /// Calculate luminance and return appropriate text color (black or white)
        /// W3C relative luminance formula: L = 0.299R + 0.587G + 0.114B
        /// Black for light backgrounds, white for dark backgrounds
        /// </summary>
        /// <param name="bgColor"></param>
        /// <returns></returns>
        public static Color GetContrastTextColor(Color bgColor)
        {
            var luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        public static RoiMode? GetRoiHitMode(PointF mouseScreen, RectangleF roiImg, Viewport viewport)
        {

            const float grabSizePx = 18f;
            float grabImg = grabSizePx / viewport.Zoom;

            RectangleF Handle(float cx, float cy) =>
                new RectangleF(
                    cx - grabImg / 2,
                    cy - grabImg / 2,
                    grabImg,
                    grabImg);

            // Corners
            if (Handle(roiImg.Left, roiImg.Top).Contains(mouseScreen)) return RoiMode.ResizingNW;
            if (Handle(roiImg.Right, roiImg.Top).Contains(mouseScreen)) return RoiMode.ResizingNE;
            if (Handle(roiImg.Left, roiImg.Bottom).Contains(mouseScreen)) return RoiMode.ResizingSW;
            if (Handle(roiImg.Right, roiImg.Bottom).Contains(mouseScreen)) return RoiMode.ResizingSE;

            // Edges
            if (Handle(roiImg.Left + roiImg.Width / 2, roiImg.Top).Contains(mouseScreen)) return RoiMode.ResizingN;
            if (Handle(roiImg.Left + roiImg.Width / 2, roiImg.Bottom).Contains(mouseScreen)) return RoiMode.ResizingS;
            if (Handle(roiImg.Left, roiImg.Top + roiImg.Height / 2).Contains(mouseScreen)) return RoiMode.ResizingW;
            if (Handle(roiImg.Right, roiImg.Top + roiImg.Height / 2).Contains(mouseScreen)) return RoiMode.ResizingE;

            return null;
        }

        public static RectangleF ClampRoi(RectangleF roi, float imgW, float imgH)
        {
            float x = Math.Max(0, roi.X);
            float y = Math.Max(0, roi.Y);

            float w = Math.Min(roi.Width, imgW - x);
            float h = Math.Min(roi.Height, imgH - y);

            return new RectangleF(x, y, Math.Max(1, w), Math.Max(1, h));
        }

        public static bool FilesAreSame(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return true;

            var fa = new FileInfo(a);
            var fb = new FileInfo(b);

            if (!fa.Exists || !fb.Exists)
                return false;

            return fa.Length == fb.Length && fa.LastWriteTimeUtc == fb.LastWriteTimeUtc;
        }

        public static Color GetDatasetSplitCategoryColor(DatasetSplit cat)
        {
            switch (cat)
            {
                case DatasetSplit.Train: return Color.Green;
                case DatasetSplit.Validate: return Color.Yellow;
                case DatasetSplit.Test: return Color.Orange;
                default: return Color.Green;
            }
        }

        public static Color GetDatasetSplitCategoryTextColor(DatasetSplit cat)
        {
            switch (cat)
            {
                case DatasetSplit.Train: return Color.White;
                case DatasetSplit.Validate: return Color.Black;
                case DatasetSplit.Test: return Color.White;
                default: return Color.White;
            }
        }

        public static void SafeDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        public static bool TryDeleteDirectoryContents(string directoryPath, out string error)
        {
            error = null;
            try
            {
                if (!Directory.Exists(directoryPath))
                    return true;

                var directoryInfo = new DirectoryInfo(directoryPath);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
                {
                    subDirectory.Delete(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string ResolveImagePathFallback(string imagesDir, Guid id, string pathFromJson, string projectRoot)
        {
            // Prefer images/{guid}.*
            if (Directory.Exists(imagesDir))
            {
                var pattern = id.ToString("D") + ".*";
                var found = Directory.EnumerateFiles(imagesDir, pattern).FirstOrDefault();
                if (found != null) return found;
            }

            // Otherwise use the JSON path (make absolute if relative)
            var candidate = pathFromJson ?? string.Empty;
            if (string.IsNullOrWhiteSpace(candidate)) return candidate;

            if (!Path.IsPathRooted(candidate))
                candidate = Path.GetFullPath(Path.Combine(projectRoot, candidate));

            return candidate;
        }

        public static void TryDeleteFile(string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Could not delete {path}: {ex.Message}");
            }
        }

        public static bool IsAllZero(SegmentationStats stats)
        {
            if (stats == null)
                return true;

            var properties = typeof(SegmentationStats)
                .GetProperties()
                .Where(p => p.CanRead &&
                            (p.PropertyType == typeof(int) || p.PropertyType == typeof(double)));

            foreach (var prop in properties)
            {
                var value = prop.GetValue(stats);

                double numeric = Convert.ToDouble(value);

                if (numeric != 0.0)
                    return false;
            }

            return true;
        }

        public static void PrepareOutputDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    // Delete files only — don’t nuke the folder structure
                    foreach (string file in Directory.EnumerateFiles(dir))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (IOException ex)
                        {
                            Debug.WriteLine($"Could not delete {file}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error preparing output directory: {ex.Message}");
                throw;
            }
        }

        public static int MapRangeStringToInt(string input, int fromMin, int fromMax, int toMin, int toMax)
        {
            if (!int.TryParse(input, out var value))
            {
                throw new ArgumentException("Invalid input string");
            }

            if (value < fromMin)
                value = fromMin;
            else if (value > fromMax)
                value = fromMax;

            var normalized = (double)(value - fromMin) / (fromMax - fromMin);
            var mapped = (int)(normalized * (toMax - toMin) + toMin);

            return mapped;
        }

        // Overkill to get GPU VRAM using ILGPU but I haven't found any simpler cross-platform way
        public static long GetCudaVRam()
        {
            long ramSize = 0;
            var myContext = Context.CreateDefault();
            foreach (var device in Context.CreateDefault())
            {
                var myAccelerator = device.CreateAccelerator(myContext);

                if (myAccelerator.AcceleratorType == AcceleratorType.Cuda)
                {
                    ramSize = myAccelerator.MemorySize;
                }
            }
            return ramSize;
        }

        // ToDo: Is there a better way?
        public static bool IsDarkMode()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (key == null)
                    return false;

                object value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                    return intValue == 0; // 0 = Dark mode

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static string FindMatchingFolder(string parentFolder, string prefix, List<string> suffixes)
        {
            foreach (string suffix in suffixes)
            {
                string expectedFolderName = $"{prefix}_{suffix}";
                string fullPath = Path.Combine(parentFolder, expectedFolderName);

                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        public static Dictionary<int, byte> BuildClassRemapByName(IReadOnlyList<Feature> oldFeatures, IReadOnlyList<Feature> newFeatures)
        {
            // New features by name
            var newByName = newFeatures.ToDictionary(f => f.Name);

            var remap = new Dictionary<int, byte>
            {
                [0] = 0 // background stays background
            };

            foreach (var oldF in oldFeatures)
            {
                Feature newF;
                if (newByName.TryGetValue(oldF.Name, out newF))
                {
                    // Feature still exists
                    remap[oldF.ClassId] = checked((byte)newF.ClassId);
                }
                else
                {
                    // Feature deleted → background
                    remap[oldF.ClassId] = 0;
                }
            }

            return remap;
        }

        public static ImageItem FindById(DeepLearningProject project, Guid id)
            => project.Images.FirstOrDefault(i => i.Guid == id);

        public static byte[] LoadGrayscalePngToByteArray(string path, int width, int height)
        {
            using (var fs = File.OpenRead(path))
            {
                using (var bmp = (Bitmap)Image.FromStream(fs))
                {

                    if (bmp.Width != width || bmp.Height != height)
                        throw new InvalidOperationException($"Heatmap size mismatch: {bmp.Width}x{bmp.Height} vs expected {width}x{height}");

                    var result = new byte[width * height];
                    var rect = new Rectangle(0, 0, width, height);

                    // PNG may decode as 8bpp indexed or 24/32bpp
                    var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                    try
                    {
                        unsafe
                        {
                            byte* src0 = (byte*)data.Scan0;
                            int stride = data.Stride;

                            bool is8bpp = bmp.PixelFormat == PixelFormat.Format8bppIndexed;
                            bool is24bpp = bmp.PixelFormat == PixelFormat.Format24bppRgb;
                            bool is32bpp = bmp.PixelFormat == PixelFormat.Format32bppArgb
                                        || bmp.PixelFormat == PixelFormat.Format32bppRgb
                                        || bmp.PixelFormat == PixelFormat.Format32bppPArgb;

                            for (int y = 0; y < height; y++)
                            {
                                byte* row = src0 + y * stride;
                                int dstIdx = y * width;

                                if (is8bpp)
                                {
                                    System.Runtime.InteropServices.Marshal.Copy((IntPtr)row, result, dstIdx, width);
                                }
                                else if (is24bpp)
                                {
                                    for (int x = 0; x < width; x++)
                                        result[dstIdx + x] = row[x * 3 + 2]; // R (grayscale => R=G=B)
                                }
                                else if (is32bpp)
                                {
                                    for (int x = 0; x < width; x++)
                                        result[dstIdx + x] = row[x * 4 + 2]; // R
                                }
                                else
                                {
                                    throw new NotSupportedException($"Unsupported pixel format: {bmp.PixelFormat}");
                                }
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }

                    return result;
                }
            }


        }
    }
}
