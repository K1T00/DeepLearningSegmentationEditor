using System;
using System.Drawing;

namespace AnnotationTool.Core.Models
{
	public sealed class Viewport
	{
		public float Zoom { get; set; } = 1f;
        public PointF Offset { get; set; } = new PointF(0, 0); // screen px offset of image origin
		public Size ImageSize { get; set; } = Size.Empty;

		public PointF ScreenToImage(Point p)
			=> new PointF((p.X - Offset.X) / Zoom, (p.Y - Offset.Y) / Zoom);

		public Point ImageToScreen(PointF img)
			=> new Point((int)Math.Round(img.X * Zoom + Offset.X),
				(int)Math.Round(img.Y * Zoom + Offset.Y));

		public Rectangle ImageToScreenRect(Rectangle r)
		{
			var tl = ImageToScreen(new PointF(r.Left, r.Top));
			var br = ImageToScreen(new PointF(r.Right, r.Bottom));
			return Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
		}

	}
}
