using AnnotationTool.Core.Models;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.Win32;


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

		public static RoiMode? GetHitMode(Point mousePos, Rectangle screenROI)
		{
			const int grabSize = 8;

			// Corner handles
			if (new Rectangle(screenROI.Left - grabSize / 2, screenROI.Top - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingNW;
			if (new Rectangle(screenROI.Right - grabSize / 2, screenROI.Top - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingNE;
			if (new Rectangle(screenROI.Left - grabSize / 2, screenROI.Bottom - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingSW;
			if (new Rectangle(screenROI.Right - grabSize / 2, screenROI.Bottom - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingSE;

			// Side handles
			if (new Rectangle(screenROI.Left + screenROI.Width / 2 - grabSize / 2, screenROI.Top - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingN;
			if (new Rectangle(screenROI.Left + screenROI.Width / 2 - grabSize / 2, screenROI.Bottom - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingS;
			if (new Rectangle(screenROI.Left - grabSize / 2, screenROI.Top + screenROI.Height / 2 - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingW;
			if (new Rectangle(screenROI.Right - grabSize / 2, screenROI.Top + screenROI.Height / 2 - grabSize / 2, grabSize, grabSize).Contains(mousePos)) return RoiMode.ResizingE;

			return null;
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

		public static Rectangle EnsureRoi(Rectangle roi, int width, int height)
		{
			var bounds = new Rectangle(0, 0, width, height);
			if (width <= 0 || height <= 0) return Rectangle.Empty;

			if (roi.Width <= 0 || roi.Height <= 0)
				return bounds;

			var clamped = Rectangle.Intersect(bounds, roi);
			return (clamped.Width > 0 && clamped.Height > 0) ? clamped : bounds;
		}

		public static void SafeDeleteFile(string path)
		{
			try { if (File.Exists(path)) File.Delete(path); }
			catch { }
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
				var key = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

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

	}
}
