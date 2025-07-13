using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.AxisPanels;
using ILGPU;
using ILGPU.Runtime;
using TorchSharp;
using static TorchSharp.torch;
using AiOps.AiModels.UNet;
using AiOps.AiModels.SimpleModel;
using AiOps.AiUtils;
using static AiOps.AiUtils.HelperFunctions;
using static AiOps.AiUtils.TensorImageCalcHelper;
using static AiOps.CudaOps.NativeOps;


namespace TrainAiModel
{
	public partial class MainForm : Form
	{

		private DataLogger? trainLossLogger;
		private DataLogger? validationLossLogger;
		private CancellationTokenSource? tokenSource;

		public MainForm()
		{
			InitializeComponent();
			InitializeUi();
			InitializeDataPlots();
		}

		#region Train/Run model buttons

		private async void btnTrainUNetModel_Click(object sender, EventArgs e)
		{
			btnTrainUNetModel.Enabled = false;
			try
			{
				ClearPlots();

				var (modelParameter, dataParameter) = GetSettingsFromUi(Phase.TrainModel);

				SaveSettings(modelParameter, dataParameter);

				var trainLossProgressReport = new Progress<LossReport>();
				var logProgressReport = new Progress<string>();
				logProgressReport.ProgressChanged += ProcessLogData;
				trainLossProgressReport.ProgressChanged += ProcessLossData;

				tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;

				await Task.Run(() =>
				{
					try
					{
						var myUNetModel = new UNetTrain();

						myUNetModel.Run(modelParameter, dataParameter, logProgressReport, trainLossProgressReport, token);
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.ToString());
					}
				}).ConfigureAwait(true);

				if (modelParameter.TrainOnDevice == DeviceType.CUDA)
				{
					EmptyCudaCache();
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
			}
			finally
			{
				btnTrainUNetModel.Enabled = true;
			}
		}

		private async void btnRunModelOnTestData_Click(object sender, EventArgs e)
		{
			btnRunModelOnTestData.Enabled = false;

			try
			{
				//
				var (modelPara, dataPara) = LoadSettings(tbProjectPath.Text);
				UpdateUiWithLoadedSettings(modelPara, dataPara);
				//

				var (modelParameter, dataParameter) = GetSettingsFromUi(Phase.TestModel);

				EmptyFolder(new DirectoryInfo(dataParameter.TestHeatmapsPath));

				var logProgressReport = new Progress<string>();
				var progressBarReport = new Progress<int>();
				logProgressReport.ProgressChanged += ProcessLogData;
				progressBarReport.ProgressChanged += ProgressBarUpdate;

				tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;

				var runOnDevice = new torch.Device(rBTrainOnDeviceCuda.Checked ? DeviceType.CUDA : DeviceType.CPU);
				modelParameter.TrainOnDevice = runOnDevice.type;

				using (var model = new UNetModel(modelParameter))
				{
					var runModelOperator = new UNetOperator();

					model
						.load(Path.Combine(dataParameter.ModelPath, dataParameter.ModelWeightsFile))
						.to(rBTrainAsFloat32.Checked ? ScalarType.Float32 : ScalarType.BFloat16, true);

					model.eval();

					await Task.Run(() =>
						runModelOperator.Execute(
							model,
							runOnDevice,
							dataParameter,
							modelParameter,
							Convert.ToInt16(tbThreasholdHeatmaps.Text),
							logProgressReport,
							progressBarReport,
							token)
					).ConfigureAwait(true);
				}

				if (runOnDevice.type == DeviceType.CUDA)
				{
					EmptyCudaCache();
				}
				progressBar1.Value = 100;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
			}
			finally
			{
				btnRunModelOnTestData.Enabled = true;
			}
		}

		private void btnStopTraining_Click(object sender, EventArgs e)
		{
			if (tokenSource == null)
				return;

			txtLog.AppendText(Environment.NewLine + "Stopping training ..." + Environment.NewLine);
			tokenSource!.Cancel();
		}

		private void btnStopRunModel_Click(object sender, EventArgs e)
		{
			if (tokenSource == null)
				return;

			txtLog.AppendText(Environment.NewLine + "Stopping run ..." + Environment.NewLine);
			tokenSource!.Cancel();
		}

		#endregion

		#region Data preperation buttons

		private async void btnPrepareTrainData_Click(object sender, EventArgs e)
		{
			btnPrepareTrainData.Enabled = false;

			try
			{
				var (modelParameter, dataParameter) =
					GetSettingsFromUi(rbFeatureDetection.Checked ? Phase.PrepareFeatureDetection : Phase.PrepareLocatePoints);

				await Task.Run(() => EmptyFolder(new DirectoryInfo(dataParameter.TrainGrabsPrePath)));
				await Task.Run(() => EmptyFolder(new DirectoryInfo(dataParameter.TrainMasksPrePath)));

				var masksCreated = 0;

				// Create masks from locations
				if (rbPointLocation.Checked)
				{
					txtLog.AppendText(Environment.NewLine + "Generating masks from locations ..." + Environment.NewLine);

					await Task.Run(() => EmptyFolder(new DirectoryInfo(dataParameter.TrainMasksPath)));

					await Task.Run(() =>
					{
						masksCreated = GenerateMasks(dataParameter, Convert.ToInt16(tbSideLength.Text));

					}).ConfigureAwait(true);

					txtLog.AppendText(masksCreated + " masks created." + Environment.NewLine);

					 (modelParameter, dataParameter) = GetSettingsFromUi(Phase.PrepareFeatureDetection);
				}

				txtLog.AppendText("\r\n" + "Resizing train images ..." + "\r\n");

				var imagesCreated = 0;

				await Task.Run(() =>
				{
					imagesCreated = SliceTrainImages(
						dataParameter,
						ckFilterByBlobs.Checked,
						ckbConvertToGreyscale.Checked,
						ckbWithBorderPadding.Checked);
					
				}).ConfigureAwait(true);

				cBbatchDivisors.Items.Clear();
				foreach (var divisor in GetDivisors(imagesCreated - 1).ToList())
				{
					cBbatchDivisors.Items.Add(divisor);
				}
				cBbatchDivisors.SelectedIndex = 0;

				MessageBox.Show((imagesCreated - 1) + " images and masks have been created.");
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
			}
			finally
			{
				btnPrepareTrainData.Enabled = true;
			}
		}

		#endregion

		#region Menu strip

		private async void trainSimpleModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			trainSimpleModelToolStripMenuItem.Enabled = false;

			try
			{
				ClearPlots();

				var trainOnDevice = DeviceType.CPU;
				if (rBTrainOnDeviceCuda.Checked) trainOnDevice = DeviceType.CUDA;

				var trainLossProgressReport = new Progress<LossReport>();
				var logProgressReport = new Progress<string>();
				logProgressReport.ProgressChanged += ProcessLogData;
				trainLossProgressReport.ProgressChanged += ProcessLossData;

				tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;

				await Task.Run(() =>
				{
					try
					{
						var mySimpleModel = new SimpleModelTrain();
						mySimpleModel.Run(trainOnDevice, logProgressReport, trainLossProgressReport, token);
					}
					catch (Exception exception)
					{
						MessageBox.Show(exception.ToString());
					}
				}).ConfigureAwait(true);

				if (trainOnDevice == DeviceType.CUDA)
				{
					EmptyCudaCache();
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			finally
			{
				trainSimpleModelToolStripMenuItem.Enabled = true;
			}
		}

		private void testLibTorchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			testLibTorchToolStripMenuItem.Enabled = false;

			try
			{
				if (cuda.is_available())
				{
					EmptyCudaCache();
					MessageBox.Show("CUDA empty cache OK");
				}
				else
				{
					MessageBox.Show("CUDA not available");
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			finally
			{
				testLibTorchToolStripMenuItem.Enabled = true;
			}
		}

		private void loadSettingsToUiToolStripMenuItem_Click(object sender, EventArgs e)
		{
			loadSettingsToUiToolStripMenuItem.Enabled = false;

			try
			{
				var (modelPara, dataPara) = LoadSettings(tbProjectPath.Text);

				UpdateUiWithLoadedSettings(modelPara, dataPara);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
			}
			finally
			{
				loadSettingsToUiToolStripMenuItem.Enabled = true;
			}
		}

		private void versionToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			MessageBox.Show("Build: " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location));
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exitToolStripMenuItem.Enabled = false;
			Application.Exit();
		}

		#endregion

		private void InitializeUi()
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
			for (var i = 0; i < 6; i++)
			{
				cBDownSampling.Items.Add(i);
			}
			for (var i = 1; i < 1000; i++)
			{
				cBbatchDivisors.Items.Add(i);
			}
			cBRoiSize.SelectedIndex = 3;
			cbFeatures.SelectedIndex = 0;
			cBDownSampling.SelectedIndex = 0;

			panelTrainOnDevice.Enabled = false;
			rBTrainOnDeviceCpu.Checked = true;
			rBTrainOnDeviceCuda.Text = "CUDA not available";

			lbSystemRam.Text = 
				$"System memory = RAM: " +
				$"{Math.Round(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0), 0)}  " +
				$"GB; no VRAM";

			if (cuda.is_available())
			{
				panelTrainOnDevice.Enabled = true;
				rBTrainOnDeviceCuda.Checked = true;
				rBTrainOnDeviceCuda.Text = "CUDA";

				var myContext = Context.CreateDefault();
				foreach (var device in myContext)
				{
					var myAccelerator = device.CreateAccelerator(myContext);

					if (myAccelerator.AcceleratorType == AcceleratorType.Cuda)
					{
						lbSystemRam.Text = 
							$"System memory = RAM: " +
							$"{Math.Round(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0), 0)} " +
							$"GB; VRAM: {Math.Round(myAccelerator.MemorySize / (1024.0 * 1024.0 * 1024.0), 0)} GB";
					}
				}
			}

			LoadLastProjectPath();
		}

		/// <summary>
		/// Plots the loss data in the forms plot control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ProcessLossData(object? sender, LossReport e)
		{
			this.formsPlot1.BeginInvoke(() =>
			{
				trainLossLogger?.Add(e.Epoch, e.TrainLoss > 2 ? 2 : e.TrainLoss);
				validationLossLogger?.Add(e.Epoch, e.ValidationLoss > 2 ? 2 : e.ValidationLoss);

				trainLossLogger?.ViewFull();
				validationLossLogger?.ViewFull();
				
				formsPlot1.Refresh();
			});
		}

		private void ProcessLogData(object? sender, string e)
		{
			this.txtLog.BeginInvoke(() =>
			{
				var logText = Environment.NewLine + e + Environment.NewLine;
				txtLog.AppendText(logText);
				if (!ckbSaveLog.Checked) return;
				File.AppendAllText(Path.Combine(tbProjectPath.Text, "Log.txt"), logText);
			});
		}

		private void ProgressBarUpdate(object? sender, int e)
		{
			this.progressBar1.BeginInvoke(() => { progressBar1.Value = e; });
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

			if (Application.SystemColorMode == SystemColorMode.Dark)
			{
				tbProjectPath.BackColor = System.Drawing.Color.Black;
				tbProjectPath.ForeColor = System.Drawing.Color.White;
			}
			else
			{
				tbProjectPath.BackColor = System.Drawing.Color.White;
				tbProjectPath.ForeColor = System.Drawing.Color.Black;
			}
		}

		private void UpdateUiWithLoadedSettings(IHyperParameter modelPara, UNetData dataPara)
		{
			txtEpochs.Text = modelPara.MaxEpochs.ToString();
			cBbatchDivisors.SelectedIndex = cBbatchDivisors.Items.IndexOf(modelPara.BatchSize);
			txtLearningRate.Text = modelPara.LearningRate.ToString(CultureInfo.InvariantCulture);
			txtStopAtLoss.Text = modelPara.StopAtLoss.ToString(CultureInfo.InvariantCulture);
			txbSplitTrainValidationSet.Text = Convert.ToString(modelPara.SplitTrainValidationSet, CultureInfo.InvariantCulture);
			rbTrainImagesAsRgb.Checked = true;
			rbTrainImagesAsGreyscale.Checked = modelPara.TrainImagesAsGreyscale;
			rBTrainAsFloat16.Checked = true;
			rBTrainAsFloat32.Checked = modelPara.TrainPrecision == ScalarType.Float32;
			rBTrainOnDeviceCpu.Checked = true;
			rBTrainOnDeviceCuda.Checked = modelPara.TrainOnDevice == DeviceType.CUDA;
			cBRoiSize.SelectedIndex = cBRoiSize.Items.IndexOf(dataPara.SliceRoi);
			cBDownSampling.SelectedIndex = cBDownSampling.Items.IndexOf(dataPara.DownSampling);
			ckbWithBorderPadding.Checked = dataPara.WithBorderPadding;
		}

		private void InitializeDataPlots()
		{
			trainLossLogger = formsPlot1.Plot.Add.DataLogger();
			validationLossLogger = formsPlot1.Plot.Add.DataLogger();

			var axisL = (LeftAxis)formsPlot1.Plot.Axes.Left;
			var axisB = (BottomAxis)formsPlot1.Plot.Axes.Bottom;

			trainLossLogger.Axes.YAxis = axisL;
			validationLossLogger.Axes.YAxis = axisL;

			trainLossLogger.Axes.XAxis = axisB;
			validationLossLogger.Axes.XAxis = axisB;

			if (Application.SystemColorMode == SystemColorMode.Dark)
			{
				formsPlot1.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Empty);
				formsPlot1.Plot.Axes.Color(ScottPlot.Color.FromColor(System.Drawing.Color.White));
			}

			formsPlot1.Plot.XLabel("Epochs");
			formsPlot1.Plot.YLabel("Loss");

			trainLossLogger.LegendText = "Train";
			validationLossLogger.LegendText = "Validation";

			formsPlot1.Plot.Legend.Alignment = Alignment.UpperRight;

			trainLossLogger.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Blue);
			validationLossLogger.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Brown);

			trainLossLogger.LineWidth = 2;
			validationLossLogger.LineWidth = 2;

			formsPlot1.Plot.Axes.AutoScale();
		}

		private void ClearPlots()
		{
			trainLossLogger!.Data.Clear();
			validationLossLogger!.Data.Clear();
		}

		public static void SaveSettings(UNetModelParameter modelPara, UNetData dataPara)
		{
			using (var modelParaWriter = new StreamWriter(Path.Combine(dataPara.ModelPath, modelPara.UNeModelParameterFile)))
			using (var dataParaWriter = new StreamWriter(Path.Combine(dataPara.ModelPath, dataPara.UNetDataParameterFile)))
			{
				var modelParameterSerializer = new XmlSerializer(typeof(UNetModelParameter));
				var dataParameterSerializer = new XmlSerializer(typeof(UNetData));

				modelParameterSerializer.Serialize(modelParaWriter, modelPara);
				dataParameterSerializer.Serialize(dataParaWriter, dataPara);
			}
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

		private (UNetModelParameter, UNetData) GetSettingsFromUi(Phase phase)
		{
			return (
				new UNetModelParameter()
				{
					Features = Convert.ToInt16(cbFeatures.SelectedItem),
					MaxEpochs = Convert.ToInt16(txtEpochs.Text),
					BatchSize = Convert.ToInt16(cBbatchDivisors.SelectedItem),
					LearningRate = Convert.ToDouble(txtLearningRate.Text, CultureInfo.InvariantCulture),
					StopAtLoss = float.Parse(txtStopAtLoss.Text, CultureInfo.InvariantCulture),
					TrainImagesAsGreyscale = rbTrainImagesAsGreyscale.Checked,
					SplitTrainValidationSet =
						float.Parse(txbSplitTrainValidationSet.Text, CultureInfo.InvariantCulture),
					TrainPrecision = rBTrainAsFloat32.Checked ? ScalarType.Float32 : ScalarType.BFloat16,
					TrainOnDevice = rBTrainOnDeviceCuda.Checked ? DeviceType.CUDA : DeviceType.CPU,
					FirstFilterSize = 64
				},
				new UNetData(tbProjectPath.Text, phase, Convert.ToInt16(cbFeatures.SelectedItem) > 1)
				{
					SliceRoi = Convert.ToInt16(cBRoiSize.Text),
					DownSampling = Convert.ToInt16(cBDownSampling.Text),
					ProjectDir = tbProjectPath.Text,
					WithBorderPadding = ckbWithBorderPadding.Checked,
				});
		}

		private void LoadLastProjectPath()
		{
			const string projectPath = @"ProjectPath.txt";
			if (!File.Exists(projectPath))
			{
				var fileCreated = File.CreateText(projectPath);
				fileCreated.Close();
			}

			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using var sr = new StreamReader(projectPath);
			using var reader = new JsonTextReader(sr);
			tbProjectPath.Text = (string)serializer.Deserialize(reader)!;
		}

		private void resetBatchListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			for (var i = 1; i < 1000; i++)
			{
				cBbatchDivisors.Items.Add(i);
			}
		}

	}
}
