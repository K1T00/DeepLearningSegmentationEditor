namespace AnnotationTool.App.Forms
{
    partial class TrainedModelsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrainedModelsForm));
            lbTrainedModels = new ListBox();
            btnChooseModel = new Button();
            btnCancel = new Button();
            btnDeleteModel = new Button();
            SuspendLayout();
            // 
            // lbTrainedModels
            // 
            lbTrainedModels.Dock = DockStyle.Top;
            lbTrainedModels.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            lbTrainedModels.FormattingEnabled = true;
            lbTrainedModels.Location = new Point(0, 0);
            lbTrainedModels.Name = "lbTrainedModels";
            lbTrainedModels.Size = new Size(531, 224);
            lbTrainedModels.TabIndex = 0;
            // 
            // btnChooseModel
            // 
            btnChooseModel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnChooseModel.BackColor = Color.PeachPuff;
            btnChooseModel.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnChooseModel.ForeColor = Color.Black;
            btnChooseModel.Location = new Point(12, 230);
            btnChooseModel.Name = "btnChooseModel";
            btnChooseModel.Size = new Size(126, 42);
            btnChooseModel.TabIndex = 1;
            btnChooseModel.Text = "Choose model";
            btnChooseModel.UseVisualStyleBackColor = false;
            btnChooseModel.Click += btnChooseModel_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancel.BackColor = Color.PeachPuff;
            btnCancel.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnCancel.ForeColor = Color.Black;
            btnCancel.Location = new Point(144, 230);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(126, 42);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnDeleteModel
            // 
            btnDeleteModel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDeleteModel.BackColor = Color.PeachPuff;
            btnDeleteModel.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            btnDeleteModel.ForeColor = Color.Black;
            btnDeleteModel.Location = new Point(455, 230);
            btnDeleteModel.Name = "btnDeleteModel";
            btnDeleteModel.Size = new Size(69, 42);
            btnDeleteModel.TabIndex = 3;
            btnDeleteModel.Text = "Delete";
            btnDeleteModel.UseVisualStyleBackColor = false;
            btnDeleteModel.Click += btnDeleteModel_Click;
            // 
            // TrainedModelsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(531, 279);
            Controls.Add(btnDeleteModel);
            Controls.Add(btnCancel);
            Controls.Add(btnChooseModel);
            Controls.Add(lbTrainedModels);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "TrainedModelsForm";
            Text = "Trained Models";
            Load += TrainedModelsForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ListBox lbTrainedModels;
        private Button btnChooseModel;
        private Button btnCancel;
        private Button btnDeleteModel;
    }
}