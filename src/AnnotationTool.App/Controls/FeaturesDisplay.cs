using AnnotationTool.Core.Models;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Controls
{
	public partial class FeaturesDisplay : UserControl
	{

		private Label selectedLabel;

		public FeaturesDisplay()
		{
			InitializeComponent();
		}

		public Feature SelectedFeature { get; private set; }

		public void UpdateFeatures(List<Feature> features)
		{
			featuresGridLayoutPanel.Controls.Clear();
			selectedLabel = null;

			foreach (var f in features)
			{
				var textColor = GetContrastTextColor(Color.FromArgb(f.Argb));

				var label = new Label
				{
					Text = f.Name,
					BackColor = Color.FromArgb(f.Argb),
					ForeColor = textColor,
					AutoSize = true,
					BorderStyle = BorderStyle.FixedSingle,
					Margin = new Padding(5),
					Tag = f
				};

				featuresGridLayoutPanel.Controls.Add(label);

				label.Click += (sender, e) =>
				{
					var clicked = (Label)sender;
					ToggleSelection(clicked);
				};

				label.Paint += Label_Paint;
			}
		}

		private void Label_Paint(object? sender, PaintEventArgs e)
		{
			if (sender is not Label lbl)
				return;

			if (lbl != selectedLabel)
			{
				return;
			}

			const int width = 5;
			using var pen = new Pen(Color.Brown, width);
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			e.Graphics.DrawRectangle(pen, 1, 1, lbl.Width - width, lbl.Height - width);
		}

		private void ToggleSelection(Label clicked)
		{
			selectedLabel = clicked;
			SelectedFeature = (Feature)clicked.Tag;

			foreach (Label lb in featuresGridLayoutPanel.Controls)
			{
				lb.Invalidate();
			}
		}
	}
}
