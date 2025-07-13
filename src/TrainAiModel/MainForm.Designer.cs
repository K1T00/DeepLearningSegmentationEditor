namespace TrainAiModel
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			label1 = new Label();
			label2 = new Label();
			label3 = new Label();
			txtEpochs = new TextBox();
			txtLearningRate = new TextBox();
			txtLog = new TextBox();
			label4 = new Label();
			txtStopAtLoss = new TextBox();
			label5 = new Label();
			label6 = new Label();
			label7 = new Label();
			btnRunModelOnTestData = new Button();
			ckFilterByBlobs = new CheckBox();
			label8 = new Label();
			label10 = new Label();
			label11 = new Label();
			txtAugGaussianBlur = new TextBox();
			label12 = new Label();
			txtAugNoise = new TextBox();
			label13 = new Label();
			txtAugContrast = new TextBox();
			txtAugBrightness = new TextBox();
			txtAugLuminance = new TextBox();
			label14 = new Label();
			label15 = new Label();
			label16 = new Label();
			txtAugFlip = new TextBox();
			label17 = new Label();
			txtAugRotation = new TextBox();
			label18 = new Label();
			txtAugScale = new TextBox();
			label20 = new Label();
			txtAugShear = new TextBox();
			label19 = new Label();
			ckbConvertToGreyscale = new CheckBox();
			rbTrainImagesAsGreyscale = new RadioButton();
			rbTrainImagesAsRgb = new RadioButton();
			panelRadioTrainImagesAs = new Panel();
			ckbWithBorderPadding = new CheckBox();
			txbSplitTrainValidationSet = new TextBox();
			label21 = new Label();
			progressBar1 = new ProgressBar();
			tbProjectPath = new TextBox();
			label22 = new Label();
			menuStrip1 = new MenuStrip();
			toolStripMenuItem1 = new ToolStripMenuItem();
			loadSettingsToUiToolStripMenuItem = new ToolStripMenuItem();
			exitToolStripMenuItem = new ToolStripMenuItem();
			testToolStripMenuItem = new ToolStripMenuItem();
			trainSimpleModelToolStripMenuItem = new ToolStripMenuItem();
			testLibTorchToolStripMenuItem = new ToolStripMenuItem();
			resetBatchListToolStripMenuItem = new ToolStripMenuItem();
			questionStripMenuItem = new ToolStripMenuItem();
			versionToolStripMenuItem = new ToolStripMenuItem();
			cBRoiSize = new ComboBox();
			cbFeatures = new ComboBox();
			cBDownSampling = new ComboBox();
			panelTrainPrecision = new Panel();
			rBTrainAsFloat16 = new RadioButton();
			rBTrainAsFloat32 = new RadioButton();
			panelTrainOnDevice = new Panel();
			rBTrainOnDeviceCpu = new RadioButton();
			rBTrainOnDeviceCuda = new RadioButton();
			label9 = new Label();
			tbThreasholdHeatmaps = new TextBox();
			label23 = new Label();
			panel1 = new Panel();
			panel2 = new Panel();
			btnStopTraining = new Button();
			btnStopRunModel = new Button();
			lbSystemRam = new Label();
			btnTrainUNetModel = new Button();
			ckbSaveLog = new CheckBox();
			cBbatchDivisors = new ComboBox();
			formsPlot1 = new ScottPlot.WinForms.FormsPlot();
			btnPrepareTrainData = new Button();
			panel3 = new Panel();
			rbPointLocation = new RadioButton();
			rbFeatureDetection = new RadioButton();
			label24 = new Label();
			tbSideLength = new TextBox();
			panelRadioTrainImagesAs.SuspendLayout();
			menuStrip1.SuspendLayout();
			panelTrainPrecision.SuspendLayout();
			panelTrainOnDevice.SuspendLayout();
			panel3.SuspendLayout();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label1.Location = new Point(507, 89);
			label1.Name = "label1";
			label1.Size = new Size(77, 25);
			label1.TabIndex = 4;
			label1.Text = "Epochs:";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label2.Location = new Point(479, 120);
			label2.Name = "label2";
			label2.Size = new Size(102, 25);
			label2.TabIndex = 5;
			label2.Text = "Batch size:";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label3.Location = new Point(451, 153);
			label3.Name = "label3";
			label3.Size = new Size(130, 25);
			label3.TabIndex = 6;
			label3.Text = "Learning rate:";
			// 
			// txtEpochs
			// 
			txtEpochs.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtEpochs.Location = new Point(590, 86);
			txtEpochs.Name = "txtEpochs";
			txtEpochs.Size = new Size(64, 33);
			txtEpochs.TabIndex = 9;
			txtEpochs.Text = "250";
			// 
			// txtLearningRate
			// 
			txtLearningRate.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtLearningRate.Location = new Point(590, 153);
			txtLearningRate.Name = "txtLearningRate";
			txtLearningRate.Size = new Size(64, 33);
			txtLearningRate.TabIndex = 10;
			txtLearningRate.Text = "0.05";
			// 
			// txtLog
			// 
			txtLog.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtLog.Location = new Point(1043, 70);
			txtLog.Multiline = true;
			txtLog.Name = "txtLog";
			txtLog.ScrollBars = ScrollBars.Vertical;
			txtLog.Size = new Size(281, 668);
			txtLog.TabIndex = 12;
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label4.Location = new Point(491, 192);
			label4.Name = "label4";
			label4.Size = new Size(90, 25);
			label4.TabIndex = 13;
			label4.Text = "Features:";
			// 
			// txtStopAtLoss
			// 
			txtStopAtLoss.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtStopAtLoss.Location = new Point(590, 225);
			txtStopAtLoss.Name = "txtStopAtLoss";
			txtStopAtLoss.Size = new Size(64, 33);
			txtStopAtLoss.TabIndex = 16;
			txtStopAtLoss.Text = "0";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label5.Location = new Point(458, 227);
			label5.Name = "label5";
			label5.Size = new Size(114, 25);
			label5.TabIndex = 15;
			label5.Text = "Stop at loss:";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label6.Location = new Point(1043, 42);
			label6.Name = "label6";
			label6.Size = new Size(51, 25);
			label6.TabIndex = 18;
			label6.Text = "Log:";
			// 
			// label7
			// 
			label7.AutoSize = true;
			label7.Font = new Font("Segoe UI Black", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label7.Location = new Point(396, 36);
			label7.Name = "label7";
			label7.Size = new Size(258, 32);
			label7.TabIndex = 19;
			label7.Text = "Training parameters:";
			// 
			// btnRunModelOnTestData
			// 
			btnRunModelOnTestData.FlatStyle = FlatStyle.System;
			btnRunModelOnTestData.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnRunModelOnTestData.Location = new Point(856, 412);
			btnRunModelOnTestData.Margin = new Padding(3, 2, 3, 2);
			btnRunModelOnTestData.Name = "btnRunModelOnTestData";
			btnRunModelOnTestData.Size = new Size(150, 122);
			btnRunModelOnTestData.TabIndex = 21;
			btnRunModelOnTestData.Text = "Run saved model";
			btnRunModelOnTestData.UseVisualStyleBackColor = true;
			btnRunModelOnTestData.Click += btnRunModelOnTestData_Click;
			// 
			// ckFilterByBlobs
			// 
			ckFilterByBlobs.AutoSize = true;
			ckFilterByBlobs.CheckAlign = ContentAlignment.MiddleRight;
			ckFilterByBlobs.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			ckFilterByBlobs.Location = new Point(93, 164);
			ckFilterByBlobs.Name = "ckFilterByBlobs";
			ckFilterByBlobs.Size = new Size(152, 29);
			ckFilterByBlobs.TabIndex = 23;
			ckFilterByBlobs.Text = "Filter by blobs";
			ckFilterByBlobs.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			label8.AutoSize = true;
			label8.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label8.Location = new Point(12, 89);
			label8.Name = "label8";
			label8.Size = new Size(193, 25);
			label8.TabIndex = 24;
			label8.Text = "ROI in original image";
			// 
			// label10
			// 
			label10.AutoSize = true;
			label10.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label10.Location = new Point(12, 128);
			label10.Name = "label10";
			label10.Size = new Size(197, 25);
			label10.TabIndex = 27;
			label10.Text = "Apply down sampling";
			// 
			// label11
			// 
			label11.AutoSize = true;
			label11.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label11.Location = new Point(789, 42);
			label11.Name = "label11";
			label11.Size = new Size(154, 25);
			label11.TabIndex = 29;
			label11.Text = "Augmentations:";
			label11.Visible = false;
			// 
			// txtAugGaussianBlur
			// 
			txtAugGaussianBlur.Enabled = false;
			txtAugGaussianBlur.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugGaussianBlur.Location = new Point(937, 209);
			txtAugGaussianBlur.Name = "txtAugGaussianBlur";
			txtAugGaussianBlur.Size = new Size(48, 33);
			txtAugGaussianBlur.TabIndex = 39;
			txtAugGaussianBlur.Text = "0";
			txtAugGaussianBlur.Visible = false;
			// 
			// label12
			// 
			label12.AutoSize = true;
			label12.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label12.Location = new Point(785, 214);
			label12.Name = "label12";
			label12.Size = new Size(133, 25);
			label12.TabIndex = 38;
			label12.Text = "Gaussian blur:";
			label12.Visible = false;
			// 
			// txtAugNoise
			// 
			txtAugNoise.Enabled = false;
			txtAugNoise.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugNoise.Location = new Point(937, 173);
			txtAugNoise.Name = "txtAugNoise";
			txtAugNoise.Size = new Size(48, 33);
			txtAugNoise.TabIndex = 37;
			txtAugNoise.Text = "0";
			txtAugNoise.Visible = false;
			// 
			// label13
			// 
			label13.AutoSize = true;
			label13.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label13.Location = new Point(865, 176);
			label13.Name = "label13";
			label13.Size = new Size(66, 25);
			label13.TabIndex = 36;
			label13.Text = "Noise:";
			label13.Visible = false;
			// 
			// txtAugContrast
			// 
			txtAugContrast.Enabled = false;
			txtAugContrast.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugContrast.Location = new Point(939, 106);
			txtAugContrast.Name = "txtAugContrast";
			txtAugContrast.Size = new Size(46, 33);
			txtAugContrast.TabIndex = 35;
			txtAugContrast.Text = "0";
			txtAugContrast.Visible = false;
			// 
			// txtAugBrightness
			// 
			txtAugBrightness.Enabled = false;
			txtAugBrightness.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugBrightness.Location = new Point(939, 138);
			txtAugBrightness.Name = "txtAugBrightness";
			txtAugBrightness.Size = new Size(46, 33);
			txtAugBrightness.TabIndex = 34;
			txtAugBrightness.Text = "0";
			txtAugBrightness.Visible = false;
			// 
			// txtAugLuminance
			// 
			txtAugLuminance.Enabled = false;
			txtAugLuminance.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugLuminance.Location = new Point(939, 71);
			txtAugLuminance.Name = "txtAugLuminance";
			txtAugLuminance.Size = new Size(46, 33);
			txtAugLuminance.TabIndex = 33;
			txtAugLuminance.Text = "0";
			txtAugLuminance.Visible = false;
			// 
			// label14
			// 
			label14.AutoSize = true;
			label14.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label14.Location = new Point(816, 142);
			label14.Name = "label14";
			label14.Size = new Size(106, 25);
			label14.TabIndex = 32;
			label14.Text = "Brightness:";
			label14.Visible = false;
			// 
			// label15
			// 
			label15.AutoSize = true;
			label15.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label15.Location = new Point(834, 107);
			label15.Name = "label15";
			label15.Size = new Size(90, 25);
			label15.TabIndex = 31;
			label15.Text = "Contrast:";
			label15.Visible = false;
			// 
			// label16
			// 
			label16.AutoSize = true;
			label16.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label16.Location = new Point(813, 74);
			label16.Name = "label16";
			label16.Size = new Size(110, 25);
			label16.TabIndex = 30;
			label16.Text = "Luminance:";
			label16.Visible = false;
			// 
			// txtAugFlip
			// 
			txtAugFlip.Enabled = false;
			txtAugFlip.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugFlip.Location = new Point(937, 280);
			txtAugFlip.Name = "txtAugFlip";
			txtAugFlip.Size = new Size(48, 33);
			txtAugFlip.TabIndex = 43;
			txtAugFlip.Text = "0";
			txtAugFlip.Visible = false;
			// 
			// label17
			// 
			label17.AutoSize = true;
			label17.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label17.Location = new Point(881, 281);
			label17.Name = "label17";
			label17.Size = new Size(48, 25);
			label17.TabIndex = 42;
			label17.Text = "Flip:";
			label17.Visible = false;
			// 
			// txtAugRotation
			// 
			txtAugRotation.Enabled = false;
			txtAugRotation.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugRotation.Location = new Point(937, 244);
			txtAugRotation.Name = "txtAugRotation";
			txtAugRotation.Size = new Size(48, 33);
			txtAugRotation.TabIndex = 41;
			txtAugRotation.Text = "0";
			txtAugRotation.Visible = false;
			// 
			// label18
			// 
			label18.AutoSize = true;
			label18.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label18.Location = new Point(833, 246);
			label18.Name = "label18";
			label18.Size = new Size(90, 25);
			label18.TabIndex = 40;
			label18.Text = "Rotation:";
			label18.Visible = false;
			// 
			// txtAugScale
			// 
			txtAugScale.Enabled = false;
			txtAugScale.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugScale.Location = new Point(937, 316);
			txtAugScale.Name = "txtAugScale";
			txtAugScale.Size = new Size(48, 33);
			txtAugScale.TabIndex = 47;
			txtAugScale.Text = "0";
			txtAugScale.Visible = false;
			// 
			// label20
			// 
			label20.AutoSize = true;
			label20.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label20.Location = new Point(867, 317);
			label20.Name = "label20";
			label20.Size = new Size(61, 25);
			label20.TabIndex = 46;
			label20.Text = "Scale:";
			label20.Visible = false;
			// 
			// txtAugShear
			// 
			txtAugShear.Enabled = false;
			txtAugShear.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txtAugShear.Location = new Point(937, 350);
			txtAugShear.Name = "txtAugShear";
			txtAugShear.Size = new Size(48, 33);
			txtAugShear.TabIndex = 49;
			txtAugShear.Text = "0";
			txtAugShear.Visible = false;
			// 
			// label19
			// 
			label19.AutoSize = true;
			label19.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label19.Location = new Point(859, 352);
			label19.Name = "label19";
			label19.Size = new Size(66, 25);
			label19.TabIndex = 48;
			label19.Text = "Shear:";
			label19.Visible = false;
			// 
			// ckbConvertToGreyscale
			// 
			ckbConvertToGreyscale.AutoSize = true;
			ckbConvertToGreyscale.CheckAlign = ContentAlignment.MiddleRight;
			ckbConvertToGreyscale.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			ckbConvertToGreyscale.Location = new Point(37, 199);
			ckbConvertToGreyscale.Name = "ckbConvertToGreyscale";
			ckbConvertToGreyscale.Size = new Size(208, 29);
			ckbConvertToGreyscale.TabIndex = 50;
			ckbConvertToGreyscale.Text = "Convert to greyscale";
			ckbConvertToGreyscale.UseVisualStyleBackColor = true;
			// 
			// rbTrainImagesAsGreyscale
			// 
			rbTrainImagesAsGreyscale.AutoSize = true;
			rbTrainImagesAsGreyscale.Checked = true;
			rbTrainImagesAsGreyscale.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rbTrainImagesAsGreyscale.Location = new Point(3, 3);
			rbTrainImagesAsGreyscale.Name = "rbTrainImagesAsGreyscale";
			rbTrainImagesAsGreyscale.Size = new Size(212, 25);
			rbTrainImagesAsGreyscale.TabIndex = 53;
			rbTrainImagesAsGreyscale.TabStop = true;
			rbTrainImagesAsGreyscale.Text = "Train images as Greyscale";
			rbTrainImagesAsGreyscale.UseVisualStyleBackColor = true;
			// 
			// rbTrainImagesAsRgb
			// 
			rbTrainImagesAsRgb.AutoSize = true;
			rbTrainImagesAsRgb.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rbTrainImagesAsRgb.Location = new Point(3, 31);
			rbTrainImagesAsRgb.Name = "rbTrainImagesAsRgb";
			rbTrainImagesAsRgb.Size = new Size(173, 25);
			rbTrainImagesAsRgb.TabIndex = 54;
			rbTrainImagesAsRgb.Text = "Train images as RGB";
			rbTrainImagesAsRgb.UseVisualStyleBackColor = true;
			// 
			// panelRadioTrainImagesAs
			// 
			panelRadioTrainImagesAs.Controls.Add(rbTrainImagesAsGreyscale);
			panelRadioTrainImagesAs.Controls.Add(rbTrainImagesAsRgb);
			panelRadioTrainImagesAs.Location = new Point(338, 299);
			panelRadioTrainImagesAs.Name = "panelRadioTrainImagesAs";
			panelRadioTrainImagesAs.Size = new Size(225, 64);
			panelRadioTrainImagesAs.TabIndex = 55;
			// 
			// ckbWithBorderPadding
			// 
			ckbWithBorderPadding.AutoSize = true;
			ckbWithBorderPadding.CheckAlign = ContentAlignment.MiddleRight;
			ckbWithBorderPadding.Checked = true;
			ckbWithBorderPadding.CheckState = CheckState.Checked;
			ckbWithBorderPadding.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			ckbWithBorderPadding.Location = new Point(82, 232);
			ckbWithBorderPadding.Name = "ckbWithBorderPadding";
			ckbWithBorderPadding.Size = new Size(163, 29);
			ckbWithBorderPadding.TabIndex = 56;
			ckbWithBorderPadding.Text = "Border padding";
			ckbWithBorderPadding.UseVisualStyleBackColor = true;
			// 
			// txbSplitTrainValidationSet
			// 
			txbSplitTrainValidationSet.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			txbSplitTrainValidationSet.Location = new Point(590, 260);
			txbSplitTrainValidationSet.Name = "txbSplitTrainValidationSet";
			txbSplitTrainValidationSet.Size = new Size(64, 33);
			txbSplitTrainValidationSet.TabIndex = 58;
			txbSplitTrainValidationSet.Text = "1";
			// 
			// label21
			// 
			label21.AutoSize = true;
			label21.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label21.Location = new Point(379, 262);
			label21.Name = "label21";
			label21.Size = new Size(193, 25);
			label21.TabIndex = 57;
			label21.Text = "Split train/validation:";
			// 
			// progressBar1
			// 
			progressBar1.Location = new Point(571, 786);
			progressBar1.Margin = new Padding(3, 2, 3, 2);
			progressBar1.Name = "progressBar1";
			progressBar1.Size = new Size(753, 34);
			progressBar1.TabIndex = 59;
			// 
			// tbProjectPath
			// 
			tbProjectPath.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tbProjectPath.Location = new Point(714, 745);
			tbProjectPath.Name = "tbProjectPath";
			tbProjectPath.Size = new Size(610, 36);
			tbProjectPath.TabIndex = 61;
			tbProjectPath.Text = "D:\\Projekte\\_GITHUB\\DATA\\DichtflächenKontrolle\\";
			tbProjectPath.KeyDown += tbProjectPath_KeyDown;
			// 
			// label22
			// 
			label22.AutoSize = true;
			label22.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
			label22.Location = new Point(569, 745);
			label22.Name = "label22";
			label22.Size = new Size(139, 30);
			label22.TabIndex = 60;
			label22.Text = "Project path:";
			// 
			// menuStrip1
			// 
			menuStrip1.ImageScalingSize = new Size(20, 20);
			menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, testToolStripMenuItem, questionStripMenuItem });
			menuStrip1.Location = new Point(0, 0);
			menuStrip1.Name = "menuStrip1";
			menuStrip1.Padding = new Padding(5, 2, 0, 2);
			menuStrip1.Size = new Size(1355, 24);
			menuStrip1.TabIndex = 62;
			menuStrip1.Text = "menuStrip1";
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { loadSettingsToUiToolStripMenuItem, exitToolStripMenuItem });
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new Size(37, 20);
			toolStripMenuItem1.Text = "File";
			// 
			// loadSettingsToUiToolStripMenuItem
			// 
			loadSettingsToUiToolStripMenuItem.Name = "loadSettingsToUiToolStripMenuItem";
			loadSettingsToUiToolStripMenuItem.Size = new Size(172, 22);
			loadSettingsToUiToolStripMenuItem.Text = "Load settings to UI";
			loadSettingsToUiToolStripMenuItem.Click += loadSettingsToUiToolStripMenuItem_Click;
			// 
			// exitToolStripMenuItem
			// 
			exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			exitToolStripMenuItem.Size = new Size(172, 22);
			exitToolStripMenuItem.Text = "Exit";
			exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
			// 
			// testToolStripMenuItem
			// 
			testToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { trainSimpleModelToolStripMenuItem, testLibTorchToolStripMenuItem, resetBatchListToolStripMenuItem });
			testToolStripMenuItem.Name = "testToolStripMenuItem";
			testToolStripMenuItem.Size = new Size(40, 20);
			testToolStripMenuItem.Text = "Test";
			// 
			// trainSimpleModelToolStripMenuItem
			// 
			trainSimpleModelToolStripMenuItem.Name = "trainSimpleModelToolStripMenuItem";
			trainSimpleModelToolStripMenuItem.Size = new Size(175, 22);
			trainSimpleModelToolStripMenuItem.Text = "Train simple model";
			trainSimpleModelToolStripMenuItem.Click += trainSimpleModelToolStripMenuItem_Click;
			// 
			// testLibTorchToolStripMenuItem
			// 
			testLibTorchToolStripMenuItem.Name = "testLibTorchToolStripMenuItem";
			testLibTorchToolStripMenuItem.Size = new Size(175, 22);
			testLibTorchToolStripMenuItem.Text = "Test empty cache";
			testLibTorchToolStripMenuItem.Click += testLibTorchToolStripMenuItem_Click;
			// 
			// resetBatchListToolStripMenuItem
			// 
			resetBatchListToolStripMenuItem.Name = "resetBatchListToolStripMenuItem";
			resetBatchListToolStripMenuItem.Size = new Size(175, 22);
			resetBatchListToolStripMenuItem.Text = "Reset batch list";
			resetBatchListToolStripMenuItem.Click += resetBatchListToolStripMenuItem_Click;
			// 
			// questionStripMenuItem
			// 
			questionStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { versionToolStripMenuItem });
			questionStripMenuItem.Name = "questionStripMenuItem";
			questionStripMenuItem.Size = new Size(24, 20);
			questionStripMenuItem.Text = "?";
			// 
			// versionToolStripMenuItem
			// 
			versionToolStripMenuItem.Name = "versionToolStripMenuItem";
			versionToolStripMenuItem.Size = new Size(112, 22);
			versionToolStripMenuItem.Text = "Version";
			versionToolStripMenuItem.Click += versionToolStripMenuItem_Click_1;
			// 
			// cBRoiSize
			// 
			cBRoiSize.DropDownStyle = ComboBoxStyle.DropDownList;
			cBRoiSize.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			cBRoiSize.FormattingEnabled = true;
			cBRoiSize.Location = new Point(230, 86);
			cBRoiSize.Name = "cBRoiSize";
			cBRoiSize.Size = new Size(61, 33);
			cBRoiSize.TabIndex = 65;
			// 
			// cbFeatures
			// 
			cbFeatures.DropDownStyle = ComboBoxStyle.DropDownList;
			cbFeatures.Enabled = false;
			cbFeatures.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			cbFeatures.FormattingEnabled = true;
			cbFeatures.Location = new Point(590, 189);
			cbFeatures.Name = "cbFeatures";
			cbFeatures.Size = new Size(40, 33);
			cbFeatures.TabIndex = 66;
			// 
			// cBDownSampling
			// 
			cBDownSampling.DropDownStyle = ComboBoxStyle.DropDownList;
			cBDownSampling.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			cBDownSampling.FormattingEnabled = true;
			cBDownSampling.Location = new Point(230, 125);
			cBDownSampling.Name = "cBDownSampling";
			cBDownSampling.Size = new Size(43, 33);
			cBDownSampling.TabIndex = 67;
			// 
			// panelTrainPrecision
			// 
			panelTrainPrecision.Controls.Add(rBTrainAsFloat16);
			panelTrainPrecision.Controls.Add(rBTrainAsFloat32);
			panelTrainPrecision.Enabled = false;
			panelTrainPrecision.Location = new Point(569, 299);
			panelTrainPrecision.Name = "panelTrainPrecision";
			panelTrainPrecision.Size = new Size(106, 64);
			panelTrainPrecision.TabIndex = 56;
			// 
			// rBTrainAsFloat16
			// 
			rBTrainAsFloat16.AutoSize = true;
			rBTrainAsFloat16.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainAsFloat16.Location = new Point(3, 3);
			rBTrainAsFloat16.Name = "rBTrainAsFloat16";
			rBTrainAsFloat16.Size = new Size(89, 25);
			rBTrainAsFloat16.TabIndex = 53;
			rBTrainAsFloat16.Text = "BFloat16";
			rBTrainAsFloat16.UseVisualStyleBackColor = true;
			// 
			// rBTrainAsFloat32
			// 
			rBTrainAsFloat32.AutoSize = true;
			rBTrainAsFloat32.Checked = true;
			rBTrainAsFloat32.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainAsFloat32.Location = new Point(3, 31);
			rBTrainAsFloat32.Name = "rBTrainAsFloat32";
			rBTrainAsFloat32.Size = new Size(82, 25);
			rBTrainAsFloat32.TabIndex = 54;
			rBTrainAsFloat32.TabStop = true;
			rBTrainAsFloat32.Text = "Float32";
			rBTrainAsFloat32.UseVisualStyleBackColor = true;
			// 
			// panelTrainOnDevice
			// 
			panelTrainOnDevice.Controls.Add(rBTrainOnDeviceCpu);
			panelTrainOnDevice.Controls.Add(rBTrainOnDeviceCuda);
			panelTrainOnDevice.Location = new Point(681, 299);
			panelTrainOnDevice.Name = "panelTrainOnDevice";
			panelTrainOnDevice.Size = new Size(174, 64);
			panelTrainOnDevice.TabIndex = 68;
			// 
			// rBTrainOnDeviceCpu
			// 
			rBTrainOnDeviceCpu.AutoSize = true;
			rBTrainOnDeviceCpu.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainOnDeviceCpu.Location = new Point(3, 3);
			rBTrainOnDeviceCpu.Name = "rBTrainOnDeviceCpu";
			rBTrainOnDeviceCpu.Size = new Size(58, 25);
			rBTrainOnDeviceCpu.TabIndex = 53;
			rBTrainOnDeviceCpu.Text = "CPU";
			rBTrainOnDeviceCpu.UseVisualStyleBackColor = true;
			// 
			// rBTrainOnDeviceCuda
			// 
			rBTrainOnDeviceCuda.AutoSize = true;
			rBTrainOnDeviceCuda.Checked = true;
			rBTrainOnDeviceCuda.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainOnDeviceCuda.Location = new Point(3, 31);
			rBTrainOnDeviceCuda.Name = "rBTrainOnDeviceCuda";
			rBTrainOnDeviceCuda.Size = new Size(71, 25);
			rBTrainOnDeviceCuda.TabIndex = 54;
			rBTrainOnDeviceCuda.TabStop = true;
			rBTrainOnDeviceCuda.Text = "CUDA";
			rBTrainOnDeviceCuda.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			label9.AutoSize = true;
			label9.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label9.Location = new Point(856, 685);
			label9.Name = "label9";
			label9.Size = new Size(104, 50);
			label9.TabIndex = 69;
			label9.Text = "Heatmaps \r\nthreshold:\r\n";
			// 
			// tbThreasholdHeatmaps
			// 
			tbThreasholdHeatmaps.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tbThreasholdHeatmaps.Location = new Point(962, 704);
			tbThreasholdHeatmaps.Name = "tbThreasholdHeatmaps";
			tbThreasholdHeatmaps.Size = new Size(44, 33);
			tbThreasholdHeatmaps.TabIndex = 70;
			tbThreasholdHeatmaps.Text = "200";
			// 
			// label23
			// 
			label23.AutoSize = true;
			label23.Font = new Font("Segoe UI Black", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label23.Location = new Point(26, 36);
			label23.Name = "label23";
			label23.Size = new Size(219, 32);
			label23.TabIndex = 71;
			label23.Text = "Data preparation:";
			// 
			// panel1
			// 
			panel1.BackColor = Color.LightGray;
			panel1.Location = new Point(309, 36);
			panel1.Name = "panel1";
			panel1.Size = new Size(12, 363);
			panel1.TabIndex = 69;
			// 
			// panel2
			// 
			panel2.BackColor = Color.LightGray;
			panel2.Location = new Point(813, 414);
			panel2.Name = "panel2";
			panel2.Size = new Size(12, 324);
			panel2.TabIndex = 70;
			// 
			// btnStopTraining
			// 
			btnStopTraining.FlatStyle = FlatStyle.System;
			btnStopTraining.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnStopTraining.Location = new Point(611, 581);
			btnStopTraining.Name = "btnStopTraining";
			btnStopTraining.Size = new Size(150, 50);
			btnStopTraining.TabIndex = 72;
			btnStopTraining.Text = "Stop training";
			btnStopTraining.UseVisualStyleBackColor = true;
			btnStopTraining.Click += btnStopTraining_Click;
			// 
			// btnStopRunModel
			// 
			btnStopRunModel.FlatStyle = FlatStyle.System;
			btnStopRunModel.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnStopRunModel.Location = new Point(856, 581);
			btnStopRunModel.Name = "btnStopRunModel";
			btnStopRunModel.Size = new Size(149, 50);
			btnStopRunModel.TabIndex = 73;
			btnStopRunModel.Text = "Stop run";
			btnStopRunModel.UseVisualStyleBackColor = true;
			btnStopRunModel.Click += btnStopRunModel_Click;
			// 
			// lbSystemRam
			// 
			lbSystemRam.AutoSize = true;
			lbSystemRam.Location = new Point(1043, 8);
			lbSystemRam.Name = "lbSystemRam";
			lbSystemRam.Size = new Size(74, 15);
			lbSystemRam.TabIndex = 75;
			lbSystemRam.Text = "myTotalRam";
			// 
			// btnTrainUNetModel
			// 
			btnTrainUNetModel.FlatStyle = FlatStyle.System;
			btnTrainUNetModel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnTrainUNetModel.Location = new Point(611, 412);
			btnTrainUNetModel.Name = "btnTrainUNetModel";
			btnTrainUNetModel.Size = new Size(150, 122);
			btnTrainUNetModel.TabIndex = 80;
			btnTrainUNetModel.Text = "Train new model";
			btnTrainUNetModel.UseVisualStyleBackColor = true;
			btnTrainUNetModel.Click += btnTrainUNetModel_Click;
			// 
			// ckbSaveLog
			// 
			ckbSaveLog.AutoSize = true;
			ckbSaveLog.Location = new Point(1238, 45);
			ckbSaveLog.Name = "ckbSaveLog";
			ckbSaveLog.Size = new Size(70, 19);
			ckbSaveLog.TabIndex = 82;
			ckbSaveLog.Text = "Save log";
			ckbSaveLog.UseVisualStyleBackColor = true;
			// 
			// cBbatchDivisors
			// 
			cBbatchDivisors.DropDownStyle = ComboBoxStyle.DropDownList;
			cBbatchDivisors.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			cBbatchDivisors.FormattingEnabled = true;
			cBbatchDivisors.Location = new Point(590, 120);
			cBbatchDivisors.Name = "cBbatchDivisors";
			cBbatchDivisors.Size = new Size(70, 33);
			cBbatchDivisors.TabIndex = 83;
			// 
			// formsPlot1
			// 
			formsPlot1.DisplayScale = 1F;
			formsPlot1.Location = new Point(12, 405);
			formsPlot1.Name = "formsPlot1";
			formsPlot1.Size = new Size(541, 424);
			formsPlot1.TabIndex = 84;
			// 
			// btnPrepareTrainData
			// 
			btnPrepareTrainData.FlatStyle = FlatStyle.System;
			btnPrepareTrainData.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnPrepareTrainData.Location = new Point(61, 262);
			btnPrepareTrainData.Margin = new Padding(3, 2, 3, 2);
			btnPrepareTrainData.Name = "btnPrepareTrainData";
			btnPrepareTrainData.Size = new Size(212, 60);
			btnPrepareTrainData.TabIndex = 85;
			btnPrepareTrainData.Text = "Prepare train data";
			btnPrepareTrainData.UseVisualStyleBackColor = true;
			btnPrepareTrainData.Click += btnPrepareTrainData_Click;
			// 
			// panel3
			// 
			panel3.Controls.Add(rbPointLocation);
			panel3.Controls.Add(rbFeatureDetection);
			panel3.Location = new Point(12, 330);
			panel3.Name = "panel3";
			panel3.Size = new Size(173, 69);
			panel3.TabIndex = 86;
			// 
			// rbPointLocation
			// 
			rbPointLocation.AutoSize = true;
			rbPointLocation.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rbPointLocation.Location = new Point(3, 35);
			rbPointLocation.Name = "rbPointLocation";
			rbPointLocation.Size = new Size(132, 25);
			rbPointLocation.TabIndex = 1;
			rbPointLocation.Text = "Point Location";
			rbPointLocation.UseVisualStyleBackColor = true;
			// 
			// rbFeatureDetection
			// 
			rbFeatureDetection.AutoSize = true;
			rbFeatureDetection.Checked = true;
			rbFeatureDetection.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rbFeatureDetection.Location = new Point(3, 7);
			rbFeatureDetection.Name = "rbFeatureDetection";
			rbFeatureDetection.Size = new Size(159, 25);
			rbFeatureDetection.TabIndex = 0;
			rbFeatureDetection.TabStop = true;
			rbFeatureDetection.Text = "Feature Detection";
			rbFeatureDetection.UseVisualStyleBackColor = true;
			// 
			// label24
			// 
			label24.AutoSize = true;
			label24.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			label24.Location = new Point(205, 330);
			label24.Name = "label24";
			label24.Size = new Size(98, 25);
			label24.TabIndex = 87;
			label24.Text = "Point size:";
			// 
			// tbSideLength
			// 
			tbSideLength.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tbSideLength.Location = new Point(230, 365);
			tbSideLength.Name = "tbSideLength";
			tbSideLength.Size = new Size(48, 33);
			tbSideLength.TabIndex = 88;
			tbSideLength.Text = "140";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1355, 838);
			Controls.Add(tbSideLength);
			Controls.Add(label24);
			Controls.Add(panel3);
			Controls.Add(btnPrepareTrainData);
			Controls.Add(formsPlot1);
			Controls.Add(cBbatchDivisors);
			Controls.Add(ckbSaveLog);
			Controls.Add(btnTrainUNetModel);
			Controls.Add(lbSystemRam);
			Controls.Add(btnStopRunModel);
			Controls.Add(btnStopTraining);
			Controls.Add(panel2);
			Controls.Add(panel1);
			Controls.Add(label23);
			Controls.Add(tbThreasholdHeatmaps);
			Controls.Add(label9);
			Controls.Add(panelTrainOnDevice);
			Controls.Add(panelTrainPrecision);
			Controls.Add(cBDownSampling);
			Controls.Add(cbFeatures);
			Controls.Add(cBRoiSize);
			Controls.Add(tbProjectPath);
			Controls.Add(label22);
			Controls.Add(progressBar1);
			Controls.Add(txbSplitTrainValidationSet);
			Controls.Add(label21);
			Controls.Add(ckbWithBorderPadding);
			Controls.Add(panelRadioTrainImagesAs);
			Controls.Add(ckbConvertToGreyscale);
			Controls.Add(txtAugShear);
			Controls.Add(label19);
			Controls.Add(txtAugScale);
			Controls.Add(label20);
			Controls.Add(txtAugFlip);
			Controls.Add(label17);
			Controls.Add(txtAugRotation);
			Controls.Add(label18);
			Controls.Add(txtAugGaussianBlur);
			Controls.Add(label12);
			Controls.Add(txtAugNoise);
			Controls.Add(label13);
			Controls.Add(txtAugContrast);
			Controls.Add(txtAugBrightness);
			Controls.Add(txtAugLuminance);
			Controls.Add(label14);
			Controls.Add(label15);
			Controls.Add(label16);
			Controls.Add(label11);
			Controls.Add(label10);
			Controls.Add(label8);
			Controls.Add(ckFilterByBlobs);
			Controls.Add(btnRunModelOnTestData);
			Controls.Add(label7);
			Controls.Add(label6);
			Controls.Add(txtStopAtLoss);
			Controls.Add(label5);
			Controls.Add(label4);
			Controls.Add(txtLog);
			Controls.Add(txtLearningRate);
			Controls.Add(txtEpochs);
			Controls.Add(label3);
			Controls.Add(label2);
			Controls.Add(label1);
			Controls.Add(menuStrip1);
			Icon = (Icon)resources.GetObject("$this.Icon");
			MainMenuStrip = menuStrip1;
			Name = "MainForm";
			Text = "Train ai model";
			panelRadioTrainImagesAs.ResumeLayout(false);
			panelRadioTrainImagesAs.PerformLayout();
			menuStrip1.ResumeLayout(false);
			menuStrip1.PerformLayout();
			panelTrainPrecision.ResumeLayout(false);
			panelTrainPrecision.PerformLayout();
			panelTrainOnDevice.ResumeLayout(false);
			panelTrainOnDevice.PerformLayout();
			panel3.ResumeLayout(false);
			panel3.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private Label label1;
		private Label label2;
		private Label label3;
		private TextBox txtEpochs;
		private TextBox txtLearningRate;
		private TextBox txtLog;
		private Label label4;
		private TextBox txtStopAtLoss;
		private Label label5;
        private Label label6;
        private Label label7;
		private Button btnRunModelOnTestData;
        private CheckBox ckFilterByBlobs;
        private Label label8;
        private Label label10;
        private Label label11;
        private TextBox txtAugGaussianBlur;
        private Label label12;
        private TextBox txtAugNoise;
        private Label label13;
        private TextBox txtAugContrast;
        private TextBox txtAugBrightness;
        private TextBox txtAugLuminance;
        private Label label14;
        private Label label15;
        private Label label16;
        private TextBox txtAugFlip;
        private Label label17;
        private TextBox txtAugRotation;
        private Label label18;
        private TextBox txtAugScale;
        private Label label20;
        private TextBox txtAugShear;
        private Label label19;
        private CheckBox ckbConvertToGreyscale;
		private RadioButton rbTrainImagesAsGreyscale;
		private RadioButton rbTrainImagesAsRgb;
		private Panel panelRadioTrainImagesAs;
		private CheckBox ckbWithBorderPadding;
		private TextBox txbSplitTrainValidationSet;
		private Label label21;
        private ProgressBar progressBar1;
        private TextBox tbProjectPath;
        private Label label22;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem questionStripMenuItem;
		private ComboBox cBRoiSize;
		private ComboBox cbFeatures;
		private ComboBox cBDownSampling;
		private Panel panelTrainPrecision;
		private RadioButton rBTrainAsFloat16;
		private RadioButton rBTrainAsFloat32;
		private Panel panelTrainOnDevice;
		private RadioButton rBTrainOnDeviceCpu;
		private RadioButton rBTrainOnDeviceCuda;
		private Label label9;
		private TextBox tbThreasholdHeatmaps;
		private Label label23;
		private Panel panel1;
		private Panel panel2;
		private Button btnStopTraining;
		private Button btnStopRunModel;
		private ToolStripMenuItem testToolStripMenuItem;
        private Label lbSystemRam;
        private ToolStripMenuItem trainSimpleModelToolStripMenuItem;
		private Button btnTrainUNetModel;
		private ToolStripMenuItem loadSettingsToUiToolStripMenuItem;
		private ToolStripMenuItem testLibTorchToolStripMenuItem;
		private ToolStripMenuItem versionToolStripMenuItem;
		private CheckBox ckbSaveLog;
		private ComboBox cBbatchDivisors;
		private ToolStripMenuItem resetBatchListToolStripMenuItem;
		private ScottPlot.WinForms.FormsPlot formsPlot1;
		private Button btnPrepareTrainData;
		private Panel panel3;
		private RadioButton rbPointLocation;
		private RadioButton rbFeatureDetection;
		private Label label24;
		private TextBox tbSideLength;
	}
}