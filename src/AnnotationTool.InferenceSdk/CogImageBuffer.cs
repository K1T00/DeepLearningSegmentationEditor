using System;

namespace AnnotationTool.InferenceSdk
{
    public readonly struct CogImageBuffer
    {
        public readonly IntPtr Data;
        public readonly int Width;
        public readonly int Height;
        public readonly int StrideBytes;
        public readonly ImagePixelFormat PixelFormat;

        public CogImageBuffer(IntPtr data, int width, int height, int strideBytes, ImagePixelFormat pixelFormat)
        {
            this.Data = data;
            this.Width = width;
            this.Height = height;
            this.StrideBytes = strideBytes;
            this.PixelFormat = pixelFormat;
        }
    }
}
