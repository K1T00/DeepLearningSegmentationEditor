using System.Drawing.Drawing2D;

namespace AnnotationTool.App.Drawing
{
	/// <summary>
	/// Draws a brush stroke at a location.
	/// </summary>
	public class BrushTool
	{

		public void DrawBrush(int x, int y, Bitmap img, Color color, int size, CompositingMode compositing)
		{
			using var g = Graphics.FromImage(img);
			using var brush = new SolidBrush(color);

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingMode = compositing;
			g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, Bitmap img, Color color, int size, CompositingMode compositing)
		{
			using var g = Graphics.FromImage(img);
			using var pen = new Pen(color, size);

			g.SmoothingMode = SmoothingMode.AntiAlias;
			pen.StartCap = pen.EndCap = LineCap.Round;
			g.CompositingMode = compositing;
			g.DrawLine(pen, x1, y1, x2, y2);
		}
	}
}
