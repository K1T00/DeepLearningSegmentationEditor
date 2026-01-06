using System;
using System.Drawing;
using AnnotationTool.Core.Models;

namespace AnnotationTool.Core.Interaction
{
    /// <summary>
    /// Handles brush-based painting and erasing on a label mask.
    /// UI-agnostic: receives screen positions, operates in image space.
    /// </summary>
    public sealed class BrushController
    {
        private bool isPainting;
        private int brushRadius;
        private byte activeClassId;

        /// <summary>
        /// Starts a new brush stroke.
        /// </summary>
        public void BeginStroke(Point screenPos, Viewport viewport, int brushSize, byte activeClassId)
        {
            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            this.isPainting = true;
            this.brushRadius = Math.Max(1, brushSize / 2);
            this.activeClassId = activeClassId;
        }

        public bool UpdateStroke(Point screenPos, Viewport viewport, LabelMask mask, out Rectangle dirtyImageRect)
        {
            dirtyImageRect = Rectangle.Empty;

            if (!isPainting)
                return false;

            var imgPos = viewport.ScreenToImage(screenPos);
            var p = new Point((int)imgPos.X, (int)imgPos.Y);

            int r = brushRadius;

            // Clamp
            if (p.X < 0 || p.Y < 0 || p.X >= mask.Width || p.Y >= mask.Height)
                return false;

            // Draw into mask
            mask.FillCircle(p.X, p.Y, r, activeClassId);

            // Compute dirty rect in IMAGE coordinates
            dirtyImageRect = new Rectangle(
                p.X - r,
                p.Y - r,
                r * 2 + 1,
                r * 2 + 1);

            dirtyImageRect.Intersect(
                new Rectangle(0, 0, mask.Width, mask.Height));

            return true;
        }

        public void EndStroke()
        {
            isPainting = false;
        }

        private static void DrawLine(Point p0, Point p1, LabelMask mask, byte classId, int radius)
        {
            int dx = Math.Abs(p1.X - p0.X);
            int dy = Math.Abs(p1.Y - p0.Y);

            int steps = Math.Max(dx, dy);
            if (steps == 0)
            {
                mask.FillCircle(p0.X, p0.Y, radius, classId);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                int x = p0.X + (p1.X - p0.X) * i / steps;
                int y = p0.Y + (p1.Y - p0.Y) * i / steps;

                mask.FillCircle(x, y, radius, classId);
            }
        }
    }
}
