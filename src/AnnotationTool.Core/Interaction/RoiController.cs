using AnnotationTool.Core.Models;
using AnnotationTool.Core.Utils;
using System.Drawing;

namespace AnnotationTool.Core.Interaction
{

    public sealed class RoiController
    {
        public RectangleF Roi { get; set; }
        public RoiMode Mode { get; set; } = RoiMode.None;

        private PointF lastImgPos;

        public RoiController(RectangleF initialRoi)
        {
            Roi = initialRoi;
        }

        public void MouseDown(Point screenPos, Viewport viewport)
        {
            var imgPos = viewport.ScreenToImage(screenPos);

            Mode = CoreUtils.GetRoiHitMode(imgPos, Roi, viewport) ?? (Roi.Contains(imgPos) 
                ? RoiMode.Moving 
                : RoiMode.None);

            lastImgPos = imgPos;
        }

        public void MouseMove(Point screenPos, Viewport viewport, SizeF imageSize)
        {
            if (Mode == RoiMode.None)
                return;

            var imgPos = viewport.ScreenToImage(screenPos);
            var delta = new PointF(
                imgPos.X - lastImgPos.X,
                imgPos.Y - lastImgPos.Y);

            var r = Roi;

            switch (Mode)
            {
                case RoiMode.Moving:
                    r.X += delta.X;
                    r.Y += delta.Y;
                    break;

                case RoiMode.ResizingNW:
                    r.X += delta.X; r.Y += delta.Y;
                    r.Width -= delta.X; r.Height -= delta.Y;
                    break;

                case RoiMode.ResizingNE:
                    r.Y += delta.Y;
                    r.Width += delta.X; r.Height -= delta.Y;
                    break;

                case RoiMode.ResizingSW:
                    r.X += delta.X;
                    r.Width -= delta.X; r.Height += delta.Y;
                    break;

                case RoiMode.ResizingSE:
                    r.Width += delta.X; r.Height += delta.Y;
                    break;
            }

            Roi = CoreUtils.ClampRoi(r, imageSize.Width, imageSize.Height);
            lastImgPos = imgPos;
        }

        public void MouseUp()
        {
            Mode = RoiMode.None;
        }

        public Rectangle GetRoundedRoi()
        {
            return Rectangle.Round(Roi);
        }
    }

}
