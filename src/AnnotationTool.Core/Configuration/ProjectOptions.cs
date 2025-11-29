namespace AnnotationTool.Core.Configuration
{
	public class ProjectOptions
	{
		public string ImagesFolder { get; } = "Images";
		public string MasksFolder { get; } = "Masks";
		public string AnnotationsFolder { get; } = "Annotations";
		public string ResultsFolder { get; } = "Results";
		public string LogsFolder { get; } = "Logs";
		public string ModelsSubFolder { get; } = "Results/Models";
		public string SlicedImagesSubFolder { get; } = "Results/Preprocessing/SlicedImages";
		public string SlicedMasksSubFolder { get; } = "Results/Preprocessing/SlicedMasks";
		public string HeatmapsSubFolder { get; } = "Results/Heatmaps/Images";
		public string HeatmapsOverlaysSubFolder { get; } = "Results/Heatmaps/Overlays";
		public string DateTimeFormat { get; set; } = "yyyy_MM_dd_HH_mm_ss";
		public string ModelFileName { get; set; } = "Model_";
		public string TrainingSettingsFileName { get; set; } = "Trainingsettings_";
	}
}
