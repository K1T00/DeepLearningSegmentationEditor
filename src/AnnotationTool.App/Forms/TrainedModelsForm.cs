using System.Diagnostics;

namespace AnnotationTool.App.Forms
{
    public partial class TrainedModelsForm : Form
    {
		public string ModelsPath { get; set; }
		public string ModelSubFileName { get; set; }
		public string TrainingSettingsSubFileName { get; set; }

		public string SelectedModelFileName { get; private set; }

		public TrainedModelsForm()
        {
			InitializeComponent();
		}

		private void btnChooseModel_Click(object sender, EventArgs e)
        {
			if (lbTrainedModels.SelectedItem is string path)
			{
				this.SelectedModelFileName = path;
				DialogResult = DialogResult.OK;
				Close();
			}
		}

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void TrainedModelsForm_Load(object sender, EventArgs e)
        {
            var models = Directory.GetFiles(ModelsPath, ModelSubFileName + "*.bin")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByDescending(File.GetCreationTime!)
                .ToList();
            lbTrainedModels.DataSource = models;
        }

        private void btnDeleteModel_Click(object sender, EventArgs e)
        {
            if (lbTrainedModels.SelectedItem is string path)
            {
                var confirm = MessageBox.Show(this, "Delete model?", "Warning",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;
            }

			if (lbTrainedModels.SelectedItem == null) return;

			var modelFile = Path.ChangeExtension(Path.Combine(ModelsPath, (string)lbTrainedModels.SelectedItem), ".bin");
			var settingsFile = Path.ChangeExtension(Path.Combine(ModelsPath, (string)lbTrainedModels.SelectedItem).Replace(ModelSubFileName, TrainingSettingsSubFileName), ".json");

			if (File.Exists(modelFile))
			{
				try
				{
					File.Delete(modelFile);
				}
				catch (IOException ex)
				{
					Debug.WriteLine($"Could not delete {modelFile}: {ex.Message}");
				}
			}

			if (File.Exists(settingsFile))
			{
				try
				{
					File.Delete(settingsFile);
				}
				catch (IOException ex)
				{
					Debug.WriteLine($"Could not delete {settingsFile}: {ex.Message}");
				}
			}

			var models = Directory.GetFiles(ModelsPath, ModelSubFileName + "*.bin")
					 .Select(Path.GetFileNameWithoutExtension)
					 .OrderByDescending(File.GetCreationTime!)
					 .ToList();
			lbTrainedModels.DataSource = models;
		}
    }
}
