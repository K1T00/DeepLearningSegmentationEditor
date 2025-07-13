using System;
using System.Threading;
using System.IO;
using OpenCvSharp;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using AiOps.CudaOps;
using static AiOps.AiUtils.HelperFunctions;
using static AiOps.AiUtils.TensorImageCalcHelper;


namespace AiOps.AiModels.UNet
{

    public class UNetOperator
    {

        public void Execute(
            Module<Tensor, Tensor> model,
            Device runOnDevice,
			UNetData dataPara,
            UNetModelParameter modelPara,
            int heatmapThreshold,
            IProgress<string> logProgress,
            IProgress<int> barUpdateProgress,
            CancellationToken token)
        { 
	        
	        using (var image = new Mat())
            {
                using (var d = NewDisposeScope())
                //using (var noGrad = no_grad())
				using (var inferenceMode = inference_mode())
				{
					logProgress.Report("Running model on " + dataPara.DatasetImages.Count + " images");

                    var i = 0;
                    foreach (var dImage in dataPara.DatasetImages)
                    {
                        if (modelPara.TrainImagesAsGreyscale)
                        {
                            Cv2.ImRead(dImage.Path, ImreadModes.Grayscale).CopyTo(image);
                        }
                        else
                        {
                            Cv2.ImRead(dImage.Path, ImreadModes.Color).CopyTo(image);
                        }

                        var imageDs = DownSampleImage(image, dataPara.DownSampling);
                        var (nRowImages, nColumnImages) = GetAmtImages(imageDs.Height, imageDs.Width, dataPara.SliceRoi, dataPara.WithBorderPadding);
                        var slicedImages = SliceImage(imageDs, dataPara.SliceRoi, dataPara.WithBorderPadding, nRowImages, nColumnImages);
                        var roiImagesTensor = SlicedImageToTensor(slicedImages, modelPara.TrainImagesAsGreyscale, runOnDevice, modelPara.TrainPrecision).Result;
                        var prediction = model.call(roiImagesTensor);
                        var predictionSplit = TensorTo2DArray(functional.sigmoid(prediction));
                        var resPredictionGreyImages = SlicedImageTensorToImage(predictionSplit).Result;
                        var mergedPredictionGreyImage = MergeImages(resPredictionGreyImages, nRowImages, nColumnImages);
                        var predictionImageUs = UpSampleImage(mergedPredictionGreyImage, dataPara.DownSampling);

						// Depending on the up-sampled prediction image dimensions we need to add padding to the right and bottom of the image
						var paddedPredictionImageUs = new Mat();
						Cv2.CopyMakeBorder(
							predictionImageUs,
							paddedPredictionImageUs, 
							0,
							image.Height - predictionImageUs.Height < 0 ? 0 : image.Height - predictionImageUs.Height, 
							0,
							image.Width - predictionImageUs.Width < 0 ? 0 : image.Width - predictionImageUs.Width, 
							BorderTypes.Constant, 
							OpenCvSharp.Scalar.Black);

						var imageWithHeatmapOverlay = ImageToHeatmap(
							image,
							paddedPredictionImageUs[0, image.Height, 0, image.Width], heatmapThreshold);

                        Cv2.ImWrite(Path.Combine(dataPara.TestHeatmapsPath, i + ".png"), imageWithHeatmapOverlay);

                        i++;

                        barUpdateProgress.Report(i == dataPara.DatasetImages.Count ? 100 : (int)(100.0 / dataPara.DatasetImages.Count * i));

                        d.DisposeEverything();

                        if (modelPara.TrainOnDevice == DeviceType.CUDA)
                        {
                            NativeOps.EmptyCudaCache();
                        }

                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
