using System.ComponentModel;


namespace AnnotationTool.Core.Models
{
	[Category("Train Settings")]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class TrainModelSettings : INotifyPropertyChanged
    {
        private ModelComplexity modelComplexity = ModelComplexity.Medium;
        private ComputeDevice device = ComputeDevice.Cpu;

        public event PropertyChangedEventHandler PropertyChanged;


        public TrainModelSettings()
        {
        }

		[Category("Train Settings")]
		[DisplayName("Complexity")]
		[Description("How complex is the usecase.")]
		public ModelComplexity ModelComplexity
		{
            get => modelComplexity;
            set { modelComplexity = value; OnPropertyChanged(nameof(ModelComplexity)); }
        }

		[Category("Train Settings")]
		[DisplayName("Device")]
		[Description("Choose to train on CPU or GPU.")]
		public ComputeDevice Device
        {
            get => device;
            set { device = value; OnPropertyChanged(nameof(Device)); }
        }

		[Browsable(false)]
		public string Error => null;

		protected void OnPropertyChanged(string name)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		// Prevent expanding in property grid
		public override string ToString() => "";

		public void CopyFrom(TrainModelSettings other)
		{
			if (other == null) return;

			this.ModelComplexity = other.ModelComplexity;
			this.Device = other.Device;
		}
	}
}
