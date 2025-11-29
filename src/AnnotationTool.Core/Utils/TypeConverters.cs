using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace AnnotationTool.Core.Utils
{
	public class SliceSizeConverter : Int32Converter
	{
		private static readonly int[] allowed = new[] { 48, 96, 144, 192, 240 };

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true; 

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
			=> new StandardValuesCollection(allowed);
	}

	public class DownSampleConverter : Int32Converter
	{
		private static readonly int[] allowed = new[] { 0, 1, 2, 3, 4, 5 };

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
			=> new StandardValuesCollection(allowed);
	}

	public class CombinedSettingsConverter : ExpandableObjectConverter
	{
		// Category order applied after flattening
		private static readonly string[] CategoryOrder = new[]{
			"Train Settings",
			"Preprocessing",
			"Train Stopping",
			"Augmentations",
		};

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			// Flatten properties (same as FlattenSettingsConverter)
			var props = TypeDescriptor.GetProperties(value, attributes)
									  .Cast<PropertyDescriptor>()
									  .ToList();

			// Rewrap flattened descriptors
			var flattened = new List<PropertyDescriptor>();
			foreach (var p in props)
			{
				// preserve existing attributes
				var attrs = p.Attributes.Cast<Attribute>().ToArray();

				flattened.Add(TypeDescriptor.CreateProperty(
					value.GetType(),
					p,
					attrs
				));
			}

			// Apply category ordering (same as OrderedCategoryConverter)
			var grouped = flattened.GroupBy(p => p.Category);

			var orderedGroups =
				grouped.OrderBy(g =>
				{
					int index = Array.IndexOf(CategoryOrder, g.Key);
					return index >= 0 ? index : int.MaxValue;
				});

			// Flatten again into final list
			var orderedProps = new List<PropertyDescriptor>();
			foreach (var g in orderedGroups)
				orderedProps.AddRange(g);

			return new PropertyDescriptorCollection(orderedProps.ToArray());
		}
	}

	public class RangeIntConverter : Int32Converter
	{
		private readonly int min;
		private readonly int max;

		public RangeIntConverter(int min, int max)
		{
			this.min = min;
			this.max = max;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (!int.TryParse(value.ToString(), out int parsed))
				throw new FormatException("Value must be a number.");

			if (parsed < min || parsed > max)
				throw new ArgumentOutOfRangeException(
					$"Value must be between {min} and {max}.");

			return parsed;
		}
	}

	public class RangeFloatConverter : SingleConverter
	{
		private readonly float min;
		private readonly float max;

		public RangeFloatConverter(float min, float max)
		{
			this.min = min;
			this.max = max;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (!float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
				throw new FormatException("Value must be a number.");

			if (parsed < min || parsed > max)
				throw new ArgumentOutOfRangeException(
					$"Value must be between {min} and {max}.");

			return parsed;
		}
	}

	public class MaxIterationCountConverter : RangeIntConverter
	{
		public MaxIterationCountConverter() : base(0, int.MaxValue) { }
	}

	public class IterationWithoutImprovementConverter : RangeIntConverter
	{
		public IterationWithoutImprovementConverter() : base(0, int.MaxValue) { }
	}

	public class MaxTrainingTimeConverter : RangeIntConverter
	{
		public MaxTrainingTimeConverter() : base(0, 604800) { }
	}

	public class MinValidationErrorConverter : RangeFloatConverter
	{
		public MinValidationErrorConverter() : base(0, 1) { }
	}

	public class LuminanceConverter : RangeIntConverter
	{
		public LuminanceConverter() : base(-100, 100) { }
	}

	public class ContrastConverter : RangeIntConverter
	{
		public ContrastConverter() : base(-100, 100) { }
	}

	public class BrightnessConverter : RangeIntConverter
	{
		public BrightnessConverter() : base(-100, 100) { }
	}

	public class NoiseConverter : RangeIntConverter
	{
		public NoiseConverter() : base(0, 100) { }
	}

	public class GaussianBlurConverter : RangeIntConverter
	{
		public GaussianBlurConverter() : base(0, 20) { }
	}

	public class RotationConverter : RangeIntConverter
	{
		public RotationConverter() : base(-180, 180) { }
	}

	public class RelativeTranslationConverter : RangeIntConverter
	{
		public RelativeTranslationConverter() : base(0, 100) { }
	}

	public class MinScaleConverter : RangeIntConverter
	{
		public MinScaleConverter() : base(1, 200) { }
	}

	public class MaxScaleConverter : RangeIntConverter
	{
		public MaxScaleConverter() : base(1, 200) { }
	}

	public class HorizontalShearConverter : RangeIntConverter
	{
		public HorizontalShearConverter() : base(-45, 45) { }
	}

	public class VerticalShearConverter : RangeIntConverter
	{
		public VerticalShearConverter() : base(-45, 45) { }
	}

}
