
using AnnotationTool.Core.Models;

namespace AnnotationTool.App.Controls
{
    public partial class AnnotationToolsPanel : UserControl
    {
        public event EventHandler<InteractionMode> ModeRequested;
        public event EventHandler<int> BrushSizeChanged;

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
            ModeRequested?.Invoke(this, InteractionMode.Paint);
        }

        private void btnEraser_Click(object sender, EventArgs e)
        {
            ModeRequested?.Invoke(this, InteractionMode.Erase);
        }

        public void SetActiveMode(InteractionMode mode)
        {
            btnPaint.BackgroundImage =
                mode == InteractionMode.Paint
                ? Properties.Resources.PaintBrushClicked
                : Properties.Resources.PaintBrush;

            btnEraser.BackgroundImage =
                mode == InteractionMode.Erase
                ? Properties.Resources.EraserClicked
                : Properties.Resources.Eraser;
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
