using System;
using System.Windows.Forms;

namespace AnnotationTool.App.Utils
{
	public static class BindingHelper
	{
		// ---------------------------
		// ComboBox helpers
		// ---------------------------

		public static void WireSilent(ComboBox cb)
		{
			cb.SelectionChangeCommitted += (s, e) =>
			{
				cb.DataBindings["SelectedItem"]?.WriteValue();
			};
		}

		public static void WireNotify(ComboBox cb, Action? onChanged)
		{
			cb.SelectionChangeCommitted += (s, e) =>
			{
				cb.DataBindings["SelectedItem"]?.WriteValue();
				onChanged?.Invoke();
			};
		}

		// ---------------------------
		// NumericUpDown helpers
		// ---------------------------

		public static void WireSilent(NumericUpDown num)
		{
			num.ValueChanged += (s, e) =>
			{
				num.DataBindings["Value"]?.WriteValue();
			};
		}

		public static void WireNotify(NumericUpDown num, Action? onChanged)
		{
			num.ValueChanged += (s, e) =>
			{
				num.DataBindings["Value"]?.WriteValue();
				onChanged?.Invoke();
			};
		}
	}
}
