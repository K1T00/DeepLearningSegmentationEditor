using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using AiOps.AiModels.UNet;
using static AiOps.AiUtils.HelperFunctions;
using static AiOps.AiUtils.TensorImageCalcHelper;
using System.Xml.Serialization;
using AiOps.CudaOps;


namespace RunAiModel
{
	public partial class MainForm : Form
	{

		public MainForm()
		{
			InitializeComponent();
			InitializeGui();
		}

		private async void btnRunModel_Click(object sender, EventArgs e)
		{
			btnRunModel.Enabled = false;

			try
			{
				progressBar1.Value = 0;
				pBImageToAnalyze.Image = null;
				pbImageAnalyzeResult.Image = null;


				var runOnDevice = new Device(rBTrainOnDeviceCuda.Checked ? DeviceType.CUDA : DeviceType.CPU);
				var (modelPara, dataPara) = LoadSettings(Path.Combine(tbProjectPath.Text));
				modelPara.TrainOnDevice = rBTrainOnDeviceCuda.Checked ? DeviceType.CUDA : DeviceType.CPU;

                dataPara.TestGrabsPath = Path.Combine(tbProjectPath.Text, dataPara.TestFolder, dataPara.GrabsFolder);
				dataPara.ModelPath = Path.Combine(tbProjectPath.Text, dataPara.ModelFolder);

                var imageFiles =
                    Directory.GetFiles(Path.Combine(dataPara.TestGrabsPath), "*", SearchOption.TopDirectoryOnly);

                if (imageFiles.Length == 0)
                {
                    throw new FileNotFoundException("No images found.");
                }

                using var image = Cv2.ImRead(imageFiles[0],
                    modelPara.TrainImagesAsGreyscale ? ImreadModes.Grayscale : ImreadModes.Color);

				using var imageDs = DownSampleImage(image, dataPara.DownSampling);
				var (nRowImages, nColumnImages) = GetAmtImages(imageDs.Height, imageDs.Width, dataPara.SliceRoi, dataPara.WithBorderPadding);


				pBImageToAnalyze.Image = image.ToBitmap();
				progressBar1.Value = 30;

				
				var slicedImages = SliceImage(imageDs, dataPara.SliceRoi, dataPara.WithBorderPadding, nRowImages, nColumnImages);

				Tensor roiImagesTensor = null;

				await Task.Run(() =>
				{
					roiImagesTensor = SlicedImageToTensor(slicedImages, modelPara.TrainImagesAsGreyscale, runOnDevice, modelPara.TrainPrecision).Result;

				}).ConfigureAwait(true);

				//using (var model = new UNetTestModel(modelPara))
				using var model = new UNetModel(modelPara);

				model.load(Path.Combine(dataPara.ModelPath, dataPara.ModelWeightsFile)).to(runOnDevice, true);
				model.eval();
				//no_grad();
				inference_mode();

				progressBar1.Value = 60;
				var sw = new Stopwatch();
				sw.Restart();

				using var d = NewDisposeScope();

				var prediction = model.call(roiImagesTensor);
				var predictionSplit = TensorTo2DArray(functional.sigmoid(prediction));


				progressBar1.Value = 90;

				// Tensor result to --> choose by user

				// Result to byte array
				if (cbCreateByteArray.Checked)
				{
					var tensFloatArray = new float[prediction.shape[0] * prediction.shape[1] * prediction.shape[2] * prediction.shape[3]];

					var l = 0;
					foreach (var pred in predictionSplit)
					{
						var predArray = pred[0].data<float>().ToArray();
						predArray.CopyTo(tensFloatArray, l * predArray.Length);
						l++;
					}
					var byteArray = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(tensFloatArray, new JsonSerializerOptions()
					{
						PropertyNamingPolicy = null,
						WriteIndented = true,
						AllowTrailingCommas = true,
						DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					}));
				}

				// Result to image or heatmap
				if (cbCreateMask.Checked | cbCreateHeatmap.Checked)
				{
					Mat[] resPredictionGreyImages = null;

					await Task.Run(() =>
					{
						resPredictionGreyImages = SlicedImageTensorToImage(predictionSplit).Result;

					}).ConfigureAwait(true);


					
					var mergedPredictionGreyImage = MergeImages(resPredictionGreyImages, nRowImages, nColumnImages);
					var predictionImageUs = UpSampleImage(mergedPredictionGreyImage, dataPara.DownSampling);

					// Depending on the up-sampled prediction image we need to add padding to the right and bottom of the image
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




					if (cbCreateHeatmap.Checked)
					{
						var imageWithHeatmapOverlay = ImageToHeatmap(
							image,
							paddedPredictionImageUs[0, image.Height, 0, image.Width], 200);

						pbImageAnalyzeResult.Image = imageWithHeatmapOverlay.ToBitmap();
					}
					else
					{
						pbImageAnalyzeResult.Image = paddedPredictionImageUs.ToBitmap();
					}
				}

				if (modelPara.TrainOnDevice == DeviceType.CUDA)
				{
					NativeOps.EmptyCudaCache();
				}

				progressBar1.Value = 100;
				tBCalcTime.Text = sw.ElapsedMilliseconds.ToString();
				tbAmtBatchImages.Text = (nRowImages * nColumnImages).ToString();
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			finally
			{
				btnRunModel.Enabled = true;
			}
		}

		private void InitializeGui()
		{
			const string projPath = @"ProjectPath.txt";

			if (!File.Exists(projPath))
			{
				var fileCreated = File.CreateText(projPath);
				fileCreated.Close();
			}
			var serializer = new Newtonsoft.Json.JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using var sr = new StreamReader(projPath);
			using var reader = new JsonTextReader(sr);
			tbProjectPath.Text = (string)serializer.Deserialize(reader)!;

			if (Application.SystemColorMode == SystemColorMode.Dark)
			{
				tbProjectPath.BackColor = Color.Black;
				tbProjectPath.ForeColor = Color.White;
			}

			panelTrainOnDevice.Enabled = false;
			rBTrainOnDeviceCpu.Checked = true;
			rBTrainOnDeviceCuda.Text = "CUDA not available";

			if (!cuda.is_available())
				return;
			
			panelTrainOnDevice.Enabled = true;
			rBTrainOnDeviceCuda.Checked = true;
			rBTrainOnDeviceCuda.Text = "CUDA";
		}

		private void tbProjectPath_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.KeyData != Keys.Enter)
					return;

				if (!tbProjectPath.Text.EndsWith('\\'))
				{
					tbProjectPath.Text += "\\";
				}

				tbProjectPath.BackColor = Color.Orange;
				Application.DoEvents();
				Thread.Sleep(500);

				var serializer = new Newtonsoft.Json.JsonSerializer();
				serializer.Converters.Add(new JavaScriptDateTimeConverter());
				serializer.NullValueHandling = NullValueHandling.Ignore;
				using var sw = new StreamWriter(@"ProjectPath.txt");
				using var writer = new JsonTextWriter(sw);
				serializer.Serialize(writer, tbProjectPath.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			finally
			{
				if (Application.SystemColorMode == SystemColorMode.Dark)
				{
					tbProjectPath.BackColor = Color.Black;
					tbProjectPath.ForeColor = Color.White;
				}
				else
				{
					tbProjectPath.BackColor = Color.White;
					tbProjectPath.ForeColor = Color.Black;
				}
			}
		}

		private void btnExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		public static (UNetModelParameter, UNetData) LoadSettings(string projDir)
		{
			var modelPara = new UNetModelParameter();
			var dataPara = new UNetData();

			using (var modelParaReader = new StreamReader(Path.Combine(projDir, dataPara.ModelFolder, modelPara.UNeModelParameterFile)))
			using (var dataParaReader = new StreamReader(Path.Combine(projDir, dataPara.ModelFolder, dataPara.UNetDataParameterFile)))
			{
				modelPara = new XmlSerializer(typeof(UNetModelParameter)).Deserialize(modelParaReader) as UNetModelParameter;
				dataPara = new XmlSerializer(typeof(UNetData)).Deserialize(dataParaReader) as UNetData;
			}
			return (modelPara!, dataPara!);
		}
	}
}
