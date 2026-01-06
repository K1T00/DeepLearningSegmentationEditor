using AnnotationTool.Ai.Geometry;
using AnnotationTool.Ai.Inference.Decoders;
using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Processing;
using AnnotationTool.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TorchSharp;
using static AnnotationTool.Ai.Utils.TensorProcessing.TensorConversion;
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
        private readonly Device device;
        private readonly Module<Tensor, Tensor> model;
        private readonly DeepLearningSettings settings;
        private readonly int numClasses;
        private readonly SegmentationModelConfig config;

        private SegmentationInferenceSession(Module<Tensor, Tensor> model, DeepLearningSettings settings, Device device, SegmentationModelConfig config, int numClasses)
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

            if (TryDeserializeSavedPackage(json, out var settings, out var numClasses))
            {
                // ok
            }
            else
            { 
                throw new InvalidOperationException("Failed to deserialize settings from JSON.");
            }

            var config = ModelComplexityConfigFactory.Create(
                settings.TrainModelSettings.ModelComplexity,
                settings.PreprocessingSettings.SliceSize,
                settings.PreprocessingSettings.SliceSize);

            var model = SegmentationModelFactoryForSdk.Create(settings, torchDevice, config, numClasses);

            model.load(modelPath).to(config.TrainPrecision, true).eval();

            return new SegmentationInferenceSession(model, settings, torchDevice, config, numClasses);
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

        // Overload without ROI
        public IReadOnlyDictionary<int, Mat> Run(Mat image)
        {
            return Run(image, null);
        }

        private static ISegmentationDecoder CreateDecoder(SegmentationMode mode, int numClasses)
        {
            if (mode == SegmentationMode.Binary)
            {
                return new BinarySegmentationDecoder();
            }
            return new MulticlassSegmentationDecoder(numClasses); // Background allready included
        }

        private static Rect ValidateOrDefaultRoi(Mat image, Rect? roi)
        {
            if (!roi.HasValue)
                return new Rect(0, 0, image.Width, image.Height);

            var r = roi.Value;

            if (r.Width <= 0 || r.Height <= 0)
                throw new ArgumentException("ROI must have positive width and height.", nameof(roi));

            if (r.X < 0 || r.Y < 0 ||
                r.Right > image.Width ||
                r.Bottom > image.Height)
                throw new ArgumentException("ROI is outside image bounds.", nameof(roi));

            return r;
        }

        private static bool TryDeserializeSavedPackage(string json, out DeepLearningSettings settings, out int numClasses)
        {
            settings = null;
            numClasses = 0;

            var jsonOptions = new JsonSerializerOptions
            {
                // Human-readable formatting
                WriteIndented = true,

                // camelCase property names (same as your old CamelCaseNamingStrategy)
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

                // Ignore null values
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

                // Allow trailing commas in JSON files (optional but convenient)
                AllowTrailingCommas = true,

                // Avoid exceptions on comments in JSON files
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // Serialize enums as strings (recommended for readability & compatibility)
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));


            try
            {
                var pkg = JsonSerializer.Deserialize<SavedModelPackage>(json, jsonOptions);
                if (pkg == null || pkg.Settings == null || pkg.NumClasses <= 0)
                    return false;

                settings = pkg.Settings;
                numClasses = pkg.NumClasses;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            model?.Dispose();
            if (device.type == DeviceType.CUDA)
                NativeTorchCudaOps.EmptyCudaCache();
        }
    }
}
