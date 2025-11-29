using AnnotationTool.App.Controls;

namespace AnnotationTool.App
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
            btnAddImages = new Button();
            imagesControl = new ImageGrid();
            mainPictureBox = new PictureBox();
            btnDeleteImages = new Button();
            btnEditFeatures = new Button();
            featuresControl = new FeaturesDisplay();
            annotationToolsControl = new AnnotationToolsPanel();
            btnToggleRoi = new Button();
            label1 = new Label();
            tbProjectPath = new TextBox();
            btnSaveProject = new Button();
            btnLoadProject = new Button();
            btnExit = new Button();
            btnUpdateCategoryToTrain = new Button();
            btnUpdateCategoryToValidate = new Button();
            btnUpdateCategoryToTest = new Button();
            btnTrain = new Button();
            btnAnnotate = new Button();
            btnTrainingResults = new Button();
            pictureBox2 = new PictureBox();
            btnNewProject = new Button();
            btnSaveAs = new Button();
            splitContainer1 = new SplitContainer();
            splitContainerImageControlSplitButtons = new SplitContainer();
            deepLearningSettingsControl = new DeepLearningSettingsPanel();
            pictureBox3 = new PictureBox();
            inferenceResultsControlAllImages = new InferenceResultsView();
            inferenceResultsControlCurrentImage = new InferenceResultsView();
            lblResultsCurrentImg = new Label();
            lblResultsAllImgs = new Label();
            lblInferenceMeanComputeTime = new Label();
            tBThreshold = new TrackBar();
            lblThreshold = new Label();
            lblSystemVram = new Label();
            lblSystemRam = new Label();
            label3 = new Label();
            ((System.ComponentModel.ISupportInitialize)mainPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerImageControlSplitButtons).BeginInit();
            splitContainerImageControlSplitButtons.Panel1.SuspendLayout();
            splitContainerImageControlSplitButtons.Panel2.SuspendLayout();
            splitContainerImageControlSplitButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tBThreshold).BeginInit();
            SuspendLayout();
            // 
            // btnAddImages
            // 
            btnAddImages.BackColor = SystemColors.Control;
            btnAddImages.BackgroundImage = (Image)resources.GetObject("btnAddImages.BackgroundImage");
            btnAddImages.BackgroundImageLayout = ImageLayout.Stretch;
            btnAddImages.FlatStyle = FlatStyle.Flat;
            btnAddImages.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAddImages.ImageAlign = ContentAlignment.TopCenter;
            btnAddImages.Location = new Point(4, 4);
            btnAddImages.Margin = new Padding(4);
            btnAddImages.Name = "btnAddImages";
            btnAddImages.Size = new Size(50, 50);
            btnAddImages.TabIndex = 0;
            btnAddImages.UseVisualStyleBackColor = true;
            btnAddImages.Click += btnAddImages_Click;
            // 
            // imagesControl
            // 
            imagesControl.Dock = DockStyle.Fill;
            imagesControl.Location = new Point(0, 0);
            imagesControl.Margin = new Padding(4);
            imagesControl.Name = "imagesControl";
            imagesControl.Size = new Size(200, 723);
            imagesControl.TabIndex = 1;
            // 
            // mainPictureBox
            // 
            mainPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mainPictureBox.BorderStyle = BorderStyle.FixedSingle;
            mainPictureBox.Location = new Point(280, 87);
            mainPictureBox.Margin = new Padding(4);
            mainPictureBox.Name = "mainPictureBox";
            mainPictureBox.Size = new Size(750, 654);
            mainPictureBox.TabIndex = 2;
            mainPictureBox.TabStop = false;
            mainPictureBox.Paint += mainDisplayPictureBox_Paint;
            mainPictureBox.MouseDown += mainDisplayPictureBox_MouseDown;
            mainPictureBox.MouseMove += mainDisplayPictureBox_MouseMove;
            mainPictureBox.MouseUp += mainDisplayPictureBox_MouseUp;
            // 
            // btnDeleteImages
            // 
            btnDeleteImages.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDeleteImages.BackgroundImage = Properties.Resources.DeleteImageData;
            btnDeleteImages.BackgroundImageLayout = ImageLayout.Stretch;
            btnDeleteImages.FlatStyle = FlatStyle.Flat;
            btnDeleteImages.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnDeleteImages.Location = new Point(135, 4);
            btnDeleteImages.Margin = new Padding(4);
            btnDeleteImages.Name = "btnDeleteImages";
            btnDeleteImages.Size = new Size(50, 50);
            btnDeleteImages.TabIndex = 3;
            btnDeleteImages.UseVisualStyleBackColor = true;
            btnDeleteImages.Click += btnDeleteImages_Click;
            // 
            // btnEditFeatures
            // 
            btnEditFeatures.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnEditFeatures.BackgroundImage = Properties.Resources.EditFeatures;
            btnEditFeatures.BackgroundImageLayout = ImageLayout.Stretch;
            btnEditFeatures.FlatStyle = FlatStyle.Flat;
            btnEditFeatures.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnEditFeatures.Location = new Point(238, 748);
            btnEditFeatures.Margin = new Padding(4);
            btnEditFeatures.Name = "btnEditFeatures";
            btnEditFeatures.Size = new Size(33, 33);
            btnEditFeatures.TabIndex = 4;
            btnEditFeatures.UseVisualStyleBackColor = true;
            btnEditFeatures.Click += btnEditFeatures_Click;
            // 
            // featuresControl
            // 
            featuresControl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            featuresControl.BackColor = SystemColors.ActiveBorder;
            featuresControl.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            featuresControl.Location = new Point(280, 748);
            featuresControl.Margin = new Padding(5);
            featuresControl.Name = "featuresControl";
            featuresControl.Size = new Size(750, 33);
            featuresControl.TabIndex = 5;
            // 
            // annotationToolsControl
            // 
            annotationToolsControl.Enabled = false;
            annotationToolsControl.EraseActive = false;
            annotationToolsControl.Location = new Point(220, 87);
            annotationToolsControl.Margin = new Padding(4);
            annotationToolsControl.Name = "annotationToolsControl";
            annotationToolsControl.PaintActive = false;
            annotationToolsControl.Size = new Size(52, 289);
            annotationToolsControl.TabIndex = 6;
            annotationToolsControl.Visible = false;
            // 
            // btnToggleRoi
            // 
            btnToggleRoi.BackgroundImage = Properties.Resources.ToggleRoi;
            btnToggleRoi.BackgroundImageLayout = ImageLayout.Stretch;
            btnToggleRoi.Enabled = false;
            btnToggleRoi.FlatStyle = FlatStyle.Flat;
            btnToggleRoi.Location = new Point(220, 384);
            btnToggleRoi.Margin = new Padding(4);
            btnToggleRoi.Name = "btnToggleRoi";
            btnToggleRoi.Size = new Size(40, 40);
            btnToggleRoi.TabIndex = 7;
            btnToggleRoi.UseVisualStyleBackColor = true;
            btnToggleRoi.Visible = false;
            btnToggleRoi.Click += btnToggleRoi_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(712, 793);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(88, 19);
            label1.TabIndex = 8;
            label1.Text = "Project path:";
            // 
            // tbProjectPath
            // 
            tbProjectPath.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbProjectPath.Enabled = false;
            tbProjectPath.Font = new Font("Segoe UI Semibold", 7F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tbProjectPath.Location = new Point(712, 816);
            tbProjectPath.Margin = new Padding(4);
            tbProjectPath.Name = "tbProjectPath";
            tbProjectPath.Size = new Size(318, 20);
            tbProjectPath.TabIndex = 9;
            // 
            // btnSaveProject
            // 
            btnSaveProject.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveProject.BackgroundImageLayout = ImageLayout.Stretch;
            btnSaveProject.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSaveProject.ForeColor = Color.Black;
            btnSaveProject.Location = new Point(496, 793);
            btnSaveProject.Margin = new Padding(4);
            btnSaveProject.Name = "btnSaveProject";
            btnSaveProject.Size = new Size(100, 46);
            btnSaveProject.TabIndex = 10;
            btnSaveProject.Text = "Save";
            btnSaveProject.UseVisualStyleBackColor = true;
            btnSaveProject.Click += btnSaveProject_Click;
            // 
            // btnLoadProject
            // 
            btnLoadProject.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLoadProject.BackgroundImageLayout = ImageLayout.Stretch;
            btnLoadProject.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLoadProject.ForeColor = Color.Black;
            btnLoadProject.Location = new Point(388, 793);
            btnLoadProject.Margin = new Padding(4);
            btnLoadProject.Name = "btnLoadProject";
            btnLoadProject.Size = new Size(100, 46);
            btnLoadProject.TabIndex = 11;
            btnLoadProject.Text = "Load";
            btnLoadProject.UseVisualStyleBackColor = true;
            btnLoadProject.Click += btnLoadProject_Click;
            // 
            // btnExit
            // 
            btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExit.BackgroundImage = Properties.Resources.ExitApp;
            btnExit.BackgroundImageLayout = ImageLayout.Stretch;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Location = new Point(1646, 775);
            btnExit.Margin = new Padding(4);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(61, 61);
            btnExit.TabIndex = 12;
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // btnUpdateCategoryToTrain
            // 
            btnUpdateCategoryToTrain.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUpdateCategoryToTrain.BackColor = Color.LimeGreen;
            btnUpdateCategoryToTrain.FlatStyle = FlatStyle.Flat;
            btnUpdateCategoryToTrain.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnUpdateCategoryToTrain.ForeColor = Color.Black;
            btnUpdateCategoryToTrain.Location = new Point(4, 6);
            btnUpdateCategoryToTrain.Margin = new Padding(4);
            btnUpdateCategoryToTrain.Name = "btnUpdateCategoryToTrain";
            btnUpdateCategoryToTrain.Size = new Size(55, 28);
            btnUpdateCategoryToTrain.TabIndex = 13;
            btnUpdateCategoryToTrain.Text = "Train";
            btnUpdateCategoryToTrain.UseVisualStyleBackColor = false;
            btnUpdateCategoryToTrain.Click += btnUpdateCategoryToTrain_Click;
            // 
            // btnUpdateCategoryToValidate
            // 
            btnUpdateCategoryToValidate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUpdateCategoryToValidate.BackColor = Color.Yellow;
            btnUpdateCategoryToValidate.FlatStyle = FlatStyle.Flat;
            btnUpdateCategoryToValidate.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnUpdateCategoryToValidate.ForeColor = Color.Black;
            btnUpdateCategoryToValidate.Location = new Point(67, 6);
            btnUpdateCategoryToValidate.Margin = new Padding(4);
            btnUpdateCategoryToValidate.Name = "btnUpdateCategoryToValidate";
            btnUpdateCategoryToValidate.Size = new Size(66, 28);
            btnUpdateCategoryToValidate.TabIndex = 14;
            btnUpdateCategoryToValidate.Text = "Validate";
            btnUpdateCategoryToValidate.UseVisualStyleBackColor = false;
            btnUpdateCategoryToValidate.Click += btnUpdateCategoryToValidate_Click;
            // 
            // btnUpdateCategoryToTest
            // 
            btnUpdateCategoryToTest.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUpdateCategoryToTest.BackColor = Color.Orange;
            btnUpdateCategoryToTest.FlatStyle = FlatStyle.Flat;
            btnUpdateCategoryToTest.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnUpdateCategoryToTest.ForeColor = Color.Black;
            btnUpdateCategoryToTest.Location = new Point(141, 6);
            btnUpdateCategoryToTest.Margin = new Padding(4);
            btnUpdateCategoryToTest.Name = "btnUpdateCategoryToTest";
            btnUpdateCategoryToTest.Size = new Size(55, 28);
            btnUpdateCategoryToTest.TabIndex = 15;
            btnUpdateCategoryToTest.Text = "Test";
            btnUpdateCategoryToTest.UseVisualStyleBackColor = false;
            btnUpdateCategoryToTest.Click += btnUpdateCategoryToTest_Click;
            // 
            // btnTrain
            // 
            btnTrain.BackColor = Color.PeachPuff;
            btnTrain.FlatStyle = FlatStyle.Flat;
            btnTrain.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnTrain.ForeColor = Color.Black;
            btnTrain.Location = new Point(667, 16);
            btnTrain.Margin = new Padding(4);
            btnTrain.Name = "btnTrain";
            btnTrain.Size = new Size(129, 50);
            btnTrain.TabIndex = 17;
            btnTrain.Text = "Train";
            btnTrain.UseVisualStyleBackColor = false;
            btnTrain.Click += btnTrain_Click;
            // 
            // btnAnnotate
            // 
            btnAnnotate.BackColor = Color.PeachPuff;
            btnAnnotate.FlatStyle = FlatStyle.Flat;
            btnAnnotate.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnAnnotate.ForeColor = Color.Black;
            btnAnnotate.Location = new Point(446, 16);
            btnAnnotate.Margin = new Padding(4);
            btnAnnotate.Name = "btnAnnotate";
            btnAnnotate.Size = new Size(129, 50);
            btnAnnotate.TabIndex = 20;
            btnAnnotate.Text = "Annotate";
            btnAnnotate.UseVisualStyleBackColor = false;
            btnAnnotate.Click += btnAnnotate_Click;
            // 
            // btnTrainingResults
            // 
            btnTrainingResults.BackColor = Color.PeachPuff;
            btnTrainingResults.FlatStyle = FlatStyle.Flat;
            btnTrainingResults.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnTrainingResults.ForeColor = Color.Black;
            btnTrainingResults.Location = new Point(888, 16);
            btnTrainingResults.Margin = new Padding(4);
            btnTrainingResults.Name = "btnTrainingResults";
            btnTrainingResults.Size = new Size(129, 50);
            btnTrainingResults.TabIndex = 21;
            btnTrainingResults.Text = "Results";
            btnTrainingResults.UseVisualStyleBackColor = false;
            btnTrainingResults.Click += btnTrainingResults_Click;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = Properties.Resources.arrow_black;
            pictureBox2.Location = new Point(804, 24);
            pictureBox2.Margin = new Padding(4);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(76, 35);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 27;
            pictureBox2.TabStop = false;
            // 
            // btnNewProject
            // 
            btnNewProject.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNewProject.BackgroundImageLayout = ImageLayout.Stretch;
            btnNewProject.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnNewProject.ForeColor = Color.Black;
            btnNewProject.Location = new Point(280, 793);
            btnNewProject.Margin = new Padding(4);
            btnNewProject.Name = "btnNewProject";
            btnNewProject.Size = new Size(100, 46);
            btnNewProject.TabIndex = 28;
            btnNewProject.Text = "New";
            btnNewProject.UseVisualStyleBackColor = true;
            btnNewProject.Click += btnNewProject_Click;
            // 
            // btnSaveAs
            // 
            btnSaveAs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveAs.BackgroundImageLayout = ImageLayout.Stretch;
            btnSaveAs.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSaveAs.ForeColor = Color.Black;
            btnSaveAs.Location = new Point(604, 793);
            btnSaveAs.Margin = new Padding(4);
            btnSaveAs.Name = "btnSaveAs";
            btnSaveAs.Size = new Size(100, 46);
            btnSaveAs.TabIndex = 29;
            btnSaveAs.Text = "Save As";
            btnSaveAs.UseVisualStyleBackColor = true;
            btnSaveAs.Click += btnSaveAs_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(12, 12);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(btnDeleteImages);
            splitContainer1.Panel1.Controls.Add(btnAddImages);
            splitContainer1.Panel1MinSize = 50;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainerImageControlSplitButtons);
            splitContainer1.Size = new Size(200, 824);
            splitContainer1.SplitterDistance = 55;
            splitContainer1.TabIndex = 30;
            // 
            // splitContainerImageControlSplitButtons
            // 
            splitContainerImageControlSplitButtons.Dock = DockStyle.Fill;
            splitContainerImageControlSplitButtons.FixedPanel = FixedPanel.Panel2;
            splitContainerImageControlSplitButtons.IsSplitterFixed = true;
            splitContainerImageControlSplitButtons.Location = new Point(0, 0);
            splitContainerImageControlSplitButtons.Name = "splitContainerImageControlSplitButtons";
            splitContainerImageControlSplitButtons.Orientation = Orientation.Horizontal;
            // 
            // splitContainerImageControlSplitButtons.Panel1
            // 
            splitContainerImageControlSplitButtons.Panel1.Controls.Add(imagesControl);
            // 
            // splitContainerImageControlSplitButtons.Panel2
            // 
            splitContainerImageControlSplitButtons.Panel2.Controls.Add(btnUpdateCategoryToTrain);
            splitContainerImageControlSplitButtons.Panel2.Controls.Add(btnUpdateCategoryToValidate);
            splitContainerImageControlSplitButtons.Panel2.Controls.Add(btnUpdateCategoryToTest);
            splitContainerImageControlSplitButtons.Panel2MinSize = 38;
            splitContainerImageControlSplitButtons.Size = new Size(200, 765);
            splitContainerImageControlSplitButtons.SplitterDistance = 723;
            splitContainerImageControlSplitButtons.TabIndex = 31;
            // 
            // deepLearningSettingsControl
            // 
            deepLearningSettingsControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            deepLearningSettingsControl.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            deepLearningSettingsControl.ForceCpuOnly = false;
            deepLearningSettingsControl.Location = new Point(1339, 56);
            deepLearningSettingsControl.Margin = new Padding(4);
            deepLearningSettingsControl.Name = "deepLearningSettingsControl";
            deepLearningSettingsControl.Size = new Size(368, 711);
            deepLearningSettingsControl.TabIndex = 31;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = Properties.Resources.arrow_black;
            pictureBox3.Location = new Point(583, 24);
            pictureBox3.Margin = new Padding(4);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(76, 35);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.TabIndex = 35;
            pictureBox3.TabStop = false;
            // 
            // inferenceResultsControlAllImages
            // 
            inferenceResultsControlAllImages.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            inferenceResultsControlAllImages.Location = new Point(1037, 533);
            inferenceResultsControlAllImages.Margin = new Padding(3, 4, 3, 4);
            inferenceResultsControlAllImages.Name = "inferenceResultsControlAllImages";
            inferenceResultsControlAllImages.SegmentationStats = null;
            inferenceResultsControlAllImages.Size = new Size(295, 250);
            inferenceResultsControlAllImages.TabIndex = 37;
            // 
            // inferenceResultsControlCurrentImage
            // 
            inferenceResultsControlCurrentImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            inferenceResultsControlCurrentImage.Location = new Point(1037, 87);
            inferenceResultsControlCurrentImage.Margin = new Padding(3, 4, 3, 4);
            inferenceResultsControlCurrentImage.Name = "inferenceResultsControlCurrentImage";
            inferenceResultsControlCurrentImage.SegmentationStats = null;
            inferenceResultsControlCurrentImage.Size = new Size(295, 250);
            inferenceResultsControlCurrentImage.TabIndex = 38;
            // 
            // lblResultsCurrentImg
            // 
            lblResultsCurrentImg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblResultsCurrentImg.AutoSize = true;
            lblResultsCurrentImg.Location = new Point(1104, 62);
            lblResultsCurrentImg.Name = "lblResultsCurrentImg";
            lblResultsCurrentImg.Size = new Size(168, 21);
            lblResultsCurrentImg.TabIndex = 39;
            lblResultsCurrentImg.Text = "Results current image";
            // 
            // lblResultsAllImgs
            // 
            lblResultsAllImgs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblResultsAllImgs.AutoSize = true;
            lblResultsAllImgs.Location = new Point(1123, 508);
            lblResultsAllImgs.Name = "lblResultsAllImgs";
            lblResultsAllImgs.Size = new Size(138, 21);
            lblResultsAllImgs.TabIndex = 40;
            lblResultsAllImgs.Text = "Results all images";
            // 
            // lblInferenceMeanComputeTime
            // 
            lblInferenceMeanComputeTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblInferenceMeanComputeTime.AutoSize = true;
            lblInferenceMeanComputeTime.Location = new Point(1061, 818);
            lblInferenceMeanComputeTime.Name = "lblInferenceMeanComputeTime";
            lblInferenceMeanComputeTime.Size = new Size(186, 21);
            lblInferenceMeanComputeTime.TabIndex = 41;
            lblInferenceMeanComputeTime.Text = "Average compute time: ";
            // 
            // tBThreshold
            // 
            tBThreshold.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            tBThreshold.Location = new Point(1095, 413);
            tBThreshold.Maximum = 100;
            tBThreshold.Name = "tBThreshold";
            tBThreshold.Size = new Size(187, 45);
            tBThreshold.TabIndex = 44;
            tBThreshold.Value = 50;
            tBThreshold.ValueChanged += tBThreshold_ValueChanged;
            // 
            // lblThreshold
            // 
            lblThreshold.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblThreshold.AutoSize = true;
            lblThreshold.Location = new Point(1177, 447);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(28, 21);
            lblThreshold.TabIndex = 45;
            lblThreshold.Text = "50";
            // 
            // lblSystemVram
            // 
            lblSystemVram.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblSystemVram.AutoSize = true;
            lblSystemVram.Location = new Point(1403, 818);
            lblSystemVram.Name = "lblSystemVram";
            lblSystemVram.Size = new Size(109, 21);
            lblSystemVram.TabIndex = 46;
            lblSystemVram.Text = "128 GB VRAM";
            // 
            // lblSystemRam
            // 
            lblSystemRam.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblSystemRam.AutoSize = true;
            lblSystemRam.Location = new Point(1403, 796);
            lblSystemRam.Name = "lblSystemRam";
            lblSystemRam.Size = new Size(99, 21);
            lblSystemRam.TabIndex = 47;
            lblSystemRam.Text = "128 GB RAM";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(1105, 389);
            label3.Name = "label3";
            label3.Size = new Size(161, 21);
            label3.TabIndex = 49;
            label3.Text = "Threshold Heatmaps";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1720, 848);
            Controls.Add(label3);
            Controls.Add(deepLearningSettingsControl);
            Controls.Add(lblSystemRam);
            Controls.Add(lblSystemVram);
            Controls.Add(lblThreshold);
            Controls.Add(tBThreshold);
            Controls.Add(lblInferenceMeanComputeTime);
            Controls.Add(lblResultsAllImgs);
            Controls.Add(lblResultsCurrentImg);
            Controls.Add(inferenceResultsControlCurrentImage);
            Controls.Add(inferenceResultsControlAllImages);
            Controls.Add(pictureBox3);
            Controls.Add(mainPictureBox);
            Controls.Add(splitContainer1);
            Controls.Add(btnSaveAs);
            Controls.Add(btnNewProject);
            Controls.Add(pictureBox2);
            Controls.Add(btnTrainingResults);
            Controls.Add(btnAnnotate);
            Controls.Add(btnTrain);
            Controls.Add(btnExit);
            Controls.Add(btnLoadProject);
            Controls.Add(btnSaveProject);
            Controls.Add(tbProjectPath);
            Controls.Add(label1);
            Controls.Add(btnToggleRoi);
            Controls.Add(annotationToolsControl);
            Controls.Add(featuresControl);
            Controls.Add(btnEditFeatures);
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "Deep Learning Editor";
            ((System.ComponentModel.ISupportInitialize)mainPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainerImageControlSplitButtons.Panel1.ResumeLayout(false);
            splitContainerImageControlSplitButtons.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerImageControlSplitButtons).EndInit();
            splitContainerImageControlSplitButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)tBThreshold).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnAddImages;
		private ImageGrid imagesControl;
		private PictureBox mainPictureBox;
		private Button btnDeleteImages;
		private Button btnEditFeatures;
		private FeaturesDisplay featuresControl;
		private AnnotationToolsPanel annotationToolsControl;
		private Button btnToggleRoi;
		private Label label1;
		private TextBox tbProjectPath;
		private Button btnSaveProject;
		private Button btnLoadProject;
		private Button btnExit;
		private Button btnUpdateCategoryToTrain;
		private Button btnUpdateCategoryToValidate;
		private Button btnUpdateCategoryToTest;
		private Button btnTrain;
		private Button btnAnnotate;
		private Button btnTrainingResults;
		private PictureBox pictureBox2;
        private Button btnNewProject;
        private Button btnSaveAs;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainerImageControlSplitButtons;
		private DeepLearningSettingsPanel deepLearningSettingsControl;
		private PictureBox pictureBox3;
		private InferenceResultsView inferenceResultsControlAllImages;
		private InferenceResultsView inferenceResultsControlCurrentImage;
		private Label lblResultsCurrentImg;
		private Label lblResultsAllImgs;
		private Label lblInferenceMeanComputeTime;
		private TrackBar tBThreshold;
		private Label lblThreshold;
		private Label lblSystemVram;
		private Label lblSystemRam;
		private Label label3;
	}
}
