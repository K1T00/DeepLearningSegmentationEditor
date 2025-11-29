namespace AnnotationTool.App.Controls
{
	partial class AnnotationToolsPanel
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
			btnPaint = new Button();
			btnEraser = new Button();
			tbBrushSize = new TrackBar();
			lbBrushSize = new Label();
			((System.ComponentModel.ISupportInitialize)tbBrushSize).BeginInit();
			SuspendLayout();
			// 
			// btnPaint
			// 
			btnPaint.BackgroundImage = Properties.Resources.PaintBrush;
			btnPaint.BackgroundImageLayout = ImageLayout.Stretch;
			btnPaint.FlatStyle = FlatStyle.Flat;
			btnPaint.Location = new Point(0, 0);
			btnPaint.Name = "btnPaint";
			btnPaint.Size = new Size(42, 34);
			btnPaint.TabIndex = 0;
			btnPaint.UseVisualStyleBackColor = true;
			btnPaint.Click += btnPaint_Click;
			// 
			// btnEraser
			// 
			btnEraser.BackColor = SystemColors.Control;
			btnEraser.BackgroundImage = Properties.Resources.Eraser;
			btnEraser.BackgroundImageLayout = ImageLayout.Stretch;
			btnEraser.FlatStyle = FlatStyle.Flat;
			btnEraser.Location = new Point(-1, 40);
			btnEraser.Name = "btnEraser";
			btnEraser.Size = new Size(42, 34);
			btnEraser.TabIndex = 1;
			btnEraser.UseVisualStyleBackColor = false;
			btnEraser.Click += btnEraser_Click;
			// 
			// tbBrushSize
			// 
			tbBrushSize.Location = new Point(3, 115);
			tbBrushSize.Maximum = 50;
			tbBrushSize.Name = "tbBrushSize";
			tbBrushSize.Orientation = Orientation.Vertical;
			tbBrushSize.Size = new Size(45, 165);
			tbBrushSize.TabIndex = 2;
			tbBrushSize.Value = 10;
			tbBrushSize.ValueChanged += tbBrushSize_ValueChanged;
			tbBrushSize.MouseDown += tbBrushSize_MouseDown;
			tbBrushSize.MouseUp += tbBrushSize_MouseUp;
			// 
			// lbBrushSize
			// 
			lbBrushSize.AutoSize = true;
			lbBrushSize.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
			lbBrushSize.Location = new Point(3, 77);
			lbBrushSize.Name = "lbBrushSize";
			lbBrushSize.Size = new Size(32, 25);
			lbBrushSize.TabIndex = 3;
			lbBrushSize.Text = "99";
			// 
			// AnnotationToolsPanel
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			Controls.Add(lbBrushSize);
			Controls.Add(tbBrushSize);
			Controls.Add(btnEraser);
			Controls.Add(btnPaint);
			Name = "AnnotationToolsPanel";
			Size = new Size(44, 280);
			((System.ComponentModel.ISupportInitialize)tbBrushSize).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button btnPaint;
		private Button btnEraser;
		private TrackBar tbBrushSize;
		private Label lbBrushSize;
	}
}
