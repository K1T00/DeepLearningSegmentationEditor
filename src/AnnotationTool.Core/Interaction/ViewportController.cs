using AnnotationTool.Core.Models;
using System.Drawing;

namespace AnnotationTool.Core.Interaction
{
    public sealed class ViewportController
    {
        private bool isPanning;
        private Point lastScreenPos;

        public void BeginPan(Point screenPos)
        {
            isPanning = true;
            lastScreenPos = screenPos;
        }

        public void UpdatePan(Point screenPos, Viewport viewport)
        {
            if (!isPanning)
                return;

            int dx = screenPos.X - lastScreenPos.X;
            int dy = screenPos.Y - lastScreenPos.Y;

            viewport.Pan(dx, dy);
            lastScreenPos = screenPos;
        }

        public void EndPan()
        {
            isPanning = false;
        }

        public void Zoom(int mouseWheelDelta, Point screenAnchor, Viewport viewport)
        {
            float factor = mouseWheelDelta > 0 ? 1.1f : 0.9f;
            viewport.SetZoom(viewport.Zoom * factor, screenAnchor);
        }

        public void Reset()
        {
            isPanning = false;
        }
    }
}
