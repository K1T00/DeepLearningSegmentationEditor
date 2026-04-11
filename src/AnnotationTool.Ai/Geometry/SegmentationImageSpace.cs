using OpenCvSharp;
using System;
using static AnnotationTool.Ai.Utils.ImageProcessing.ImageUtils;


namespace AnnotationTool.Ai.Geometry
{
    /// <summary>
    /// Defines the geometric image space in which a segmentation model operates.
    /// 
    /// This object is the SINGLE source of truth for:
    /// - ROI
    /// - Downsampling
    /// - Tile grid
    /// - Reconstruction
    /// - full image paste back
    /// 
    /// Both training and inference must use the same instance/config:
    /// Original image
    ///   ↓ ExtractRoi()
    /// ROI image
    ///   ↓ Downsample + tile
    /// Model space
    ///   ↓ Decode () in Segmentation decoder
    /// ROI prediction
    ///   ↓ ReconstructToOriginal()
    /// Full-size prediction
    /// </summary>
    public sealed class SegmentationImageSpace
    {
        // Input / configuration
        public Size OriginalImageSize { get; }
        public Rect Roi { get; }
        public int SliceSize { get; }
        public int DownSample { get; }
        public bool BorderPadding { get; }

        // Derived geometry
        public Size RoiSize { get; }
        public Size WorkingSize { get; }
        public int TileRows { get; }
        public int TileCols { get; }

        /// <summary>
        /// Size of one slice in ORIGINAL image space.
        /// </summary>
        public int SliceSizeInOriginalSpace { get; }

        /// <summary>
        /// Creates a segmentation image space definition.
        /// </summary>
        public SegmentationImageSpace(Size originalImageSize, Rect roi, int sliceSize, int downSample, bool borderPadding)
        {
            this.OriginalImageSize = originalImageSize;
            this.Roi = roi;
            this.SliceSize = sliceSize;
            this.DownSample = downSample;
            this.BorderPadding = borderPadding;

            // Slice size in original image coordinates
            this.SliceSizeInOriginalSpace = sliceSize << downSample;

            // Ensure ROI is at least one slice large
            var roiWidth = Math.Max(roi.Width, SliceSizeInOriginalSpace);
            var roiHeight = Math.Max(roi.Height, SliceSizeInOriginalSpace);

            this.RoiSize = new Size(roiWidth, roiHeight);

            // Downsampled working size (model input space)
            this.WorkingSize = new Size(roiWidth >> downSample, roiHeight >> downSample);

            // Compute tile grid
            ComputeTileGrid(WorkingSize, SliceSize, BorderPadding, out var tileRows, out var tileCols);

            this.TileRows = tileRows;
            this.TileCols = tileCols;
        }

        /// <summary>
        /// Computes the tile grid dimensions for a given working size.
        /// This logic MUST be shared by training and inference.
        /// </summary>
        private static void ComputeTileGrid(Size workingSize, int sliceSize, bool borderPadding, out int rows, out int cols)
        {
            if (borderPadding)
            {
                rows = (int)Math.Ceiling((double)workingSize.Height / sliceSize);
                cols = (int)Math.Ceiling((double)workingSize.Width / sliceSize);
            }
            else
            {
                rows = workingSize.Height / sliceSize;
                cols = workingSize.Width / sliceSize;
            }

            rows = Math.Max(1, rows);
            cols = Math.Max(1, cols);
        }

        /// <summary>
        /// Reconstructs a model-space prediction into full original image space.
        /// This method performs ALL reverse geometry steps:
        /// UpSample → pad → paste ROI.
        /// </summary>
        public Mat ReconstructToOriginal(Mat modelPrediction)
        {
            using (var roiUpSampled = UpSampleImage(modelPrediction, DownSample))
            {
                var fullSizeImg = new Mat(OriginalImageSize.Height, OriginalImageSize.Width, roiUpSampled.Type(), Scalar.Black);

                CopyMatWithClipping(roiUpSampled, fullSizeImg, Roi.X, Roi.Y);

                return fullSizeImg;
            }
        }

        /// <summary>
        /// Creates an empty full-size prediction map.
        /// Useful for debugging and advanced decoders.
        /// </summary>
        public Mat CreateEmptyOriginalMap(MatType type)
        {
            return new Mat(OriginalImageSize.Height, OriginalImageSize.Width, type, Scalar.Black);
        }

        /// <summary>
        /// Applies ROI cropping to an image.
        /// This is the forward mapping (original → ROI).
        /// </summary>
        public Mat ExtractRoi(Mat original)
        {
            return new Mat(original, Roi);
        }

        public Mat UpSampleToRoi(Mat modelPrediction)
        {
            return UpSampleImage(modelPrediction, DownSample);
        }

        public Mat PadToRoi(Mat roiPrediction)
        {
            var roiMap = new Mat(Roi.Height, Roi.Width, roiPrediction.Type(), Scalar.Black);

            roiPrediction.CopyTo(
                roiMap[new Rect(
                    0,
                    0,
                    Math.Min(roiPrediction.Width, roiMap.Width),
                    Math.Min(roiPrediction.Height, roiMap.Height))]);

            return roiMap;
        }

        public override string ToString()
        {
            return
                "SegmentationImageSpace {" +
                " Original=" + OriginalImageSize +
                ", ROI=" + Roi +
                ", Working=" + WorkingSize +
                ", Slice=" + SliceSize +
                ", DownSample=" + DownSample +
                ", Tiles=" + TileRows + "x" + TileCols +
                " }";
        }
    }
}
