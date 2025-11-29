namespace AnnotationTool.App.Controls
{
	partial class InferenceResultsView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            radarPlotResults = new ScottPlot.WinForms.FormsPlot();
            SuspendLayout();
            // 
            // radarPlotResults
            // 
            radarPlotResults.DisplayScale = 1F;
            radarPlotResults.Dock = DockStyle.Fill;
            radarPlotResults.Location = new Point(0, 0);
            radarPlotResults.Name = "radarPlotResults";
            radarPlotResults.Size = new Size(399, 377);
            radarPlotResults.TabIndex = 0;
            radarPlotResults.MouseMove += radarPlotResults_MouseMove;
            // 
            // InferenceResultsView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(radarPlotResults);
            Name = "InferenceResultsView";
            Size = new Size(399, 377);
            Load += InferenceResultsControl_Load;
            ResumeLayout(false);
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot radarPlotResults;
	}
}
