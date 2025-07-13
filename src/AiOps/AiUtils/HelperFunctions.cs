using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using AiOps.AiModels.UNet;
using CsvHelper;


namespace AiOps.AiUtils
{
	public class HelperFunctions
	{

        /// <summary>
        /// Convert array to a C# matrix
        /// </summary>
        /// <param name="flat">Array</param>
        /// <param name="m">Rows</param>
        /// <param name="n">Columns</param>
        /// <returns>The C# 2dim Matrix</returns>
        /// <exception cref="ArgumentException"></exception>
        public static double[,] ConvertToMatrix(double[] flat, int m, int n)
		{
			if (flat.Length != m * n)
			{
				throw new ArgumentException("Invalid length");
			}
			var ret = new double[m, n];

			Buffer.BlockCopy(flat, 0, ret, 0, flat.Length * sizeof(double));
			return ret;
		}

        /// <summary>
        /// Split a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations"></param>
        /// <param name="nSize"></param>
        /// <returns></returns>
		public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize)
		{
			for (var i = 0; i < locations.Count; i += nSize)
			{
				yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
			}
		}

		/// <summary>
		/// Sort images files based on their file names
		/// </summary>
		/// <param name="path">Image folder</param>
		public static List<ImageEntry> SortImageFiles(string path, Phase phase)
		{
			var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
			var fileEntries = new List<ImageEntry>();

			var i = 0;
            foreach (var datasetImage in files)
            {
	            var sStart = datasetImage.LastIndexOf("\\", StringComparison.CurrentCulture) + 1;
	            var sTo = datasetImage.LastIndexOf(".", StringComparison.CurrentCulture);

	            if (phase == Phase.TestModel)
	            {
		            fileEntries.Add(new ImageEntry() { ImageIndex = i, Path = datasetImage });

					i++;
	            }
	            else
	            {
		            fileEntries.Add(
			            new ImageEntry()
			            {
				            ImageIndex = Convert.ToInt16(datasetImage.Substring(sStart, sTo - sStart)), 
				            Path = datasetImage
			            });
				}
            }
			return fileEntries.OrderBy(f => f.ImageIndex).ToList();
		}

		/// <summary>
		/// Sort masks files based on their file names
		/// </summary>
		/// <param name="path"></param>
		/// <param name="moreThanOneFeature"></param>
		public static List<MaskEntry> SortMaskFiles(string path, bool moreThanOneFeature)
		{
			var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
			var fileEntries = new List<MaskEntry>();

            foreach (var datasetImage in files)
			{
				var sStart = datasetImage.LastIndexOf("\\", StringComparison.CurrentCulture) + 1;
				var sTo = datasetImage.LastIndexOf(".", StringComparison.CurrentCulture);

				if (moreThanOneFeature)
				{
					var sSplit = datasetImage.Substring(sStart, sTo - sStart).Split('_');

					fileEntries.Add(new MaskEntry()
					{
						ImageIndex = Convert.ToInt16(sSplit[0]) , 
						FeatureIndex = Convert.ToInt16(sSplit[1]),
						Path = datasetImage
					});
				}
				else
				{
					fileEntries.Add(new MaskEntry()
					{
						ImageIndex = Convert.ToInt16(datasetImage.Substring(sStart, sTo - sStart)),
						FeatureIndex = 1,
						Path = datasetImage
					});
				}
			}
			return fileEntries.OrderBy(t => t.ImageIndex).ToList();
		}

		/// <summary>
		/// Sort CSV files based on their file names
		/// </summary>
		/// <param name="path">Files in the locations folder</param>
		/// <returns>Tuple of filepath, grabNr </returns>
		public static List<LocationsEntry> SortCsvFiles(string path)
		{
			var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
			var fileEntries = new List<LocationsEntry>();

			foreach (var datasetImage in files)
			{
				var sStart = datasetImage.LastIndexOf("\\", StringComparison.CurrentCulture) + 1;
				var sTo = datasetImage.LastIndexOf(".", StringComparison.CurrentCulture);

				fileEntries.Add(new LocationsEntry()
				{
					ImageIndex = Convert.ToInt16(datasetImage.Substring(sStart, sTo - sStart)),
					Path = datasetImage
				});
			}
			return fileEntries.OrderBy(t => t.ImageIndex).ToList();
		}

		public static void EmptyFolder(DirectoryInfo directory)
		{
			foreach (var file in directory.GetFiles()) file.Delete();
			//foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
		}

		/// <summary>
		/// Calculates the number of image slices that can be created from an image of the specified dimensions.
		/// </summary>
		/// <remarks>If <paramref name="withBorderPadding"/> is <see langword="true"/>, the method calculates the
		/// number of slices  by rounding up to include any partial slices. If <paramref name="withBorderPadding"/> is <see
		/// langword="false"/>,  only full slices are included, and the calculation rounds down.</remarks>
		/// <param name="imageHeight">The height of the image, in pixels.</param>
		/// <param name="imageWidth">The width of the image, in pixels.</param>
		/// <param name="sliceSize">The size of each slice, in pixels. Must be greater than 0.</param>
		/// <param name="withBorderPadding">A value indicating whether to include partial slices at the edges of the image.  If <see langword="true"/>,
		/// partial slices are included; otherwise, only full slices are counted.</param>
		/// <returns>A tuple containing two integers: the number of slices along the rows and the number of slices along the columns.</returns>
		public static (int, int) GetAmtImages(int imageHeight, int imageWidth, int sliceSize, bool withBorderPadding)
        {
	        decimal rowImages = 1;
	        decimal columnImages = 1;

	        if (withBorderPadding)
	        {
                rowImages = Math.Ceiling(Convert.ToDecimal((double)imageHeight / sliceSize));
                columnImages = Math.Ceiling(Convert.ToDecimal((double)imageWidth / sliceSize));
            }
	        else
	        {
                rowImages = Math.Floor(Convert.ToDecimal((double)imageHeight / sliceSize));
                columnImages = Math.Floor(Convert.ToDecimal((double)imageWidth / sliceSize));
            }
	        return ((int)rowImages, (int)columnImages);
        }

		public static IEnumerable<int> GetDivisors(int number)
		{
			return Enumerable.Range(1, number).Where(x => number % x == 0).Select(x => number / x);
		}

		public static List<CsvRecord>[] LoadCsvFiles(string[] filePaths)
		{
			var result = new List<CsvRecord>[filePaths.Length];

			var r = 0;
			foreach (var filePath in filePaths)
			{
				using (var reader = new StreamReader(filePath))
				using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
				{
					csv.Context.RegisterClassMap<CsvRecordMap>();
					var records = csv.GetRecords<CsvRecord>().ToList();
					result[r] = records;
				}

				r++;
			}
			return result;
		}

		/// <summary>
		/// Get locations from csv files
		/// </summary>
		/// <param name="csvFiles"></param>
		/// <returns></returns>
		public static List<Locations> GetLocations(List<CsvRecord>[] csvFiles)
		{

			var locationsList = new List<Locations>();

			foreach (var file in csvFiles)
			{
				var locations = new List<Location>();

				foreach (var loc in file)
				{
					locations.Add(new Location
					{
						X = Convert.ToInt16(loc.Axis1),
						Y = Convert.ToInt16(loc.Axis0)
					});
				}

				var myLocations = new Locations();
				myLocations.AddRange(locations);
				locationsList.Add(myLocations);
			}
			return locationsList;
		}

	}


}
