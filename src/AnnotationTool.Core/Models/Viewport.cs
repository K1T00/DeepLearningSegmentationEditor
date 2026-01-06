using System;
using System.Drawing;

namespace AnnotationTool.Core.Models
{
    public sealed class Viewport
    {
        public float Zoom { get; private set; } = 1f;
        public PointF Offset { get; private set; } = PointF.Empty;

        public Viewport() : this(1f, PointF.Empty)
        {
        }

        public Viewport(float zoom, PointF offset)
        {
            Zoom = zoom;
            Offset = offset;
        }

        // -------------------------------
        // Coordinate transforms
        // -------------------------------

        public PointF ScreenToImage(Point screen)
        {
            return new PointF(
                (screen.X - Offset.X) / Zoom,
                (screen.Y - Offset.Y) / Zoom);
        }

        public PointF ImageToScreen(PointF image)
        {
            return new PointF(
                image.X * Zoom + Offset.X,
                image.Y * Zoom + Offset.Y);
        }

        public RectangleF ImageToScreenRect(RectangleF imgRect)
        {
            var tl = ImageToScreen(new PointF(imgRect.Left, imgRect.Top));
            var br = ImageToScreen(new PointF(imgRect.Right, imgRect.Bottom));

            return RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y);
        }

        public RectangleF ScreenToImageRect(RectangleF screenRect)
        {
            var tl = ScreenToImage(Point.Round(screenRect.Location));
            var br = ScreenToImage(Point.Round(
                new PointF(screenRect.Right, screenRect.Bottom)));

            return RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y);
        }

        // -------------------------------
        // View manipulation
        // -------------------------------

        public void SetZoom(float zoom, PointF anchorScreen)
        {
            var anchorImg = ScreenToImage(Point.Round(anchorScreen));

            Zoom = zoom;

            var newAnchorScreen = ImageToScreen(anchorImg);
            Offset = new PointF(
                Offset.X + (anchorScreen.X - newAnchorScreen.X),
                Offset.Y + (anchorScreen.Y - newAnchorScreen.Y));
        }

        public void Pan(float dx, float dy)
        {
            Offset = new PointF(Offset.X + dx, Offset.Y + dy);
        }

        public void FitToView(Size imageSize, Size viewSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0)
                return;

            float scaleX = (float)viewSize.Width / imageSize.Width;
            float scaleY = (float)viewSize.Height / imageSize.Height;

            // Use the smaller scale so the whole image fits
            Zoom = Math.Min(scaleX, scaleY);

            // Center image
            float scaledWidth = imageSize.Width * Zoom;
            float scaledHeight = imageSize.Height * Zoom;

            Offset = new PointF(
                (viewSize.Width - scaledWidth) / 2f,
                (viewSize.Height - scaledHeight) / 2f);
        }

    }

}
