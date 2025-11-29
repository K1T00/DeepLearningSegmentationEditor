namespace AnnotationTool.App.Forms
{
	partial class FeaturesEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeaturesEditor));
            btnAdd = new Button();
            btnRemove = new Button();
            btnRename = new Button();
            btnChangeColor = new Button();
            btnOk = new Button();
            lbTrainFeatures = new ListBox();
            SuspendLayout();
            // 
            // btnAdd
            // 
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.BackColor = Color.LightGray;
            btnAdd.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnAdd.ForeColor = Color.Black;
            btnAdd.Location = new Point(198, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(82, 34);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = false;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnRemove
            // 
            btnRemove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemove.BackColor = Color.LightGray;
            btnRemove.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnRemove.ForeColor = Color.Black;
            btnRemove.Location = new Point(198, 52);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(82, 34);
            btnRemove.TabIndex = 1;
            btnRemove.Text = "Remove";
            btnRemove.UseVisualStyleBackColor = false;
            btnRemove.Click += btnRemove_Click;
            // 
            // btnRename
            // 
            btnRename.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRename.BackColor = Color.LightGray;
            btnRename.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnRename.ForeColor = Color.Black;
            btnRename.Location = new Point(198, 92);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(82, 34);
            btnRename.TabIndex = 2;
            btnRename.Text = "Rename";
            btnRename.UseVisualStyleBackColor = false;
            btnRename.Click += btnRename_Click;
            // 
            // btnChangeColor
            // 
            btnChangeColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChangeColor.BackColor = Color.LightGray;
            btnChangeColor.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnChangeColor.ForeColor = Color.Black;
            btnChangeColor.Location = new Point(198, 132);
            btnChangeColor.Name = "btnChangeColor";
            btnChangeColor.Size = new Size(82, 34);
            btnChangeColor.TabIndex = 3;
            btnChangeColor.Text = "Color";
            btnChangeColor.UseVisualStyleBackColor = false;
            btnChangeColor.Click += btnChangeColor_Click;
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.BackColor = Color.LightGray;
            btnOk.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnOk.ForeColor = Color.Black;
            btnOk.Location = new Point(199, 252);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(82, 34);
            btnOk.TabIndex = 4;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += btnOk_Click;
            // 
            // lbTrainFeatures
            // 
            lbTrainFeatures.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lbTrainFeatures.DrawMode = DrawMode.OwnerDrawFixed;
            lbTrainFeatures.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbTrainFeatures.FormattingEnabled = true;
            lbTrainFeatures.ItemHeight = 30;
            lbTrainFeatures.Location = new Point(12, 12);
            lbTrainFeatures.Name = "lbTrainFeatures";
            lbTrainFeatures.Size = new Size(167, 274);
            lbTrainFeatures.TabIndex = 6;
            lbTrainFeatures.DrawItem += lBTrainFeatures_DrawItem;
            // 
            // FeaturesEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(293, 296);
            Controls.Add(lbTrainFeatures);
            Controls.Add(btnOk);
            Controls.Add(btnChangeColor);
            Controls.Add(btnRename);
            Controls.Add(btnRemove);
            Controls.Add(btnAdd);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FeaturesEditor";
            Text = "Edit Features";
            ResumeLayout(false);
        }

        #endregion

        private Button btnAdd;
		private Button btnRemove;
		private Button btnRename;
		private Button btnChangeColor;
		private Button btnOk;
		private ListBox lbTrainFeatures;
	}
}