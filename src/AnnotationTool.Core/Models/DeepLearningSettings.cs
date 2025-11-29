using AnnotationTool.Core.Utils;
using System.ComponentModel;

namespace AnnotationTool.Core.Models
{
	/// <summary>
	/// Parent wrapper model for deep learning settings.
	/// </summary>
	[TypeConverter(typeof(CombinedSettingsConverter))]
	public class DeepLearningSettings
    {
        private PreprocessingSettings preprocessingSettings = new PreprocessingSettings();
        private TrainModelSettings trainModelSettings = new TrainModelSettings();
        private AugmentationSettings augmentationSettings = new AugmentationSettings();
        private TrainingStoppingSettings trainingStoppingSettings = new TrainingStoppingSettings();

		public event PropertyChangedEventHandler PropertyChanged;

		public DeepLearningSettings()
        {
        }


		[Browsable(false)]
		public int HeatmapThreshold { get; set; }

		[DisplayName("Preprocessing")]
		public PreprocessingSettings PreprocessingSettings
        {
            get => preprocessingSettings;
            set { preprocessingSettings = value; OnPropertyChanged(nameof(PreprocessingSettings)); }
        }

		[DisplayName("Train Settings")]
		public TrainModelSettings TrainModelSettings
        {
            get => trainModelSettings;
            set { trainModelSettings = value; OnPropertyChanged(nameof(TrainModelSettings)); }
        }

		[DisplayName("Augmentations")]
		public AugmentationSettings AugmentationSettings
        {
            get => augmentationSettings;
            set { augmentationSettings = value; OnPropertyChanged(nameof(AugmentationSettings)); }
        }

		[DisplayName("Train Stopping")]
		public TrainingStoppingSettings TrainingStoppingSettings
        {
            get => trainingStoppingSettings;
            set { trainingStoppingSettings = value; OnPropertyChanged(nameof(TrainingStoppingSettings)); }
		}

		protected void OnPropertyChanged(string name)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public void CopyFrom(DeepLearningSettings other)
		{
			if (other == null) return;

			this.PreprocessingSettings.CopyFrom(other.PreprocessingSettings);
			this.TrainModelSettings.CopyFrom(other.TrainModelSettings);
			this.AugmentationSettings.CopyFrom(other.AugmentationSettings);
			this.TrainingStoppingSettings.CopyFrom(other.TrainingStoppingSettings);
		}
	}
}
