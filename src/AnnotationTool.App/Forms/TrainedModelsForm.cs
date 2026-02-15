using AnnotationTool.Core.Services;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Forms
{
    public partial class TrainedModelsForm : Form
    {
        required public ProjectPaths Paths { get; set; }
        public string? SelectedModelFileName { get; private set; }

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
            if (Paths == null)
            {
                MessageBox.Show(
                    "Project paths are not set.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            if (string.IsNullOrWhiteSpace(Paths.Models) || !Directory.Exists(Paths.Models))
            {
                MessageBox.Show(
                    "Models directory is not configured.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            var models = Directory.GetFiles(Paths.Models, Paths.ModelSub + "*" + Paths.ModelExt)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByDescending(path => File.GetCreationTime(
                    Path.Combine(Paths.Models, path + Paths.ModelExt)))
                .ToList();

            lbTrainedModels.DataSource = models;
        }

        private void btnDeleteModel_Click(object sender, EventArgs e)
        {
            if (lbTrainedModels.SelectedItem is not string modelName)
                return;

            var confirm = MessageBox.Show(
                this,
               "Delete model?",
               "Warning",
               MessageBoxButtons.YesNo,
               MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            var modelFile = Path.Combine(Paths.Models, modelName + Paths.ModelExt);
            var settingsFile = Path.Combine(
                Paths.Models,
                modelName.Replace(Paths.ModelSub, Paths.ModelSettingsSub) + ".json");

            TryDeleteFile(modelFile);
            TryDeleteFile(settingsFile);

            var models = Directory.GetFiles(Paths.Models, Paths.ModelSub + "*" + Paths.ModelExt)
                     .Select(Path.GetFileNameWithoutExtension)
                     .OrderByDescending(File.GetCreationTime!)
                     .ToList();

            lbTrainedModels.DataSource = models;
        }
    }
}
