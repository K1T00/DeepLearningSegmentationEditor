using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace AnnotationTool.Core.Utils
{

    public static class BitmapIO
    {

        /// <summary>
        /// Load bitmap from file without locking it (so it can be deleted/moved while in use)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Bitmap LoadBitmapUnlocked(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                using (var img = Image.FromStream(fs))
                {
                    return new Bitmap(img);
                }
            }
        }

        public static Bitmap CreateThumbnail(Bitmap source, int controlWidth)
        {
            var containerWidth = controlWidth - 40;

            //Preserve aspect ratio(width fixed, height scales)
            var originalW = source.Width;
            var originalH = source.Height;
            var ratio = (float)originalW / originalH;
            var thumbW = containerWidth;
            var thumbH = (int)(thumbW / ratio);
            if (thumbH < 1) thumbH = 1;

            var result = new Bitmap(thumbW, thumbH, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.Low;
                g.PixelOffsetMode = PixelOffsetMode.None;
                g.SmoothingMode = SmoothingMode.None;
                g.CompositingQuality = CompositingQuality.HighSpeed;

                g.DrawImage(source, 0, 0, thumbW, thumbH);
            }

            return result;
        }
    }
}