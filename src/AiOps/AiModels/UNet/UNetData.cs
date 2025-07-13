using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AiOps.AiUtils;
using static AiOps.AiUtils.HelperFunctions;


namespace AiOps.AiModels.UNet
{

	[Serializable]
	public class UNetData
	{
		public readonly string ModelFolder = "model";
		public readonly string TrainFolder = "train";
		public readonly string TestFolder = "test";

		public readonly string GrabsFolder = "grabs";
		public readonly string MasksFolder = "masks";
		public readonly string HeatmapsFolder = "heatmaps";
		public readonly string GrabsPreFolder = "grabsPre";
		public readonly string MasksPreFolder = "masksPre";
		public readonly string LocationsFolder = "locations";

		public readonly string UNetDataParameterFile = "DataParameter.xml";
		public readonly string ModelWeightsFile = "model.bin";

		private string projectDir;

		private string modelPath;
		private string trainGrabsPath;
		private string trainGrabsPrePath;
		private string trainMasksPath;
		private string trainMasksPrePath;
		private string testGrabsPath;
		private string testHeatmapsPath;
		private string trainLocationsPath;

		private List<ImageEntry> datasetImages;
		private List<MaskEntry> datasetMasks;
		private List<LocationsEntry> datasetLocations;

		private int downSampling;
		private int sliceRoi;
		private int amtDataset;
		private bool withBorderPadding;

		public UNetData()
		{
			this.projectDir = string.Empty;
			this.modelPath = string.Empty;
			this.trainGrabsPath = string.Empty;
			this.trainGrabsPrePath = string.Empty;
			this.trainMasksPath = string.Empty;
			this.trainMasksPrePath = string.Empty;
			this.testGrabsPath = string.Empty;
			this.testHeatmapsPath = string.Empty;
			this.trainLocationsPath = string.Empty;
			this.datasetLocations = new List<LocationsEntry>();

			this.datasetImages = new List<ImageEntry>();
			this.datasetMasks = new List<MaskEntry>();

			this.downSampling = 0;
			this.sliceRoi = 0;
			this.amtDataset = 0;
			this.withBorderPadding = false;
		}

		public UNetData(UNetData other)
		{
			datasetImages = new List<ImageEntry>(other.datasetImages);
			datasetMasks = new List<MaskEntry>(other.datasetMasks);
			this.amtDataset = other.amtDataset;
			this.downSampling = other.downSampling;
			this.downSampling = other.downSampling;
			this.sliceRoi = other.sliceRoi;
			this.projectDir = other.projectDir;
			this.withBorderPadding = other.withBorderPadding;
			this.datasetLocations = other.datasetLocations;
			this.trainLocationsPath = other.trainLocationsPath;
		}

		public UNetData(string projDir, Phase phase, bool moreThanOneFeature)
		{
			if (!CheckProjectDirectoryStructure(projDir))
			{
				throw new DirectoryNotFoundException($"Project directory structure is invalid. Missing folder(s) under: {projDir}");
			}

			this.projectDir = projDir;
			this.modelPath = Path.Combine(projDir, ModelFolder);
			this.trainGrabsPath = Path.Combine(projDir, TrainFolder, GrabsFolder);
			this.trainGrabsPrePath = Path.Combine(projDir, TrainFolder, GrabsPreFolder);
			this.trainMasksPath = Path.Combine(projDir, TrainFolder, MasksFolder);
			this.trainMasksPrePath = Path.Combine(projDir, TrainFolder, MasksPreFolder);
			this.testGrabsPath = Path.Combine(projDir, TestFolder, GrabsFolder);
			this.testHeatmapsPath = Path.Combine(projDir, TestFolder, HeatmapsFolder);
			this.trainLocationsPath = Path.Combine(projDir, TrainFolder, LocationsFolder);


			switch (phase)
			{
				case Phase.PrepareFeatureDetection:

					this.datasetImages = SortImageFiles(this.trainGrabsPath, Phase.PrepareFeatureDetection);
					this.datasetMasks = SortMaskFiles(this.trainMasksPath, moreThanOneFeature);
					break;

				case Phase.PrepareLocatePoints:

					this.datasetImages = SortImageFiles(this.trainGrabsPath, Phase.PrepareLocatePoints);
					this.datasetLocations = SortCsvFiles(this.trainLocationsPath);
					break;

				case Phase.TrainModel:

					this.datasetImages = SortImageFiles(this.trainGrabsPrePath, Phase.TrainModel);
					this.datasetMasks = SortMaskFiles(this.trainMasksPrePath, moreThanOneFeature);
					break;

				case Phase.TestModel:

					this.datasetImages = SortImageFiles(this.testGrabsPath, Phase.TestModel);
					this.datasetMasks = new List<MaskEntry>();
					break;

				default:

					this.datasetImages = new List<ImageEntry>();
					this.datasetMasks = new List<MaskEntry>();
					break;
			}

			this.amtDataset = datasetImages.Count;
		}

		private bool CheckProjectDirectoryStructure(string projectPath)
		{
			return Directory.Exists(projectPath) &&
			       Directory.Exists(Path.Combine(projectPath, ModelFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TrainFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TestFolder)) &&

			       Directory.Exists(Path.Combine(projectPath, TrainFolder, GrabsFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TrainFolder, MasksFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TrainFolder, GrabsPreFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TrainFolder, MasksPreFolder)) &&

			       Directory.Exists(Path.Combine(projectPath, TestFolder, GrabsFolder)) &&
			       Directory.Exists(Path.Combine(projectPath, TestFolder, HeatmapsFolder));
		}

		public string ModelPath { get => modelPath; set => modelPath = value; }
		public string TrainGrabsPath { get => trainGrabsPath; set => trainGrabsPath = value; }
		public string TrainGrabsPrePath { get => trainGrabsPrePath; set => trainGrabsPrePath = value; }
		public string TrainMasksPath { get => trainMasksPath; set => trainMasksPath = value; }
		public string TrainMasksPrePath { get => trainMasksPrePath; set => trainMasksPrePath = value; }
		public string TestGrabsPath { get => testGrabsPath; set => testGrabsPath = value; }
		public string TestHeatmapsPath { get => testHeatmapsPath; set => testHeatmapsPath = value; }
		public string TrainLocationsPath { get => trainLocationsPath; set => trainLocationsPath = value; }
		public string ProjectDir { get => projectDir; set => projectDir = value; }
		public int AmtDataset { get => amtDataset; set => amtDataset = value; }
		public int SliceRoi { get => sliceRoi; set => sliceRoi = value; }
		public int DownSampling { get => downSampling; set => downSampling = value; }
		public bool WithBorderPadding { get => withBorderPadding; set => withBorderPadding = value; }
		[XmlIgnore] public List<ImageEntry> DatasetImages { get => datasetImages; set => datasetImages = value; }
		[XmlIgnore] public List<MaskEntry> DatasetMasks { get => datasetMasks; set => datasetMasks = value; }
		[XmlIgnore] public List<LocationsEntry> DatasetLocations { get => this.datasetLocations; set => this.datasetLocations = value; }

	}

	public class ImageEntry
	{
		public string Path { get; set; }
		public int ImageIndex { get; set; }
	}

	public class MaskEntry
	{
		public string Path { get; set; }
		public int FeatureIndex { get; set; }
		public int ImageIndex { get; set; }
	}

	public class LocationsEntry
	{
		public string Path { get; set; }
		public int ImageIndex { get; set; }
	}

	public class LossReport
	{
		public int Epoch { get; set; }
		public float TrainLoss { get; set; }
		public float ValidationLoss { get; set; }
	}

	public enum Phase
	{
		TrainModel,
		TestModel,
		PrepareFeatureDetection,
		PrepareLocatePoints
	}
}
