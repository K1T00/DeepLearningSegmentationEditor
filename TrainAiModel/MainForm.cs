using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScottPlot.Plottables;
using AiModels;
using AiModels.UNet;
using AiModels.SimpleModel;
using ScottPlot;
using TorchSharp;
using static TorchSharp.torch;
using Utils;
using static Utils.HelperFunctions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Xml.Serialization;
using NativeOps;
// Hello world

namespace TrainAiModel
{
	public partial class MainForm : Form
	{
		
		private DataLogger? plotTrainLoss;
		private DataLogger? plotValLoss;
		private CancellationTokenSource? tokenSource;

		public MainForm()
		{
			InitializeComponent();
			InitializeGui();
		}

		private async void btnSimpleModel_Click(object sender, EventArgs e)
		{
			btnSimpleModel.Enabled = false;

			plotTrainLoss!.Data.Clear();
			plotValLoss!.Data.Clear();

			var trainOnDevice = DeviceType.CPU;
			if (rBTrainOnDeviceCuda.Checked) trainOnDevice = DeviceType.CUDA;

			var mySimpleModel = new SimpleModelRun();

			// Epoch, TrainLoss, ValidationLoss
			var trainLossProgressReport = new Progress<Tuple<int, float, float>>();
			var logProgressReport = new Progress<string>();
			tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			logProgressReport.ProgressChanged += ProcessLogData;
			trainLossProgressReport.ProgressChanged += ProcessLossData;

			await Task.Run(() =>
			{
				try
				{
					mySimpleModel.Run(trainOnDevice, logProgressReport, trainLossProgressReport, token);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.ToString());
				}
			}).ConfigureAwait(true);

			if (trainOnDevice == DeviceType.CUDA)
			{
				LibTorchLoader.EnsureLoaded();

				if (LibTorchLoader.Loaded & cuda_is_available())
				{
					Ops.cuda_empty_cache();
				}
			}


			btnSimpleModel.Enabled = true;
		}

		private async void btnUNet_Click(object sender, EventArgs e)
		{
			btnUNet.Enabled = false;

			if (CheckFolderStructure(tbProjectPath.Text))
			{
				plotTrainLoss!.Data.Clear();
				plotValLoss!.Data.Clear();

				var uNetPara = InitializeUNetParameter();
				var cfgParameter = InitializeConfigParameter(tbProjectPath.Text, Phase.Train, uNetPara.Features > 1);
				var myUNetModel = new UNetRun();

				await Task.Run(() => SaveSettings(uNetPara, cfgParameter)).ConfigureAwait(true);

				// Epoch, TrainLoss, ValidationLoss
				var trainLossProgressReport = new Progress<Tuple<int, float, float>>();
				var logProgressReport = new Progress<string>();
				tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				logProgressReport.ProgressChanged += ProcessLogData;
				trainLossProgressReport.ProgressChanged += ProcessLossData;

				await Task.Run(() =>
				{
					try
					{
						myUNetModel.Run(uNetPara, cfgParameter, logProgressReport, trainLossProgressReport, token);
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.ToString());
					}
				}).ConfigureAwait(true);

				if (uNetPara.TrainOnDevice == DeviceType.CUDA)
				{
					LibTorchLoader.EnsureLoaded();

					if (LibTorchLoader.Loaded & cuda_is_available())
					{
						Ops.cuda_empty_cache();
					}
				}
			}
			btnUNet.Enabled = true;
		}

		private async void btnRunModelOnTestData_Click(object sender, EventArgs e)
		{
			btnRunModelOnTestData.Enabled = false;

			if (CheckFolderStructure(tbProjectPath.Text))
			{
				var cfgParameter = InitializeConfigParameter(tbProjectPath.Text, Phase.Test, false);
				var uNetPara = InitializeUNetParameter();
				var runModelFile = new ModelRun();

				await Task.Run(() =>
				{
					try
					{
						EmptyFolder(new DirectoryInfo(cfgParameter.HeatmapsTestPath));
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.ToString());
					}
				}).ConfigureAwait(true);

				// Epoch, TrainLoss, ValidationLoss
				var logProgressReport = new Progress<string>();
				var progressBarReport = new Progress<int>();
				tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				logProgressReport.ProgressChanged += ProcessLogData;
				progressBarReport.ProgressChanged += ProgressBarUpdate;

				await Task.Run(() =>
					runModelFile.Run(new UNetModelBn(uNetPara), cfgParameter, uNetPara, logProgressReport, progressBarReport, token)
					).ConfigureAwait(true);


				if (uNetPara.TrainOnDevice == DeviceType.CUDA)
				{
					LibTorchLoader.EnsureLoaded();

					if (LibTorchLoader.Loaded & cuda_is_available())
					{
						Ops.cuda_empty_cache();
					}
				}

				progressBar1.Value = 100;
			}
			btnRunModelOnTestData.Enabled = true;
		}

		private async void btnResizeImages_Click(object sender, EventArgs e)
		{
			btnResizeImages.Enabled = false;

			if (CheckFolderStructure(tbProjectPath.Text))
			{
				var cfgParameter = InitializeConfigParameter(tbProjectPath.Text, Phase.ResizeTrain, Convert.ToInt16(cbFeatures.SelectedItem) > 1);

				var tasks = new Task[]
				{
					Task.Run(() => EmptyFolder(new DirectoryInfo(cfgParameter.TrainGrabsPrePath))),
					Task.Run(() => EmptyFolder(new DirectoryInfo(cfgParameter.TrainMasksPrePath)))
				};
				try
				{
					await Task.WhenAll(tasks);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.ToString());
				}

				txtLog.AppendText("\r\n" + "Resizing train images ..." + "\r\n");

				var imagesCreated = 0;

				await Task.Run(() =>
				{
					try
					{
						imagesCreated = SplitImages(
							cfgParameter.DatasetImages,
							cfgParameter.DatasetMasks,
							cfgParameter.TrainGrabsPrePath,
							cfgParameter.TrainMasksPrePath,
							cfgParameter.Roi,
							cfgParameter.DownSampling,
							ckFilterByBlobs.Checked,
							ckbConvertToGreyscale.Checked,
							ckbWithBoarderPadding.Checked);
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.ToString());
					}
				}).ConfigureAwait(true);

				MessageBox.Show((imagesCreated - 1) + " images and masks have been created.");
			}
			btnResizeImages.Enabled = true;
		}

		private void btnStopTraining_Click(object sender, EventArgs e)
		{
			if (tokenSource == null)
			{
				return;
			}
			txtLog.AppendText("\r\n" + "Stopping training ..." + "\r\n");
			tokenSource!.Cancel();
		}

		private void btnStopRunModel_Click(object sender, EventArgs e)
		{
			if (tokenSource == null)
			{
				return;
			}
			txtLog.AppendText("\r\n" + "Stopping run ..." + "\r\n");
			tokenSource!.Cancel();
		}

		private void InitializeGui()
		{
			cBRoiSize.Items.Add(48);
			cBRoiSize.Items.Add(96);
			cBRoiSize.Items.Add(144);
			cBRoiSize.Items.Add(192);
			cBRoiSize.Items.Add(240);
			for (var i = 1; i < 7; i++)
			{
				cbFeatures.Items.Add(i);
			}
			cBDownSampling.Items.Add(0);
			cBDownSampling.Items.Add(1);
			cBDownSampling.Items.Add(2);
			cBDownSampling.Items.Add(3);
			cBRoiSize.SelectedIndex = 3;
			cbFeatures.SelectedIndex = 0;
			cBDownSampling.SelectedIndex = 0;

			if (!cuda.is_available())
			{
				panelTrainOnDevice.Enabled = false;
				rBTrainOnDeviceCpu.Checked = true;
				rBTrainOnDeviceCuda.Text = "CUDA not available";
			}

			const string projPath = @"ProjectPath.txt";

			if (!File.Exists(projPath))
			{
				var fileCreated = File.CreateText(projPath);
				fileCreated.Close();
			}

			// Load saved data
			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using var sr = new StreamReader(projPath);
			using var reader = new JsonTextReader(sr);
			tbProjectPath.Text = (string)serializer.Deserialize(reader)!;

			if (File.Exists(tbProjectPath.Text + "models\\ModelParameter.xml"))
			{
				var uNetParameterSerializer = new XmlSerializer(typeof(UNetParameter));
				using var uNetReader = new StreamReader(tbProjectPath.Text + "models\\ModelParameter.xml");
				var uNetPara = (UNetParameter)uNetParameterSerializer.Deserialize(uNetReader)!;

				txtEpochs.Text = uNetPara.MaxEpochs.ToString();
				txtBatchSize.Text = uNetPara.BatchSize.ToString();
				txtLearningRate.Text = uNetPara.LearningRate.ToString(CultureInfo.CurrentCulture);
				txtStopAtLoss.Text = uNetPara.StopAtLoss.ToString(CultureInfo.CurrentCulture);
				txbSplitTrainValidationSet.Text = Convert.ToString(uNetPara.SplitTrainValidationSet, CultureInfo.CurrentCulture);

				rbTrainImagesAsRgb.Checked = true;
				rbTrainImagesAsGreyscale.Checked = uNetPara.TrainImagesAsGreyscale;

				rBTrainAsFloat16.Checked = true;
				rBTrainAsFloat32.Checked = uNetPara.TrainPrecision == ScalarType.Float32;

				rBTrainOnDeviceCpu.Checked = true;
				rBTrainOnDeviceCuda.Checked = uNetPara.TrainOnDevice == DeviceType.CUDA;
			}
			if (File.Exists(tbProjectPath.Text + "models\\DbParameter.xml"))
			{
				var dbConfigSerializer = new XmlSerializer(typeof(Db));
				using var cfgReader = new StreamReader(tbProjectPath.Text + "models\\DbParameter.xml");
				var cfgParameter = (Db)dbConfigSerializer.Deserialize(cfgReader)!;
				tbThreasholdHeatmaps.Text = cfgParameter.ThresholdHeatmaps.ToString();
				cBRoiSize.SelectedIndex = cBRoiSize.Items.IndexOf(cfgParameter.Roi);
				cBDownSampling.SelectedIndex = cBDownSampling.Items.IndexOf(cfgParameter.DownSampling);
				ckbWithBoarderPadding.Checked = cfgParameter.WithBoarderPadding;
			}

			// Plot
			fmpLoss.Interaction.Disable();
			fmpLoss.Plot.XLabel("Epochs");
			fmpLoss.Plot.YLabel("Loss");
			//fmpLoss.Plot.Axes.AutoScale();
			fmpLoss.Plot.ScaleFactor = 1.5;
			fmpLoss.Plot.Axes.Left.Max = 1;
			fmpLoss.Plot.Axes.Left.Min = 0;
			fmpLoss.Plot.Axes.Bottom.Min = 0;
			fmpLoss.Plot.Legend.Alignment = Alignment.UpperRight;

			plotTrainLoss = fmpLoss.Plot.Add.DataLogger();
			plotValLoss = fmpLoss.Plot.Add.DataLogger();

			plotTrainLoss.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Blue);
			plotValLoss.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Brown);

			plotTrainLoss.LegendText = "Train";
			plotValLoss.LegendText = "Validation";

			plotTrainLoss.LineWidth = 2;
			plotValLoss.LineWidth = 2;
		}

		private void ProcessLossData(object? sender, Tuple<int, float, float> e)
		{
			this.fmpLoss.BeginInvoke(new Action(() =>
			{
				plotTrainLoss?.Add(e.Item1, e.Item2 > 1 ? 1 : e.Item2);

				if (float.Parse(txbSplitTrainValidationSet.Text, CultureInfo.CurrentCulture) < 0.81f)
				{
					plotValLoss?.Add(e.Item1, e.Item3 > 1 ? 1 : e.Item3);
				}

				fmpLoss.Refresh();
			}));
		}

		private void ProcessLogData(object? sender, string e)
		{
			this.txtLog.BeginInvoke(new Action(() =>
			{
				var logText = "\r\n" + e + "\r\n";
				txtLog.AppendText(logText);
				File.AppendAllText(tbProjectPath.Text + "Log.txt", logText);
			}));
		}

		private void ProgressBarUpdate(object? sender, int e)
		{
			this.progressBar1.BeginInvoke(new Action(() => { progressBar1.Value = e; }));
		}

		private Db InitializeConfigParameter(string projectPath, Phase phase, bool moreThanOneFeature)
		{
			return new Db(projectPath, phase, moreThanOneFeature)
			{
				Roi = Convert.ToInt16(cBRoiSize.Text),
				DownSampling = Convert.ToInt16(cBDownSampling.Text),
				ProjectPath = tbProjectPath.Text,
				WithBoarderPadding = ckbWithBoarderPadding.Checked,
				ThresholdHeatmaps = Convert.ToInt16(tbThreasholdHeatmaps.Text)
			};
		}

		private UNetParameter InitializeUNetParameter()
		{
			var usePrecision = ScalarType.BFloat16;
			if (rBTrainAsFloat32.Checked) usePrecision = float32;

			var trainOnDevice = DeviceType.CPU;
			if (rBTrainOnDeviceCuda.Checked) trainOnDevice = DeviceType.CUDA;

			return new UNetParameter
			{
				Features = Convert.ToInt16(cbFeatures.SelectedItem),
				MaxEpochs = Convert.ToInt16(txtEpochs.Text),
				BatchSize = Convert.ToInt16(txtBatchSize.Text),
				LearningRate = Convert.ToDouble(txtLearningRate.Text),
				StopAtLoss = float.Parse(txtStopAtLoss.Text, CultureInfo.CurrentCulture),
				TrainImagesAsGreyscale = rbTrainImagesAsGreyscale.Checked,
				SplitTrainValidationSet = float.Parse(txbSplitTrainValidationSet.Text, CultureInfo.CurrentCulture),
				TrainPrecision = usePrecision,
				TrainOnDevice = trainOnDevice,
				FirstFilterSize = 64
			};
		}

		private void tbProjectPath_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData != Keys.Enter)
				return;

			if (!tbProjectPath.Text.EndsWith('\\'))
			{
				tbProjectPath.Text += "\\";
			}

			tbProjectPath.BackColor = System.Drawing.Color.Orange;
			Application.DoEvents();
			Thread.Sleep(500);

			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using var sw = new StreamWriter(@"ProjectPath.txt");
			using var writer = new JsonTextWriter(sw);
			serializer.Serialize(writer, tbProjectPath.Text);

			tbProjectPath.BackColor = System.Drawing.Color.White;
		}

		public void SaveSettings(UNetParameter uNetPara, Db cfgPara)
		{
			var uNetParameterSerializer = new XmlSerializer(typeof(UNetParameter));
			var dbConfigSerializer = new XmlSerializer(typeof(Db));
			using var uNetWriter = new StreamWriter(cfgPara.ModelPath + "ModelParameter.xml");
			using var cfgWriter = new StreamWriter(cfgPara.ModelPath + "DbParameter.xml");
			uNetParameterSerializer.Serialize(uNetWriter, uNetPara);
			dbConfigSerializer.Serialize(cfgWriter, cfgPara);
		}

		private static bool CheckFolderStructure(string projectPath)
		{
			var folderModels = projectPath + "models";

			var folderTestGrabs = projectPath + "test\\grabs";
			var folderTestHeatmaps = projectPath + "test\\heatmaps";

			var folderTrainGrabs = projectPath + "train\\grabs";
			var folderTrainGrabsPre = projectPath + "train\\grabsPre";
			var folderTrainMasks = projectPath + "train\\masks";
			var folderTrainMasksPre = projectPath + "train\\masksPre";

			if (!Directory.Exists(folderModels) | !Directory.Exists(folderTestGrabs) | !Directory.Exists(folderTestHeatmaps) | !Directory.Exists(folderTrainGrabs) |
				!Directory.Exists(folderTrainGrabsPre) | !Directory.Exists(folderTrainMasks) | !Directory.Exists(folderTrainMasksPre))
			{
				MessageBox.Show("Folder structure not correct.");
				return false;
			}
			return true;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void toolStripMenuItem2_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Version: 0.065");
		}

		private void loadAndTestsLibtorchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LibTorchLoader.EnsureLoaded();

			if (LibTorchLoader.Loaded & cuda_is_available())
			{
				Ops.cuda_empty_cache();
			}
		}
	}
}
