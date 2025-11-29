namespace AnnotationTool.App.Forms
{
	partial class TrainingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrainingForm));
            btnClose = new Button();
            formsPlotTrainLoss = new ScottPlot.WinForms.FormsPlot();
            pBTrainingForms = new ProgressBar();
            btnStopTraining = new Button();
            label1 = new Label();
            tbLogTrainForm = new TextBox();
            SuspendLayout();
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.BackColor = Color.PeachPuff;
            btnClose.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            btnClose.ForeColor = Color.Black;
            btnClose.Location = new Point(981, 517);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(133, 43);
            btnClose.TabIndex = 14;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // 
            // formsPlotTrainLoss
            // 
            formsPlotTrainLoss.DisplayScale = 1F;
            formsPlotTrainLoss.Dock = DockStyle.Left;
            formsPlotTrainLoss.Location = new Point(0, 0);
            formsPlotTrainLoss.Name = "formsPlotTrainLoss";
            formsPlotTrainLoss.Size = new Size(643, 575);
            formsPlotTrainLoss.TabIndex = 13;
            // 
            // pBTrainingForms
            // 
            pBTrainingForms.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pBTrainingForms.Location = new Point(661, 480);
            pBTrainingForms.Name = "pBTrainingForms";
            pBTrainingForms.Size = new Size(453, 31);
            pBTrainingForms.TabIndex = 12;
            // 
            // btnStopTraining
            // 
            btnStopTraining.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnStopTraining.BackColor = Color.PeachPuff;
            btnStopTraining.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            btnStopTraining.ForeColor = Color.Black;
            btnStopTraining.Location = new Point(661, 520);
            btnStopTraining.Name = "btnStopTraining";
            btnStopTraining.Size = new Size(133, 43);
            btnStopTraining.TabIndex = 11;
            btnStopTraining.Text = "Stop";
            btnStopTraining.UseVisualStyleBackColor = false;
            btnStopTraining.Click += btnStopTraining_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            label1.Location = new Point(661, 5);
            label1.Name = "label1";
            label1.Size = new Size(38, 21);
            label1.TabIndex = 10;
            label1.Text = "Log";
            // 
            // tbLogTrainForm
            // 
            tbLogTrainForm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tbLogTrainForm.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            tbLogTrainForm.Location = new Point(661, 30);
            tbLogTrainForm.Multiline = true;
            tbLogTrainForm.Name = "tbLogTrainForm";
            tbLogTrainForm.ScrollBars = ScrollBars.Vertical;
            tbLogTrainForm.Size = new Size(453, 444);
            tbLogTrainForm.TabIndex = 9;
            // 
            // TrainingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1132, 575);
            Controls.Add(btnClose);
            Controls.Add(formsPlotTrainLoss);
            Controls.Add(pBTrainingForms);
            Controls.Add(btnStopTraining);
            Controls.Add(label1);
            Controls.Add(tbLogTrainForm);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "TrainingForm";
            Text = "Training";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnClose;
		private ScottPlot.WinForms.FormsPlot formsPlotTrainLoss;
		private ProgressBar pBTrainingForms;
		private Button btnStopTraining;
		private Label label1;
		private TextBox tbLogTrainForm;
	}
}