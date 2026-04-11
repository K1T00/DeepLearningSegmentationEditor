using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Models.UNet;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using TorchSharp;
using static AnnotationTool.Ai.Utils.CudaOps.NativeTorchCudaOps;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
using static AnnotationTool.InferenceSdk.Utils;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;


namespace AnnotationTool.InferenceSdk
{
    /// <summary>
    /// 
    /// using var session = 
    /// SegmentationInferenceSession.Load(
    /// modelPath: "model.bin", 
    /// settingsJsonPath: "settings.json", 
    /// device: InferenceDevice.Cuda); // or InferenceDevice.Cpu
    /// 
    /// model.bin – trained model weights
    /// settings.json – preprocessing & model settings saved during training
    /// One session instance should be reused for multiple images
    /// 
    /// Use without ROI:
    /// Mat image = Cv2.ImRead("image.png");
    /// var result = session.Run(image);
    /// 
    /// Use with ROI:
    /// var roi = new Rect(100, 50, 512, 512);
    /// var result = session.Run(image, roi);
    /// 
    /// Read the results:
    /// 
    /// foreach (var kv in result)
    /// {
    /// int classId = kv.Key;   // 1..N (background is implicit)
    /// Mat probabilityMask = kv.Value; // probabilityMask contains per-pixel probabilities (0..1)
    /// }
    /// 
    /// Binary segmentation:
    /// { 1 → foreground probability mask }
    /// 
    /// Multiclass segmentation:
    /// {
    /// 1 → class 1 probability mask,
    ///  2 → class 2 probability mask,
    ///  ...
    /// }
    /// 
    /// </summary>
    public sealed class SegmentationInferenceSession : IDisposable
    {
        private bool disposed = false;

        private readonly Device device;
        private readonly Module<Tensor, Tensor> model;
        private readonly DeepLearningSettings settings;
        private readonly int numClasses;
        private readonly SegmentationModelConfig config;


        private SegmentationInferenceSession(
            Module<Tensor, Tensor> model,
            DeepLearningSettings settings,
            Device device,
            SegmentationModelConfig config,
            int numClasses)
        {
            this.device = device;
            this.model = model;
            this.settings = settings;
            this.numClasses = numClasses;
            this.config = config;
        }


        /// <summary>
        /// Loads a saved model (.bin) and training settings (.json).
        ///
        /// Threading:
        /// - Run() is synchronous and compute-bound. Callers may wrap it in Task.Run.
        /// - Do not call Run() concurrently on the same session instance.
        /// </summary>
        /// <param name="modelPath"></param>
        /// <param name="settingsJsonPath"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static SegmentationInferenceSession Load(string modelPath, string settingsJsonPath, InferenceDevice device)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Model path is required.", nameof(modelPath));
            if (string.IsNullOrWhiteSpace(settingsJsonPath))
                throw new ArgumentException("Settings JSON path is required.", nameof(settingsJsonPath));
            if (!File.Exists(modelPath))
                throw new FileNotFoundException("Model file not found.", modelPath);
            if (!File.Exists(settingsJsonPath))
                throw new FileNotFoundException("Settings JSON file not found.", settingsJsonPath);

            var torchDevice = new Device(device == InferenceDevice.Cuda ? DeviceType.CUDA : DeviceType.CPU);
            var json = File.ReadAllText(settingsJsonPath);

            if (InferenceJsonHelper.TryDeserializeSavedPackage(json, out var settings, out var numClasses))
            {
                // ok
            }
            else
            {
                throw new InvalidOperationException("Failed to deserialize settings from JSON.");
            }

            var segmentationMode = numClasses == 1
                ? SegmentationMode.Binary
                : SegmentationMode.Multiclass;

            var cfg = ModelComplexityConfigProviderSdk.GetConfig(settings.TrainModelSettings.SegmentationArchitecture, settings.TrainModelSettings.ModelComplexity);

            var inChannels = settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3;

            var model = UNetModelFactorySdk.Create(inChannels, numClasses, cfg, torchDevice).AsModule();

            model.load(modelPath).to(cfg.TrainPrecision, true).eval();

            return new SegmentationInferenceSession(model, settings, torchDevice, cfg, numClasses);
        }

        /// <summary>
        /// Run a trained segmentation model on a single image (optionally restricted to an ROI) and return per-class probability masks in the original image space.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public IReadOnlyDictionary<int, Mat> Run(Mat image, Rect? roi)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (image.Empty())
                throw new ArgumentException("Input image is empty.", nameof(image));

            var segmentationMode = numClasses <= 2
                ? SegmentationMode.Binary
                : SegmentationMode.Multiclass;

            var resolvedRoi = ValidateOrDefaultRoi(image, roi);

            var space = new SegmentationImageSpace(
                new OpenCvSharp.Size(image.Width, image.Height),
                resolvedRoi,
                settings.PreprocessingSettings.SliceSize,
                settings.PreprocessingSettings.DownSample,
                settings.PreprocessingSettings.BorderPadding);

            var preProc = new SegmentationPreprocessor(space);
            var postProc = new SegmentationPostprocessor(space);


            Mat[] imageTiles = null;

            try
            {
                imageTiles = preProc.ProcessImage(image);

                using (var inputTensor = SlicedImageToTensor(
                    imageTiles,
                    settings.PreprocessingSettings.TrainAsGreyscale,
                    device,
                    settings.PreprocessingSettings.Normalization,
                    config.TrainPrecision))


                using (var d = NewDisposeScope())
                using (var decoder = CreateDecoder(segmentationMode, numClasses))
                using (var inference = inference_mode())
                {
                    var logits = model.call(inputTensor);
                    var predTilesDic = decoder.Decode(logits);

                    var fullMaskPredictions = new Dictionary<int, Mat>();
                    foreach (var predTiles in predTilesDic)
                    {
                        fullMaskPredictions.Add(predTiles.Key, postProc.ProcessImageTiles(predTiles.Value));
                    }
                    return fullMaskPredictions;
                }
            }
            finally
            {
                // Dispose tiles created by preprocessor
                if (imageTiles != null)
                {
                    for (int i = 0; i < imageTiles.Length; i++)
                        imageTiles[i]?.Dispose();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public IReadOnlyDictionary<int, Mat> Run(Mat image)
        {
            return Run(image, null);
        }

        public IReadOnlyDictionary<int, Bitmap> Run(Bitmap image, Rectangle? roi)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            EnsureNotDisposed();

            using (var mat = BitmapToMat(image))
            {
                IReadOnlyDictionary<int, Mat> mats =
                    Run(mat, ConvertRoi(roi));

                var result = new Dictionary<int, Bitmap>(mats.Count);

                foreach (var kv in mats)
                {
                    // Convert Mat → Bitmap
                    result[kv.Key] = MatToBitmap(kv.Value);

                    // Dispose Mat immediately (ownership ends here)
                    kv.Value.Dispose();
                }

                return result;
            }
        }

        public IReadOnlyDictionary<int, Bitmap> Run(Bitmap image)
        {
            return Run(image, null);
        }

        public IReadOnlyDictionary<int, Mat> Run(CogImageBuffer image, Rect? roi)
        {
            EnsureNotDisposed();

            if (image.Data == IntPtr.Zero)
                throw new ArgumentException("ImageBuffer.Data is null");

            using (var mat = ImageBufferToMat(image))
            {
                return Run(mat, roi);
            }
        }

        public IReadOnlyDictionary<int, Mat> Run(CogImageBuffer image)
        {
            return Run(image, null);
        }


        // Disposings
        private void EnsureNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(
                    nameof(SegmentationInferenceSession),
                    "Inference session has been disposed.");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            model?.Dispose();
            if (device.type == DeviceType.CUDA)
                EmptyCudaCache();
        }


        //! For Cognex CogImage conversion !//

        //public static CogImageBuffer FromCogImage(CogImage8Grey img)
        //{
        //    var pm = img.Get8GreyPixelMemory(
        //        CogImageDataModeConstants.Read,
        //        0, 0, img.Width, img.Height);

        //    return new CogImageBuffer(
        //        pm.Scan0,
        //        img.Width,
        //        img.Height,
        //        pm.Stride,
        //        ImagePixelFormat.Gray8);
        //}

        //public static CogImageBuffer FromCogPlanar(CogImage24PlanarColor img)
        //{
        //    img.Get24PlanarColorPixelMemory(
        //        CogImageDataModeConstants.Read,
        //        0, 0, img.Width, img.Height,
        //        out var b, out var g, out var r);

        //    // You can pass 3 separate buffers OR interleave once
        //}

    }
}
