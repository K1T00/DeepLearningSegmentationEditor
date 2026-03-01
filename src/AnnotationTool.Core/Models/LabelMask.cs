using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AnnotationTool.Core.Models
{

    /// <summary>
    /// 8-bit per pixel label mask. Each byte is a class id (0 = background).
    /// </summary>
    public class LabelMask
    {
        public int Width { get; }
        public int Height { get; }
        /// <summary>Raw row-major buffer, length = Width * Height.</summary>
        /// for codec
        public byte[] Data { get; }
        public readonly struct Delta
        {
            public readonly int Index;
            public readonly byte From;
            public readonly byte To;
            public Delta(int index, byte from, byte to)
            {
                Index = index; From = from; To = to;
            }
        }

        public LabelMask(int width, int height, byte fill = 0)
        {
            Width = width;
            Height = height;
            Data = new byte[width * height];

            if (fill == 0)
                return;

            for (var i = 0; i < Data.Length; i++)
            {
                Data[i] = fill;
            }
        }

        public LabelMask(int w, int h, byte[] existing)
        {
            Width = w;
            Height = h;
            Data = existing;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int x, int y) => y * Width + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(int x, int y) => Data[IndexOf(x, y)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public void Set(int x, int y, byte value, IList<Delta> deltas = null)
        {
            var idx = IndexOf(x, y);
            var from = Data[idx];
            if (from == value) return;
            Data[idx] = value;
            deltas?.Add(new Delta(idx, from, value));
        }

        /// <summary>
        /// Paint a filled circle (disk) with class <paramref name="cls"/>
        /// Returns count of changed pixels.
        /// </summary>
        public int FillCircle(int cx, int cy, int radius, byte cls, IList<Delta> deltas = null)
        {
            var changed = 0;
            var r2 = radius * radius;
            var y0 = Math.Max(0, cy - radius);
            var y1 = Math.Min(Height - 1, cy + radius);

            for (var y = y0; y <= y1; y++)
            {
                var dy = y - cy;
                var span = (int)Math.Floor(Math.Sqrt(r2 - dy * dy));
                var x0 = Math.Max(0, cx - span);
                var x1 = Math.Min(Width - 1, cx + span);
                var row = y * Width;

                for (var x = x0; x <= x1; x++)
                {
                    var idx = row + x;
                    var from = Data[idx];
                    if (from == cls) continue;
                    Data[idx] = cls;
                    deltas?.Add(new Delta(idx, from, cls));
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>
        /// Draw a line using integer Bresenham. For thickness &gt; 1, stamps small disks along the path.
        /// Returns count of changed pixels.
        /// </summary>
        public int DrawLine(int x0, int y0, int x1, int y1, byte cls, int thickness = 1, IList<Delta> deltas = null)
        {
            if (!InBounds(x0, y0) && !InBounds(x1, y1))
            {
                // still run Bresenham and let Set/FillCircle guard bounds via InBounds checks below
            }

            var changed = 0;
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;
            var rad = Math.Max(0, (thickness - 1) / 2);

            while (true)
            {
                if (InBounds(x0, y0))
                {
                    if (rad == 0)
                    {
                        int idx = IndexOf(x0, y0);
                        byte from = Data[idx];
                        if (from != cls)
                        {
                            Data[idx] = cls;
                            deltas?.Add(new Delta(idx, from, cls));
                            changed++;
                        }
                    }
                    else
                    {
                        changed += FillCircle(x0, y0, rad, cls, deltas);
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = err << 1;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }

            return changed;
        }

        /// <summary>
        /// Remaps class ids in-place according to a mapping table.
        /// Any value not found in the map is set to background (0).
        /// </summary>
        public void RemapClasses(Dictionary<int, byte> remap, IList<Delta> deltas = null)
        {
            if (remap == null)
                throw new ArgumentNullException(nameof(remap));

            var data = Data;

            for (int i = 0; i < data.Length; i++)
            {
                var oldVal = data[i]; // byte -> int
                byte newVal;

                if (!remap.TryGetValue(oldVal, out newVal))
                    newVal = 0;

                if (oldVal == newVal)
                    continue;

                data[i] = newVal;
                deltas?.Add(new Delta(i, oldVal, newVal));
            }
        }


        public static void SavePng8(string path, LabelMask lm)
        {
            using (var bmp = new Bitmap(lm.Width, lm.Height, PixelFormat.Format8bppIndexed))
            {
                var pal = bmp.Palette;
                for (int i = 0; i < 256; i++)
                {
                    pal.Entries[i] = Color.FromArgb(255, i, i, i);
                }
                bmp.Palette = pal;

                var rect = new Rectangle(0, 0, lm.Width, lm.Height);
                var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                try
                {
                    int stride = bd.Stride;
                    IntPtr scan0 = bd.Scan0;
                    for (int y = 0; y < lm.Height; y++)
                    {
                        Marshal.Copy(lm.Data, y * lm.Width, scan0 + y * stride, lm.Width);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bd);
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        public static bool TryLoadPng8(string path, out LabelMask lm)
        {
            lm = null;
            if (!File.Exists(path)) return false;

            using (var fs = File.OpenRead(path))
            using (var srcBmp = (Bitmap)Image.FromStream(fs))
            {
                // Force 8bpp indexed clone
                using (var bmp = srcBmp.PixelFormat == PixelFormat.Format8bppIndexed
                    ? srcBmp
                    : srcBmp.Clone(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), PixelFormat.Format8bppIndexed))
                {
                    var w = bmp.Width;
                    var h = bmp.Height;
                    var data = new byte[w * h];

                    var rect = new Rectangle(0, 0, w, h);
                    var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                    try
                    {
                        int stride = bd.Stride;
                        IntPtr scan0 = bd.Scan0;
                        for (int y = 0; y < h; y++)
                        {
                            Marshal.Copy(scan0 + y * stride, data, y * w, w);
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bd);
                    }
                    lm = new LabelMask(w, h, data);
                }
            }
            return true;
        }

        public void CopyFrom(LabelMask other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other.Width != Width || other.Height != Height)
                throw new ArgumentException(
                    "Source LabelMask dimensions do not match target.");

            Buffer.BlockCopy(other.Data, 0, Data, 0, Data.Length);
        }

    }

}
