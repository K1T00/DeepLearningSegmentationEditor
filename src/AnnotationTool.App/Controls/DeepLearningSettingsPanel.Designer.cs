namespace AnnotationTool.App.Controls
{
    partial class DeepLearningSettingsPanel
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
			pgDeepLearningSettings = new PropertyGrid();
			SuspendLayout();
			// 
			// pgDeepLearningSettings
			// 
			pgDeepLearningSettings.BackColor = SystemColors.Control;
			pgDeepLearningSettings.Dock = DockStyle.Fill;
			pgDeepLearningSettings.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
			pgDeepLearningSettings.Location = new Point(0, 0);
			pgDeepLearningSettings.Name = "pgDeepLearningSettings";
			pgDeepLearningSettings.PropertySort = PropertySort.NoSort;
			pgDeepLearningSettings.Size = new Size(366, 929);
			pgDeepLearningSettings.TabIndex = 53;
			pgDeepLearningSettings.ToolbarVisible = false;
			// 
			// DeepLearningSettingsPanel
			// 
			AutoScaleDimensions = new SizeF(9F, 21F);
			AutoScaleMode = AutoScaleMode.Font;
			Controls.Add(pgDeepLearningSettings);
			Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			Margin = new Padding(4);
			Name = "DeepLearningSettingsPanel";
			Size = new Size(366, 929);
			ResumeLayout(false);
		}

		#endregion
		private PropertyGrid pgDeepLearningSettings;
	}
}
