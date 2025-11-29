
using AnnotationTool.Core.Models;

namespace AnnotationTool.App.Controls
{
	public partial class AnnotationToolsPanel : UserControl
	{

		public event EventHandler<int> BrushSizeChanged;
		

		public bool PaintActive { get; set; }
		public bool EraseActive { get; set; }
		public BrushMode LastClickedBrushMode { get; private set; }
		public int BrushSize => tbBrushSize.Value;


		public AnnotationToolsPanel()
		{
			InitializeComponent();
			lbBrushSize.Text = tbBrushSize.Value.ToString();
			this.LastClickedBrushMode = BrushMode.None;
		}

		private void btnPaint_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				PaintActive = !PaintActive;
				if (PaintActive) EraseActive = false;

				UpdateButtonStates();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error toggling paint mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		private void btnEraser_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				EraseActive = !EraseActive;
				if (EraseActive) PaintActive = false;
				UpdateButtonStates();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error toggling eraser mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		public void UpdateButtonStates()
		{
			btnPaint.BackgroundImage = PaintActive ? Properties.Resources.PaintBrushClicked : Properties.Resources.PaintBrush;
			btnEraser.BackgroundImage = EraseActive ? Properties.Resources.EraserClicked : Properties.Resources.Eraser;
		}

		private void tbBrushSize_ValueChanged(object sender, EventArgs e)
		{
			lbBrushSize.Text = tbBrushSize.Value.ToString();
			BrushSizeChanged.Invoke(this, tbBrushSize.Value);
		}

		private void tbBrushSize_MouseDown(object sender, MouseEventArgs e)
		{
			LastClickedBrushMode = BrushMode.MouseDown;
		}

		private void tbBrushSize_MouseUp(object sender, MouseEventArgs e)
		{
			LastClickedBrushMode = BrushMode.MouseUp;
			BrushSizeChanged.Invoke(this, tbBrushSize.Value);
		}
	}
}
