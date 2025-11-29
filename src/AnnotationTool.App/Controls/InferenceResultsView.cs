using AnnotationTool.Core.Models;
using ScottPlot;
using SkiaSharp.Views.Desktop;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Controls
{
    public partial class InferenceResultsView : UserControl
    {

        private SegmentationStats segmentationStats;


        // WinForms tooltip for metric descriptions
        private readonly ToolTip radarTooltip = new ToolTip();

        // Remember which bar we are currently over (null = none)
        private int? lastHoveredBarIndex = null;

        private readonly (string Name, string Description)[] metricInfo =
        {
            ("Dice",       "Measures overlap between prediction and mask. 100% = perfect match."),
            ("Precision",  "Of all predicted defects, how many were correct. High = fewer false alarms."),
            ("Recall",     "Of all real defects, how many were detected. High = fewer missed defects."),
            ("FPR",        "False-positive rate. Lower is better."),
            ("DiceLoss",   "1 - Dice score. Lower means better segmentation.")
        };



        public InferenceResultsView()
        {
            InitializeComponent();

            radarTooltip.AutoPopDelay = 6000;
            radarTooltip.InitialDelay = 400; // slightly longer to make it feel stable
            radarTooltip.ReshowDelay = 100;
            radarTooltip.ShowAlways = true;
        }

        public SegmentationStats SegmentationStats
        {
            get => segmentationStats;
            set
            {
                segmentationStats = value;
                UpdatePlot();
            }
        }

        private void InferenceResultsControl_Load(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        public void ClearPlot()
        {
            radarPlotResults.Plot.Clear();
            radarPlotResults.Refresh();
        }

        public void UpdatePlot()
        {
            if (this.SegmentationStats == null)
                return;

            radarPlotResults.Plot.Clear();

            var barDice = radarPlotResults.Plot.Add.Bar(position: 1, value: this.SegmentationStats.Dice * 100);
            var barPrecision = radarPlotResults.Plot.Add.Bar(position: 2, value: this.SegmentationStats.Precision * 100);
            var barRecall = radarPlotResults.Plot.Add.Bar(position: 3, value: this.SegmentationStats.Recall * 100);
            var barFpr = radarPlotResults.Plot.Add.Bar(position: 4, value: this.SegmentationStats.FPR * 100);
            var barDiceLoss = radarPlotResults.Plot.Add.Bar(position: 5, value: 100 - this.SegmentationStats.Dice * 100);

            Tick[] ticks =
            {
                new(1, "Dice"),
                new(2, "Precision"),
                new(3, "Recall"),
                new(4, "FPR"),
                new(5, "DiceLoss")
            };

            radarPlotResults.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
            radarPlotResults.Plot.Axes.Bottom.MajorTickStyle.Length = 0;
            radarPlotResults.Plot.HideGrid();

            // Auto-scale with no padding beneath the bars
            radarPlotResults.Plot.Axes.Margins(bottom: 0);
            radarPlotResults.Plot.Axes.SetLimitsY(0, 100);

            barDice.Color = Colors.Green;
            barPrecision.Color = Colors.Green;
            barRecall.Color = Colors.Green;
            barFpr.Color = Colors.Green;
            barDiceLoss.Color = Colors.Red;

            if (IsDarkMode())
            {
                radarPlotResults.Plot.Axes.Bottom.TickLabelStyle.ForeColor = Colors.White;
                radarPlotResults.Plot.Axes.Left.TickLabelStyle.ForeColor = Colors.White;
            }
            else
            {
                radarPlotResults.Plot.Axes.Bottom.TickLabelStyle.ForeColor = Colors.Black;
                radarPlotResults.Plot.Axes.Left.TickLabelStyle.ForeColor = Colors.Black;
            }

            radarPlotResults.Refresh();
        }

        private void radarPlotResults_MouseMove(object sender, MouseEventArgs e)
        {
            if (segmentationStats == null)
                return;

            var plt = radarPlotResults.Plot;

            // Convert mouse pixel -> coordinate
            var coordinates = plt.GetCoordinates(e.X, e.Y);
            double x = coordinates.X;
            double y = coordinates.Y;

            // Bar centers: 1,2,3,4,5
            double[] centers = { 1, 2, 3, 4, 5 };

            // 5 metric values (already displayed as percentages)
            double[] values =
            {
                segmentationStats.Dice * 100,
                segmentationStats.Precision * 100,
                segmentationStats.Recall * 100,
                segmentationStats.FPR * 100,
                (1 - segmentationStats.Dice) * 100
            };

            int? hoveredIndex = null;

            for (int i = 0; i < centers.Length; i++)
            {
                double cx = centers[i];
                double barHeight = values[i];
                double dx = Math.Abs(x - cx);

                // Inside bar horizontally and vertically?
                if (dx < 0.5 && y >= 0 && y <= barHeight)
                {
                    hoveredIndex = i;
                    break;
                }
            }

            // If hover target hasn't changed, do nothing → prevents flicker
            if (hoveredIndex == lastHoveredBarIndex)
                return;

            lastHoveredBarIndex = hoveredIndex;

            if (hoveredIndex is int idx)
            {
                // Update tooltip text once, let WinForms handle display
                string text =
                    $"{metricInfo[idx].Name}: {values[idx]:0.0}%\r\n{metricInfo[idx].Description}";

                radarTooltip.Show(text, radarPlotResults, e.X + 15, e.Y + 15);
            }
            else
            {
                // No bar hovered → clear tooltip
                radarTooltip.SetToolTip(radarPlotResults, string.Empty);
            }
        }
    }
}
