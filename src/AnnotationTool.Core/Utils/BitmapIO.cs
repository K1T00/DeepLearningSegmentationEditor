using System.Drawing;
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
	}
}