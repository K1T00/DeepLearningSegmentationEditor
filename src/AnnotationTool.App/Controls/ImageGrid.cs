using AnnotationTool.Core.Models;
using static AnnotationTool.Core.Utils.BitmapIO;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Controls
{
	public partial class ImageGrid : UserControl
	{

		public event EventHandler<Guid> ImageSelected;
		public event EventHandler<(Guid Id, int Width, int Height)> ImageAdded;

		private readonly List<PictureBox> selectedPictureBoxes;
		private readonly List<Guid> imageIds = [];
		private PictureBox lastClickedPictureBox;

		public ImageGrid()
		{
			InitializeComponent();
			selectedPictureBoxes = new List<PictureBox>();
		}

		public bool HasSelectedImages
		{
			get { return selectedPictureBoxes.Count > 0; }
		}

		public void AddImage(Guid imageId, string imagePath)
		{
			if (imageIds.Contains(imageId)) return;

			const int labelPosX = 5;
			const int labelPosY = 5;
			const int padding = 7;
			var containerWidth = this.Width - 40;

            var originalImage = LoadBitmapUnlocked(imagePath);

            // Preserve aspect ratio (width fixed, height scales)
            var originalW = originalImage.Width;
            var originalH = originalImage.Height;
            var ratio = (float)originalW / originalH;
            var thumbW = containerWidth;
            var thumbH = (int)(thumbW / ratio);
            if (thumbH < 1) thumbH = 1;
            int resultPosY = thumbH - labelPosY - 24;

            var thumbnail = originalImage.GetThumbnailImage(thumbW, thumbH, null, IntPtr.Zero);

			var pictureBox = new PictureBox
			{
				Name = "thumbnailPictureBox",
				Image = thumbnail,
                Size = new Size(thumbW, thumbH),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(padding),
				Tag = imageId,
				BorderStyle = BorderStyle.None
			};

			var categoryLabel = new Label
			{
				Name = "categoryLabel",
				AutoSize = true,
				BackColor = Color.LightGray,
				ForeColor = Color.Black,
				Padding = new Padding(1),
				Location = new Point(labelPosX, labelPosY),
				Text = ""
			};

			var trainResultLabel = new Label
			{
				Name = "trainResultLabel",
				AutoSize = true,
				BackColor = Color.Wheat,
				ForeColor = Color.Black,
				Padding = new Padding(1),
				Location = new Point(labelPosX, resultPosY),
				Text = ""
			};

			var panel = new Panel
			{
				Name = "thumbnailPanel",
                Size = new Size(thumbW, thumbH),
                Margin = new Padding(padding)
			};

			panel.Controls.Add(pictureBox);
			panel.Controls.Add(categoryLabel);
			panel.Controls.Add(trainResultLabel);
			categoryLabel.BringToFront();
			trainResultLabel.BringToFront();

			pictureBox.Click += (sender, e) =>
			{
				var clicked = (PictureBox)sender;
				ToggleSelection(clicked);
				ImageSelected?.Invoke(this, (Guid)clicked.Tag);
			};

			pictureBox.Paint += PictureBox_Paint;

			imageGridflowLayoutPanel.Controls.Add(panel);
			imageIds.Add(imageId);

			// Notify MainForm with image dimensions
			ImageAdded?.Invoke(this, (imageId, originalImage.Width, originalImage.Height));
			originalImage.Dispose();
		}

		public List<Guid> GetImageIds() => new(imageIds);

		public List<Guid> GetSelectedImageIds() => selectedPictureBoxes.Select(pb => (Guid)pb.Tag).ToList();

		public void RemoveSelectedImages()
		{
			foreach (var pb in selectedPictureBoxes.ToList())
			{
				var panel = pb.Parent as Panel;
				var id = (Guid)pb.Tag;
				imageGridflowLayoutPanel.Controls.Remove(panel);
				panel.Dispose();
				imageIds.Remove(id);
				selectedPictureBoxes.Remove(pb);
			}
			lastClickedPictureBox = null;
		}

		private void SetSelected(PictureBox pb, bool selected)
		{
			if (selected)
			{
				if (!selectedPictureBoxes.Contains(pb))
					selectedPictureBoxes.Add(pb);
			}
			else
			{
				selectedPictureBoxes.Remove(pb);
			}
			// Redraw
			pb.Invalidate();
		}

		private void ToggleSelection(PictureBox clicked)
		{
			if ((ModifierKeys & Keys.Control) == Keys.Control)
			{
				SetSelected(clicked, !selectedPictureBoxes.Contains(clicked));
				lastClickedPictureBox = clicked;
				return;
			}
			if ((ModifierKeys & Keys.Shift) == Keys.Shift && lastClickedPictureBox != null)
			{
				var boxes = imageGridflowLayoutPanel.Controls.OfType<Panel>()
					.Select(p => p.Controls.OfType<PictureBox>().First(pc => pc.Name == "thumbnailPictureBox"))
					.ToList();
				var i1 = boxes.IndexOf(lastClickedPictureBox);
				var i2 = boxes.IndexOf(clicked);
				if (i1 > -1 && i2 > -1)
				{
					var lo = Math.Min(i1, i2);
					var hi = Math.Max(i1, i2);
					for (int i = lo; i <= hi; i++)
						SetSelected(boxes[i], true);
				}
				return;
			}
			ClearSelection();
			SetSelected(clicked, true);
			lastClickedPictureBox = clicked;
			clicked.Invalidate();
		}

        public void SelectImage(Guid imageId)
        {
            // Find the corresponding panel
            var panel = FindPanelById(imageId);
            if (panel == null)
                return;

            // Find the PictureBox inside it
            var pb = panel.Controls
                          .OfType<PictureBox>()
                          .FirstOrDefault(pc => pc.Name == "thumbnailPictureBox");

            if (pb == null)
                return;

            // Clear old selection
            ClearSelection();

            // Select this one
            SetSelected(pb, true);
            lastClickedPictureBox = pb;

            // Fire the same event as a user click
            ImageSelected?.Invoke(this, imageId);

            // Refresh UI
            pb.Invalidate();
        }

        private void ClearSelection()
		{
			foreach (var pb in selectedPictureBoxes.ToList())
				SetSelected(pb, false);
			selectedPictureBoxes.Clear();
		}

		public void ClearGrid()
		{
			foreach (Control c in imageGridflowLayoutPanel.Controls)
			{
				c.Dispose();
			}
			imageGridflowLayoutPanel.Controls.Clear();

			imageIds.Clear();
			selectedPictureBoxes.Clear();
			lastClickedPictureBox = null;
		}

		private Panel FindPanelById(Guid imageId)
		{
			return imageGridflowLayoutPanel.Controls.OfType<Panel>().FirstOrDefault(
				p =>{var pb = p.Controls.OfType<PictureBox>().FirstOrDefault(pc => pc.Name == "thumbnailPictureBox");
					return pb != null && (Guid)pb.Tag == imageId;});
		}

		private void PictureBox_Paint(object? sender, PaintEventArgs e)
		{
			if (sender is not PictureBox pb)
				return;

			if (!selectedPictureBoxes.Contains(pb))
				return;

			using var pen = new Pen(Color.DeepSkyBlue, 4);
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			e.Graphics.DrawRectangle(pen, 2, 2, pb.Width - 5, pb.Height - 5);
		}

		public void UpdateCategory(Guid imageId, DatasetSplit category)
		{
			var panel = FindPanelById(imageId);
			if (panel == null) return;

			var label = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "categoryLabel");
			if (label == null) return;
			
			label.Text = category.ToString();
			label.BackColor = GetDatasetSplitCategoryColor(category);
			label.ForeColor = GetDatasetSplitCategoryTextColor(category);
			label.BringToFront();
			panel.Invalidate();
		}

		public void UpdateTrainResult(Guid imageId, string resultText)
		{
			var panel = FindPanelById(imageId);
			if (panel == null) return;

			var label = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "trainResultLabel");
			if (label == null) return;

			label.Text = resultText ?? "";
			label.BringToFront();
			panel.Invalidate();
		}
	}
}

