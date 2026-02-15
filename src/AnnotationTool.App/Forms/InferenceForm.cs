using AnnotationTool.Ai.Inference;
using AnnotationTool.Core.Services;
using System.ComponentModel;

namespace AnnotationTool.App.Forms
{
    public partial class InferenceForm : Form
    {
        private CancellationTokenSource? cts;
        private readonly ISegmentationInferencePipeline pipeline;
        private readonly IProjectOptionsService projectOptionsService;

        public InferenceForm(ISegmentationInferencePipeline pipeline, IProjectOptionsService projectOptionsService)
        {
            // Allows designer to open the form without DI
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                InitializeComponent();
                return;
            }
            InitializeComponent();

            this.pipeline = pipeline!;
            this.projectOptionsService = projectOptionsService!;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancel.Enabled = false;
            this.Close();
        }

        public async Task StartInferenceRun(IProjectPresenter projectPresenter, string selectedModelPath)
        {
            var keepDeviceFromUi = projectPresenter.Project.Settings.TrainModelSettings.Device;

            progressBar.Style = ProgressBarStyle.Continuous;
            cts = new CancellationTokenSource();
            var progress = new Progress<int>(percent => progressBar.Value = percent);

            try
            {
                projectPresenter.UpdateTrainingSettings(projectOptionsService.ExtractMetadataFilePath(selectedModelPath));
                projectPresenter.Project.Settings.TrainModelSettings.Device = keepDeviceFromUi;

                // Run inference on all images
                await Task.Run(() => pipeline.RunInference(projectPresenter, selectedModelPath, progress, cts.Token), cts.Token);

                btnCancel.Enabled = false;
                this.Close();
            }
            catch (OperationCanceledException)
            {
                // User canceled inference
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
