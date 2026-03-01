using AnnotationTool.Core.Interaction;
using AnnotationTool.Core.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AnnotationTool.App.Rendering
{
    /// <summary>
    /// Stateless renderer + fast overlay updater.
    ///
    /// Responsibilities:
    /// - Draw bitmaps in viewport space (no bitmap ownership)
    /// - Draw ROI and UI overlays
    /// - Incrementally update annotation overlay bitmap from LabelMask (dirty rect)
    ///
    /// IMPORTANT:
    /// - Callers must synchronize bitmap access (AnnotationLock / HeatmapLock) if the bitmap
    ///   can be LockBits()'d and DrawImage()'d concurrently.
    /// - This class does NOT lock; it stays UI/framework agnostic.
    /// </summary>
    public static class OverlayRenderer
    {
        /// <summary>
        /// Draw a bitmap in image space using the viewport transform.
        /// This is for "image-space" bitmaps: FullImage, AnnotationOverlay, Heatmaps that match image size.
        /// </summary>
        public static void DrawImage(Graphics g, Bitmap image, Viewport viewport)
        {
            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(viewport);

            var imgRect = viewport.ImageToScreenRect(new RectangleF(0, 0, image.Width, image.Height));

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.DrawImage(
                image,
                imgRect.X,
                imgRect.Y,
                imgRect.Width,
                imgRect.Height);
        }

        /// <summary>
        /// Draw ROI rectangle (in image coordinates) onto the screen.
        /// </summary>
        public static void DrawRoi(Graphics g, RoiController roiController, Viewport viewport)
        {
            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(roiController);
            ArgumentNullException.ThrowIfNull(viewport);

            var roi = roiController.Roi;
            var roiScreen = viewport.ImageToScreenRect(roi);

            using var pen = new Pen(Color.Red, 2f);
            pen.Alignment = PenAlignment.Inset;

            g.DrawRectangle(
                pen,
                roiScreen.X,
                roiScreen.Y,
                roiScreen.Width,
                roiScreen.Height);
        }

        public static void DrawBrushIndicator(Graphics g, RoiController roiController, Viewport viewport, PictureBox mainPictureBox, int currentBrushSize)
        {
            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(roiController);
            ArgumentNullException.ThrowIfNull(viewport);
            ArgumentNullException.ThrowIfNull(mainPictureBox);

            //var roi = roiController.Roi;
            //var roiScreen = viewport.ImageToScreenRect(roi);

            using var pen = new Pen(Color.Blue, 4) { DashStyle = DashStyle.Solid };
            g.DrawEllipse(
                pen,
                mainPictureBox.Width / 2 - currentBrushSize / 2,
                mainPictureBox.Height / 2 - currentBrushSize / 2,
                (int)(currentBrushSize * viewport.Zoom),
                (int)(currentBrushSize * viewport.Zoom));
        }

        /// <summary>
        /// Optional: draw debug text in screen space.
        /// </summary>
        public static void DrawText(Graphics g, string text, Point screenPos)
        {
            ArgumentNullException.ThrowIfNull(g);

            using var font = new Font("Segoe UI", 9);
            using var brush = new SolidBrush(Color.White);
            g.DrawString(text, font, brush, screenPos);
        }


        /// <summary>
        /// Incrementally updates a rectangular region of an ARGB annotation overlay bitmap from the LabelMask.
        ///
        /// - overlay: Format32bppArgb bitmap (visual-only)
        /// - mask: byte-per-pixel class ids (truth)
        /// - classIdToColor: maps classId -> RGB color (background 0 should not exist or may map to transparent)
        /// - imageRect: dirty region in IMAGE coordinates
        /// - overlayAlpha: 0..255 alpha for non-background pixels
        ///
        /// IMPORTANT:
        /// - Caller must hold the overlay's synchronization lock around calls to this method AND around drawing.
        /// </summary>
        public static void UpdateAnnotationOverlayRegion(
            Bitmap overlay,
            LabelMask mask,
            IReadOnlyDictionary<int, Color> featureColorMap,
            Rectangle imageRect,
            byte overlayAlpha)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(mask);
            ArgumentNullException.ThrowIfNull(featureColorMap);

            if (imageRect.Width <= 0 || imageRect.Height <= 0)
                return;

            var data = overlay.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* dstBase = (byte*)data.Scan0;
                    int dstStride = data.Stride;

                    for (int y = 0; y < imageRect.Height; y++)
                    {
                        int imgY = imageRect.Y + y;
                        byte* dstRow = dstBase + y * dstStride;

                        int maskRow = imgY * mask.Width;

                        for (int x = 0; x < imageRect.Width; x++)
                        {
                            int imgX = imageRect.X + x;
                            byte classId = mask.Data[maskRow + imgX];

                            int i = x * 4;

                            if (classId == 0 ||
                                !featureColorMap.TryGetValue(classId, out var c))
                            {
                                // Background = fully transparent in visualization overlay
                                dstRow[i + 0] = 0;
                                dstRow[i + 1] = 0;
                                dstRow[i + 2] = 0;
                                dstRow[i + 3] = 0;
                            }
                            else
                            {
                                dstRow[i + 0] = c.B;
                                dstRow[i + 1] = c.G;
                                dstRow[i + 2] = c.R;
                                dstRow[i + 3] = overlayAlpha;
                            }
                        }
                    }
                }
            }
            finally
            {
                overlay.UnlockBits(data);
            }
        }

        /// <summary>
        /// Draws a semi-transparent grayscale mask (e.g. prediction or label).
        /// </summary>
        public static void DrawAnnotation(Graphics g, Bitmap mask, Viewport viewport, float opacity = 0.7f)
        {
            if (mask == null)
                return;

            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(viewport);

            var rect = viewport.ImageToScreenRect(
                new RectangleF(0, 0, mask.Width, mask.Height));

            using var attr = new ImageAttributes();
            var matrix = new ColorMatrix
            {
                Matrix33 = opacity
            };

            attr.SetColorMatrix(matrix);

            g.DrawImage(
                mask,
                Rectangle.Round(rect),
                0,
                0,
                mask.Width,
                mask.Height,
                GraphicsUnit.Pixel,
                attr);
        }


        // Draw feature size rectangle at top-left of the PictureBox
        public static void DrawSliceSizeRectangle(Graphics g, RoiController roiController, Viewport viewport, int sliceSize, int downSample)
        {
            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(roiController);
            ArgumentNullException.ThrowIfNull(viewport);

            var effectiveSize = sliceSize * (1 << downSample);
            //var roi = roiController.Roi;
            //var roiScreen = viewport.ImageToScreenRect(roi);
            var screenRect = new Rectangle(10, 10, (int)(effectiveSize * viewport.Zoom), (int)(effectiveSize * viewport.Zoom));

            using var p = new Pen(Color.Green, 2);
            g.DrawRectangle(p, screenRect);

        }

        /// <summary>
        /// Draws a heatmap bitmap aligned with the image.
        /// </summary>
        public static void DrawHeatmap(Graphics g, Bitmap heatmap, Viewport viewport, float opacity = 0.7f)
        {
            if (heatmap == null)
                return;

            ArgumentNullException.ThrowIfNull(g);
            ArgumentNullException.ThrowIfNull(viewport);

            var rect = viewport.ImageToScreenRect(
                new RectangleF(0, 0, heatmap.Width, heatmap.Height));

            using var attr = new ImageAttributes();

            // Treat pixels that are exactly black as transparent (your below-threshold pixels)
            attr.SetColorKey(Color.Black, Color.Black);

            var matrix = new ColorMatrix
            {
                Matrix33 = opacity
            };
            attr.SetColorMatrix(matrix);

            g.DrawImage(
                heatmap,
                Rectangle.Round(rect),
                0,
                0,
                heatmap.Width,
                heatmap.Height,
                GraphicsUnit.Pixel,
                attr);
        }

    }
}
