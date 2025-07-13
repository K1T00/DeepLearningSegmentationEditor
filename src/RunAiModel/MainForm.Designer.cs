namespace RunAiModel
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			btnRunModel = new Button();
			pBImageToAnalyze = new PictureBox();
			pbImageAnalyzeResult = new PictureBox();
			btnExit = new Button();
			label1 = new Label();
			tBCalcTime = new TextBox();
			panelTrainOnDevice = new Panel();
			rBTrainOnDeviceCpu = new RadioButton();
			rBTrainOnDeviceCuda = new RadioButton();
			label2 = new Label();
			tbAmtBatchImages = new TextBox();
			label3 = new Label();
			progressBar1 = new ProgressBar();
			cbCreateHeatmap = new CheckBox();
			cbCreateByteArray = new CheckBox();
			cbCreateMask = new CheckBox();
			tbProjectPath = new TextBox();
			label22 = new Label();
			((System.ComponentModel.ISupportInitialize)pBImageToAnalyze).BeginInit();
			((System.ComponentModel.ISupportInitialize)pbImageAnalyzeResult).BeginInit();
			panelTrainOnDevice.SuspendLayout();
			SuspendLayout();
			// 
			// btnRunModel
			// 
			btnRunModel.FlatStyle = FlatStyle.System;
			btnRunModel.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
			btnRunModel.Location = new Point(10, 638);
			btnRunModel.Margin = new Padding(3, 2, 3, 2);
			btnRunModel.Name = "btnRunModel";
			btnRunModel.Size = new Size(154, 104);
			btnRunModel.TabIndex = 0;
			btnRunModel.Text = "Run model";
			btnRunModel.UseVisualStyleBackColor = true;
			btnRunModel.Click += btnRunModel_Click;
			// 
			// pBImageToAnalyze
			// 
			pBImageToAnalyze.Location = new Point(10, 10);
			pBImageToAnalyze.Margin = new Padding(3, 2, 3, 2);
			pBImageToAnalyze.Name = "pBImageToAnalyze";
			pBImageToAnalyze.Size = new Size(600, 623);
			pBImageToAnalyze.SizeMode = PictureBoxSizeMode.Zoom;
			pBImageToAnalyze.TabIndex = 1;
			pBImageToAnalyze.TabStop = false;
			// 
			// pbImageAnalyzeResult
			// 
			pbImageAnalyzeResult.Location = new Point(616, 10);
			pbImageAnalyzeResult.Margin = new Padding(3, 2, 3, 2);
			pbImageAnalyzeResult.Name = "pbImageAnalyzeResult";
			pbImageAnalyzeResult.Size = new Size(600, 623);
			pbImageAnalyzeResult.SizeMode = PictureBoxSizeMode.Zoom;
			pbImageAnalyzeResult.TabIndex = 2;
			pbImageAnalyzeResult.TabStop = false;
			// 
			// btnExit
			// 
			btnExit.FlatStyle = FlatStyle.System;
			btnExit.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			btnExit.Location = new Point(1091, 678);
			btnExit.Margin = new Padding(3, 2, 3, 2);
			btnExit.Name = "btnExit";
			btnExit.Size = new Size(125, 43);
			btnExit.TabIndex = 3;
			btnExit.Text = "Exit";
			btnExit.UseVisualStyleBackColor = true;
			btnExit.Click += btnExit_Click;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label1.Location = new Point(616, 646);
			label1.Name = "label1";
			label1.Size = new Size(103, 21);
			label1.TabIndex = 4;
			label1.Text = "Calc time for";
			// 
			// tBCalcTime
			// 
			tBCalcTime.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tBCalcTime.Location = new Point(894, 643);
			tBCalcTime.Name = "tBCalcTime";
			tBCalcTime.Size = new Size(68, 29);
			tBCalcTime.TabIndex = 5;
			// 
			// panelTrainOnDevice
			// 
			panelTrainOnDevice.Controls.Add(rBTrainOnDeviceCpu);
			panelTrainOnDevice.Controls.Add(rBTrainOnDeviceCuda);
			panelTrainOnDevice.Location = new Point(432, 638);
			panelTrainOnDevice.Name = "panelTrainOnDevice";
			panelTrainOnDevice.Size = new Size(178, 104);
			panelTrainOnDevice.TabIndex = 69;
			// 
			// rBTrainOnDeviceCpu
			// 
			rBTrainOnDeviceCpu.AutoSize = true;
			rBTrainOnDeviceCpu.Checked = true;
			rBTrainOnDeviceCpu.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainOnDeviceCpu.Location = new Point(3, 3);
			rBTrainOnDeviceCpu.Name = "rBTrainOnDeviceCpu";
			rBTrainOnDeviceCpu.Size = new Size(58, 25);
			rBTrainOnDeviceCpu.TabIndex = 53;
			rBTrainOnDeviceCpu.TabStop = true;
			rBTrainOnDeviceCpu.Text = "CPU";
			rBTrainOnDeviceCpu.UseVisualStyleBackColor = true;
			// 
			// rBTrainOnDeviceCuda
			// 
			rBTrainOnDeviceCuda.AutoSize = true;
			rBTrainOnDeviceCuda.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			rBTrainOnDeviceCuda.Location = new Point(3, 35);
			rBTrainOnDeviceCuda.Name = "rBTrainOnDeviceCuda";
			rBTrainOnDeviceCuda.Size = new Size(71, 25);
			rBTrainOnDeviceCuda.TabIndex = 54;
			rBTrainOnDeviceCuda.Text = "CUDA";
			rBTrainOnDeviceCuda.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label2.Location = new Point(968, 646);
			label2.Name = "label2";
			label2.Size = new Size(31, 21);
			label2.TabIndex = 70;
			label2.Text = "ms";
			// 
			// tbAmtBatchImages
			// 
			tbAmtBatchImages.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tbAmtBatchImages.Location = new Point(725, 643);
			tbAmtBatchImages.Name = "tbAmtBatchImages";
			tbAmtBatchImages.Size = new Size(46, 29);
			tbAmtBatchImages.TabIndex = 71;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			label3.Location = new Point(777, 646);
			label3.Name = "label3";
			label3.Size = new Size(111, 21);
			label3.TabIndex = 72;
			label3.Text = "batch images:";
			// 
			// progressBar1
			// 
			progressBar1.Location = new Point(616, 749);
			progressBar1.Margin = new Padding(3, 2, 3, 2);
			progressBar1.Name = "progressBar1";
			progressBar1.Size = new Size(600, 33);
			progressBar1.TabIndex = 73;
			// 
			// cbCreateHeatmap
			// 
			cbCreateHeatmap.AutoSize = true;
			cbCreateHeatmap.Checked = true;
			cbCreateHeatmap.CheckState = CheckState.Checked;
			cbCreateHeatmap.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			cbCreateHeatmap.Location = new Point(233, 709);
			cbCreateHeatmap.Name = "cbCreateHeatmap";
			cbCreateHeatmap.Size = new Size(168, 29);
			cbCreateHeatmap.TabIndex = 75;
			cbCreateHeatmap.Text = "Create heatmap";
			cbCreateHeatmap.UseVisualStyleBackColor = true;
			// 
			// cbCreateByteArray
			// 
			cbCreateByteArray.AutoSize = true;
			cbCreateByteArray.Checked = true;
			cbCreateByteArray.CheckState = CheckState.Checked;
			cbCreateByteArray.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			cbCreateByteArray.Location = new Point(233, 638);
			cbCreateByteArray.Name = "cbCreateByteArray";
			cbCreateByteArray.Size = new Size(152, 29);
			cbCreateByteArray.TabIndex = 76;
			cbCreateByteArray.Text = "Create byte [ ]";
			cbCreateByteArray.UseVisualStyleBackColor = true;
			// 
			// cbCreateMask
			// 
			cbCreateMask.AutoSize = true;
			cbCreateMask.Checked = true;
			cbCreateMask.CheckState = CheckState.Checked;
			cbCreateMask.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold);
			cbCreateMask.Location = new Point(233, 673);
			cbCreateMask.Name = "cbCreateMask";
			cbCreateMask.Size = new Size(137, 29);
			cbCreateMask.TabIndex = 77;
			cbCreateMask.Text = "Create mask";
			cbCreateMask.UseVisualStyleBackColor = true;
			// 
			// tbProjectPath
			// 
			tbProjectPath.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			tbProjectPath.Location = new Point(121, 749);
			tbProjectPath.Name = "tbProjectPath";
			tbProjectPath.Size = new Size(489, 29);
			tbProjectPath.TabIndex = 79;
			tbProjectPath.KeyDown += tbProjectPath_KeyDown;
			// 
			// label22
			// 
			label22.AutoSize = true;
			label22.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
			label22.Location = new Point(12, 753);
			label22.Name = "label22";
			label22.Size = new Size(103, 21);
			label22.TabIndex = 78;
			label22.Text = "Project path:";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 17F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1246, 805);
			Controls.Add(tbProjectPath);
			Controls.Add(label22);
			Controls.Add(cbCreateMask);
			Controls.Add(cbCreateByteArray);
			Controls.Add(cbCreateHeatmap);
			Controls.Add(progressBar1);
			Controls.Add(label3);
			Controls.Add(tbAmtBatchImages);
			Controls.Add(label2);
			Controls.Add(panelTrainOnDevice);
			Controls.Add(tBCalcTime);
			Controls.Add(label1);
			Controls.Add(btnExit);
			Controls.Add(pbImageAnalyzeResult);
			Controls.Add(pBImageToAnalyze);
			Controls.Add(btnRunModel);
			Icon = (Icon)resources.GetObject("$this.Icon");
			Margin = new Padding(3, 2, 3, 2);
			Name = "MainForm";
			Text = "Use ai model";
			((System.ComponentModel.ISupportInitialize)pBImageToAnalyze).EndInit();
			((System.ComponentModel.ISupportInitialize)pbImageAnalyzeResult).EndInit();
			panelTrainOnDevice.ResumeLayout(false);
			panelTrainOnDevice.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button btnRunModel;
        private PictureBox pBImageToAnalyze;
        private PictureBox pbImageAnalyzeResult;
        private Button btnExit;
		private Label label1;
		private TextBox tBCalcTime;
		private Panel panelTrainOnDevice;
		private RadioButton rBTrainOnDeviceCpu;
		private RadioButton rBTrainOnDeviceCuda;
		private Label label2;
		private TextBox tbAmtBatchImages;
		private Label label3;
		private ProgressBar progressBar1;
		private CheckBox cbCreateHeatmap;
		private CheckBox cbCreateByteArray;
		private CheckBox cbCreateMask;
		private TextBox tbProjectPath;
		private Label label22;
	}
}
