using AnnotationTool.Core.Utils;
using System.ComponentModel;


namespace AnnotationTool.Core.Models
{
	[Category("Augmentations")]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class AugmentationSettings : INotifyPropertyChanged
    {
        private int luminance = 0;
        private int contrast = 0;
        private int brightness = 0;
        private int noise = 0;
        private int gaussianBlur = 0;
        private int rotation = 0;
        private bool flipVertical = false;
        private bool flipHorizontal = false;
        private int relativeTranslation = 0;
        private int minScale = 0;
        private int maxScale = 0;
        private int horizontalShear = 0;
        private int verticalShear = 0;
		private AugmentationMode augmentationMode = AugmentationMode.Standard;

        public event PropertyChangedEventHandler PropertyChanged;

        public AugmentationSettings()
        {
        }

        [Category("Augmentations")]
        [DisplayName("AugmentationMode")]
        [Description("How augmentations are applied to the dataset.")]
        public AugmentationMode AugmentationMode
        {
            get => augmentationMode;
            set { augmentationMode = value; OnPropertyChanged(nameof(AugmentationMode)); }
        }

        [Category("Augmentations")]
		[DisplayName("Luminance")]
		[Description("Adjusts the overall luminance of the image to simulate lighting variations.")]
		[TypeConverter(typeof(LuminanceConverter))]
		public int Luminance
        {
            get => luminance;
            set { luminance = value; OnPropertyChanged(nameof(Luminance)); }
        }

		[Category("Augmentations")]
		[DisplayName("Contrast")]
		[Description("Modifies the contrast to make differences between light and dark regions stronger or weaker.")]
		[TypeConverter(typeof(ContrastConverter))]
		public int Contrast
        {
            get => contrast;
            set { contrast = value; OnPropertyChanged(nameof(Contrast)); }
        }

		[Category("Augmentations")]
		[DisplayName("Brightness")]
		[Description("Increases or decreases the brightness to simulate overexposure or underexposure.")]
		[TypeConverter(typeof(BrightnessConverter))]
		public int Brightness
        {
            get => brightness;
            set { brightness = value; OnPropertyChanged(nameof(Brightness)); }
        }

		[Category("Augmentations")]
		[DisplayName("Noise")]
		[Description("Adds random noise to simulate sensor noise or low-quality images.")]
		[TypeConverter(typeof(NoiseConverter))]
		public int Noise
        {
            get => noise;
            set { noise = value; OnPropertyChanged(nameof(Noise)); }
        }

		[Category("Augmentations")]
		[DisplayName("GaussianBlur")]
		[Description("Applies Gaussian blur to soften the image and simulate out-of-focus conditions.")]
		[TypeConverter(typeof(GaussianBlurConverter))]
		public int GaussianBlur
        {
            get => gaussianBlur;
            set { gaussianBlur = value; OnPropertyChanged(nameof(GaussianBlur)); }
        }

		[Category("Augmentations")]
		[DisplayName("Rotation")]
		[Description("Randomly rotates the image within a specified angle range to improve rotational robustness.")]
		[TypeConverter(typeof(RotationConverter))]
		public int Rotation
        {
            get => rotation;
            set { rotation = value; OnPropertyChanged(nameof(Rotation)); }
        }

		[Category("Augmentations")]
		[DisplayName("FlipVertical")]
		[Description("Flips the image vertically to introduce vertical mirroring.")]
		public bool FlipVertical
        {
            get => flipVertical;
            set { flipVertical = value; OnPropertyChanged(nameof(FlipVertical)); }
        }

		[Category("Augmentations")]
		[DisplayName("FlipHorizontal")]
		[Description("Flips the image horizontally to introduce horizontal mirroring.")]
		public bool FlipHorizontal
        {
            get => flipHorizontal;
            set { flipHorizontal = value; OnPropertyChanged(nameof(FlipHorizontal)); }
        }

		[Category("Augmentations")]
		[DisplayName("RelativeTranslation")]
		[Description("Shifts the image horizontally and vertically by a percentage to simulate object displacement.")]
		[TypeConverter(typeof(RelativeTranslationConverter))]
		public int RelativeTranslation
        {
            get => relativeTranslation;
            set { relativeTranslation = value; OnPropertyChanged(nameof(RelativeTranslation)); }
        }

		[Category("Augmentations")]
		[DisplayName("MinScale")]
		[Description("Specifies the minimum scaling factor used when randomly resizing the image.")]
		[TypeConverter(typeof(MinScaleConverter))]
		public int MinScale
        {
            get => minScale;
            set { minScale = value; OnPropertyChanged(nameof(MinScale)); }
        }

		[Category("Augmentations")]
		[DisplayName("MaxScale")]
		[Description("Specifies the maximum scaling factor used when randomly resizing the image.")]
		[TypeConverter(typeof(MaxScaleConverter))]
		public int MaxScale
        {
            get => maxScale;
            set { maxScale = value; OnPropertyChanged(nameof(MaxScale)); }
        }

		[Category("Augmentations")]
		[DisplayName("HorizontalShear")]
		[Description("Applies a horizontal shear transformation to slant the image sideways.")]
		[TypeConverter(typeof(HorizontalShearConverter))]
		public int HorizontalShear
        {
            get => horizontalShear;
            set { horizontalShear = value; OnPropertyChanged(nameof(HorizontalShear)); }
        }

		[Category("Augmentations")]
		[DisplayName("VerticalShear")]
		[Description("Applies a vertical shear transformation to slant the image upward or downward.")]
		[TypeConverter(typeof(VerticalShearConverter))]
		public int VerticalShear
        {
            get => verticalShear;
            set { verticalShear = value; OnPropertyChanged(nameof(VerticalShear)); }
        }

		[Browsable(false)]
		public string Error => null;


		protected void OnPropertyChanged(string name)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		// Prevent expanding in property grid
		public override string ToString() => "";

		public void CopyFrom(AugmentationSettings other)
		{
			if (other == null) return;

			this.Luminance = other.Luminance;
			this.Contrast = other.Contrast;
			this.Brightness = other.Brightness;
			this.Noise = other.Noise;
			this.GaussianBlur = other.GaussianBlur;
			this.Rotation = other.Rotation;
			this.HorizontalShear = other.HorizontalShear;
			this.VerticalShear = other.VerticalShear;
			this.RelativeTranslation = other.RelativeTranslation;
			this.MinScale = other.MinScale;
			this.MaxScale = other.MaxScale;
			this.FlipHorizontal = other.FlipHorizontal;
			this.FlipVertical = other.FlipVertical;
		}
	}
}
