using System;
using System.ComponentModel;
using AnnotationTool.Core.Utils;


namespace AnnotationTool.Core.Models
{
	[Category("Preprocessing")]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class PreprocessingSettings : INotifyPropertyChanged
    {
        private bool trainAsGreyscale = true;
        private int downSample = 1;
        private bool borderPadding = true;
        private bool trainOnlyFeatures = false;
        private int sliceSize = 96;

		public event PropertyChangedEventHandler PropertyChanged;

		public PreprocessingSettings()
		{
		}


		[Browsable(false)]
		public NormalizationSettings Normalization { get; set; } = new NormalizationSettings();

		[Category("Preprocessing")]
		[DisplayName("Train as Greyscale")]
		[Description("Converts all input images to greyscale before training.")]
		public bool TrainAsGreyscale
        {
            get => trainAsGreyscale;
            set { trainAsGreyscale = value; OnPropertyChanged(nameof(TrainAsGreyscale)); }
        }

		[Category("Preprocessing")]
		[DisplayName("Downsampling")]
		[Description("Reduces the image resolution by repeatedly halving its size.")]
		[TypeConverter(typeof(DownSampleConverter))]
		public int DownSample
        {
            get => downSample;
            set { downSample = value; OnPropertyChanged(nameof(DownSample)); }
        }

		[Category("Preprocessing")]
		[DisplayName("Slice size")]
		[Description("Specifies the size of each image slice used for training. Should match the scale of the features you want to detect.")]
		[TypeConverter(typeof(SliceSizeConverter))]
		public int SliceSize
        {
            get => sliceSize;
            set { sliceSize = value; OnPropertyChanged(nameof(SliceSize)); }
        }

		[Category("Preprocessing")]
		[DisplayName("Border padding")]
		[Description("Allows slices to extend beyond the image boundary by padding the missing regions.")]
		public bool BorderPadding
        {
            get => borderPadding;
            set { borderPadding = value; OnPropertyChanged(nameof(BorderPadding)); }
        }

		[Category("Preprocessing")]
		[DisplayName("Only feature")]
		[Description("Includes only slices that contain annotated features during training, ignoring empty background slices.")]
		public bool TrainOnlyFeatures
        {
            get => trainOnlyFeatures;
            set { trainOnlyFeatures = value; OnPropertyChanged(nameof(TrainOnlyFeatures)); }
        }

		[Browsable(false)]
		public string Error => null;

		// Prevent expanding in property grid
		public override string ToString() => "";

		protected void OnPropertyChanged(string name)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public void CopyFrom(PreprocessingSettings other)
		{
			if (other == null) return;

			this.TrainAsGreyscale = other.TrainAsGreyscale;
			this.DownSample = other.DownSample;
			this.BorderPadding = other.BorderPadding;
			this.TrainOnlyFeatures = other.TrainOnlyFeatures;
			this.SliceSize = other.SliceSize;
			this.Normalization = other.Normalization;
		}
	}

    public class NormalizationSettings
    {
        public float[] Mean { get; set; } = Array.Empty<float>();
        public float[] Std { get; set; } = Array.Empty<float>();
	}
}
