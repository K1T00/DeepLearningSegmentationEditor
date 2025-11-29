namespace AnnotationTool.App.Controls
{
	partial class FeaturesDisplay
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
			featuresGridLayoutPanel = new FlowLayoutPanel();
			SuspendLayout();
			// 
			// featuresGridLayoutPanel
			// 
			featuresGridLayoutPanel.Dock = DockStyle.Fill;
			featuresGridLayoutPanel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
			featuresGridLayoutPanel.Location = new Point(0, 0);
			featuresGridLayoutPanel.Name = "featuresGridLayoutPanel";
			featuresGridLayoutPanel.Size = new Size(787, 173);
			featuresGridLayoutPanel.TabIndex = 0;
			// 
			// FeaturesDisplayControl
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			Controls.Add(featuresGridLayoutPanel);
			Name = "FeaturesDisplayControl";
			Size = new Size(787, 173);
			ResumeLayout(false);
		}

		#endregion

		private FlowLayoutPanel featuresGridLayoutPanel;
	}
}
