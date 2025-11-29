using System.Drawing.Drawing2D;

namespace AnnotationTool.App.Controls
{
    public partial class PulsingSpinner : UserControl
    {
        private readonly System.Windows.Forms.Timer timer;
        private int angle;
        private double pulsePhase;
        private readonly int dotCount = 12;

        public Color AccentColor { get; set; } = Color.DeepSkyBlue;
        public int DotSize { get; set; } = 8;
        public int Radius { get; set; } = 18;
        public int Speed { get; set; } = 100; // ms per frame
        public bool AutoStart { get; set; } = true;

        public PulsingSpinner()
        {
            DoubleBuffered = true;
            Size = new Size(60, 60);

            timer = new System.Windows.Forms.Timer { Interval = Speed };


            timer.Tick += (s, e) =>
            {
                angle = (angle + 30) % 360;
                pulsePhase += 0.2;
                Invalidate();
            };

            if (AutoStart)
                timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var center = new Point(Width / 2, Height / 2);
            var baseColor = AccentColor;

            for (var i = 0; i < dotCount; i++)
            {
                var a = (Math.PI * 2 / dotCount) * i + angle * Math.PI / 180.0;
                var x = center.X + (int)(Math.Cos(a) * Radius);
                var y = center.Y + (int)(Math.Sin(a) * Radius);

                // Pulse intensity varies with distance from the leading dot
                var pulse = 0.5 + 0.5 * Math.Sin(pulsePhase + i * 0.5);
                var alpha = (int)(150 + 105 * pulse);

                using var brush = new SolidBrush(Color.FromArgb(alpha, baseColor));
                e.Graphics.FillEllipse(brush, x - DotSize / 2, y - DotSize / 2, DotSize, DotSize);
            }
        }
    }
}
