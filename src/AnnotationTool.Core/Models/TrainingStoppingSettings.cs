using AnnotationTool.Core.Utils;
using System.ComponentModel;


namespace AnnotationTool.Core.Models
{
	[Category("Train Stopping")]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class TrainingStoppingSettings : INotifyPropertyChanged
	{
		private int maxIterationCount = 100;
		private int iterationWithoutImprovement = 0;
		private int maxTrainingTime = 0;
		private float minValidationError = 0.0f;

		public event PropertyChangedEventHandler PropertyChanged;

		public TrainingStoppingSettings()
		{
		}

		[Category("Train Stopping")]
		[DisplayName("Max iteration")]
		[Description("Stops training when the maximum number of iterations has been reached.")]
		[TypeConverter(typeof(MaxIterationCountConverter))]
		public int MaxIterationCount
		{
			get => maxIterationCount;
			set { maxIterationCount = value; OnPropertyChanged(nameof(MaxIterationCount)); }
		}

		[Category("Train Stopping")]
		[DisplayName("Without imporovement")]
		[Description("Stops training if no improvement in validation performance occurs after the specified number of iterations.")]
		[TypeConverter(typeof(IterationWithoutImprovementConverter))]
		public int IterationWithoutImprovement
		{
			get => iterationWithoutImprovement;
			set { iterationWithoutImprovement = value; OnPropertyChanged(nameof(IterationWithoutImprovement)); }
		}

		[Category("Train Stopping")]
		[DisplayName("Max Time")]
		[Description("Stops training when the maximum allowed training time (in minutes) is exceeded.")]
		[TypeConverter(typeof(MaxTrainingTimeConverter))]
		public int MaxTrainingTime
		{
			get => maxTrainingTime;
			set { maxTrainingTime = value; OnPropertyChanged(nameof(MaxTrainingTime)); }
		}

		[Category("Train Stopping")]
		[DisplayName("Validation error")]
		[Description("Stops training as soon as the validation error drops below this threshold.")]
		[TypeConverter(typeof(MinValidationErrorConverter))]
		public float MinValidationError
		{
			get => minValidationError;
			set { minValidationError = value; OnPropertyChanged(nameof(MinValidationError)); }
		}

		[Browsable(false)]
		public string Error => null;


		protected void OnPropertyChanged(string name)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		// Prevent expanding in property grid
		public override string ToString() => "";

		public void CopyFrom(TrainingStoppingSettings other)
		{
			if (other == null) return;

			this.MaxIterationCount = other.MaxIterationCount;
			this.IterationWithoutImprovement = other.IterationWithoutImprovement;
			this.MaxTrainingTime = other.MaxTrainingTime;
			this.MinValidationError = other.MinValidationError;
		}

	}
}
