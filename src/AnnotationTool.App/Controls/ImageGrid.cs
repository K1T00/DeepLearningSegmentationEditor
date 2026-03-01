using AnnotationTool.Core.Models;
using System.Drawing.Drawing2D;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Controls
{
    public partial class ImageGrid : UserControl
    {

        public event EventHandler<Guid>? ImageSelected;
        public event EventHandler<Guid>? ImageAdded;

        private readonly List<PictureBox> selectedPictureBoxes = new List<PictureBox>();
        private readonly List<Guid> imageIds = [];
        private PictureBox? lastClickedPictureBox;

        private const string ThumbnailPanelName = "thumbnailPanel";
        private const string ThumbnailPictureBoxName = "thumbnailPictureBox";
        private const string CategoryLabelName = "categoryLabel";
        private const string TrainResultLabelName = "trainResultLabel";

        public ImageGrid()
        {
            InitializeComponent();

            // Ensure resources are cleaned up on disposal, hooked to the control's Disposed event (in the designer)
            Disposed += (_, __) =>
            {
                try { ClearGrid(); } catch { /* ignore during shutdown */ }
            };
        }

        public bool HasSelectedImages
        {
            get { return selectedPictureBoxes.Count > 0; }
        }

        /// <summary>
        /// ImageGrid takes ownership of the thumbnail Bitmap and will dispose it when the item is removed/cleared.
        /// </summary>
        public void AddImage(Guid imageId, Bitmap thumbnail)
        {
            const int labelPosX = 5;
            const int labelPosY = 5;
            const int padding = 7;
            int resultPosY = thumbnail.Height - labelPosY - 24;

            var pictureBox = new PictureBox
            {
                Name = ThumbnailPictureBoxName,
                Image = thumbnail,
                Size = new Size(thumbnail.Width, thumbnail.Height),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(padding),
                Tag = imageId,
                BorderStyle = BorderStyle.None
            };

            var categoryLabel = new Label
            {
                Name = CategoryLabelName,
                AutoSize = true,
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                Padding = new Padding(1),
                Location = new Point(labelPosX, labelPosY),
                Text = ""
            };

            var trainResultLabel = new Label
            {
                Name = TrainResultLabelName,
                AutoSize = true,
                BackColor = Color.Wheat,
                ForeColor = Color.Black,
                Padding = new Padding(1),
                Location = new Point(labelPosX, resultPosY),
                Text = ""
            };

            var panel = new Panel
            {
                Name = ThumbnailPanelName,
                Size = new Size(thumbnail.Width, thumbnail.Height),
                Margin = new Padding(padding)
            };

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(categoryLabel);
            panel.Controls.Add(trainResultLabel);
            categoryLabel.BringToFront();
            trainResultLabel.BringToFront();

            pictureBox.Click += (sender, e) =>
            {
                var clickedPb = sender as PictureBox;
                if (clickedPb == null) return;

                ToggleSelection(clickedPb);

                var id = GetImageId(clickedPb);
                ImageSelected?.Invoke(this, id);
            };

            pictureBox.Paint += PictureBox_Paint;

            imageGridflowLayoutPanel.Controls.Add(panel);
            imageIds.Add(imageId);

            // Notify MainForm
            ImageAdded?.Invoke(this, imageId);
        }

        public List<Guid> GetImageIds() => new(imageIds);

        public List<Guid> GetSelectedImageIds() => selectedPictureBoxes.Select(GetImageId).ToList();

        public void RemoveSelectedImages()
        {
            // Work on a snapshot because we'll mutate selection state.
            var selected = selectedPictureBoxes.ToList();

            foreach (var pb in selected)
            {
                var panel = pb.Parent as Panel;
                if (panel == null)
                    continue;

                var id = GetImageId(pb);

                // Detach + dispose the thumbnail bitmap explicitly
                DisposePictureBoxImage(pb);

                // Remove + dispose controls (UI thread)
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
                           .Select(GetThumbnailPictureBox)
                           .Where(pb => pb != null)
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
            var pb = GetThumbnailPictureBox(panel);
            if (pb == null)
                return;

            // Clear old selection
            ClearSelection();

            // Select this one
            SetSelected(pb, true);
            lastClickedPictureBox = pb;


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
            SuspendLayout();
            imageGridflowLayoutPanel.SuspendLayout();

            try
            {
                foreach (var panel in imageGridflowLayoutPanel.Controls.OfType<Panel>().ToList())
                {
                    var pb = GetThumbnailPictureBox(panel);
                    if (pb != null)
                        DisposePictureBoxImage(pb);

                    panel.Dispose();
                }

                imageGridflowLayoutPanel.Controls.Clear();
                imageIds.Clear();
                selectedPictureBoxes.Clear();
                lastClickedPictureBox = null;
            }
            finally
            {
                imageGridflowLayoutPanel.ResumeLayout();
                ResumeLayout();
            }
        }

        /// <summary>
        /// Controls are disposed here (on the UI thread) only the Bitmaps are returned to be desposed on worker thread
        /// </summary>
        public List<Bitmap> ExtractThumbnailsForDisposal()
        {
            // UI-thread only
            SuspendLayout();
            imageGridflowLayoutPanel.SuspendLayout();

            var thumbnails = new List<Bitmap>();

            try
            {
                foreach (var panel in imageGridflowLayoutPanel.Controls.OfType<Panel>().ToList())
                {
                    var pb = GetThumbnailPictureBox(panel);
                    if (pb != null)
                    {
                        var bmp = pb.Image as Bitmap;
                        pb.Image = null; // detach to avoid paint/use-after-dispose
                        if (bmp != null)
                            thumbnails.Add(bmp);
                    }

                    panel.Dispose();
                }

                imageGridflowLayoutPanel.Controls.Clear();

                imageIds.Clear();
                selectedPictureBoxes.Clear();
                lastClickedPictureBox = null;
            }
            finally
            {
                imageGridflowLayoutPanel.ResumeLayout();
                ResumeLayout();
            }

            return thumbnails;
        }

        private Panel? FindPanelById(Guid imageId)
        {
            return imageGridflowLayoutPanel.Controls
                .OfType<Panel>()
                .FirstOrDefault(p =>
                {
                    var pb = GetThumbnailPictureBox(p);
                    return pb != null && GetImageId(pb) == imageId;
                });
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            var pb = sender as PictureBox;
            if (pb == null) return;

            if (!selectedPictureBoxes.Contains(pb))
                return;

            using var pen = new Pen(Color.DeepSkyBlue, 4);
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.DrawRectangle(pen, 2, 2, pb.Width - 5, pb.Height - 5);
        }

        public void UpdateCategory(Guid imageId, DatasetSplit category)
        {
            var panel = FindPanelById(imageId);
            if (panel == null) return;

            var label = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Name == CategoryLabelName);
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
            if (panel == null)
                return;

            var label = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Name == TrainResultLabelName);
            if (label == null)
                return;

            label.Text = resultText;
            label.BringToFront();
            panel.Invalidate();
        }

        private static Guid GetImageId(PictureBox pb)
        {
            if (pb.Tag is not Guid id)
                throw new InvalidOperationException("PictureBox.Tag must contain a Guid");

            return id;
        }

        private static PictureBox GetThumbnailPictureBox(Panel panel)
        {
            var pb = panel.Controls
                .OfType<PictureBox>()
                .FirstOrDefault(pc => pc.Name == ThumbnailPictureBoxName);

            return pb ?? throw new InvalidOperationException(
                    $"Panel '{panel.Name}' does not contain a '{ThumbnailPictureBoxName}'.");
        }

        private static void DisposePictureBoxImage(PictureBox pb)
        {
            var img = pb.Image;
            pb.Image = null;
            img?.Dispose();
        }

    }
}

