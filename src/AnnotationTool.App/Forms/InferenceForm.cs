using AnnotationTool.Ai.Inference;
using AnnotationTool.Core.Services;
using System.ComponentModel;

namespace AnnotationTool.App.Forms
{
    public partial class InferenceForm : Form
    {
        private CancellationTokenSource cts;
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

			this.pipeline = pipeline;
			this.projectOptionsService = projectOptionsService;
		}

		private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancel.Enabled = false;
            cts?.Cancel();
            this.Close();
        }

		public async Task StartInferenceAsync(IProjectPresenter projectPresenter, string selectedModelPath)
		{
			progressBar.Style = ProgressBarStyle.Continuous;
			cts = new CancellationTokenSource();
			var progress = new Progress<int>(percent => progressBar.Value = percent);

			try
			{
				await projectPresenter.UpdateTrainingSettingsAsync(projectOptionsService.ExtractMetadataFilePath(selectedModelPath));

				await pipeline.RunAsync(projectPresenter, selectedModelPath, progress, cts.Token);

				btnCancel.Enabled = false;
				cts?.Cancel();
				this.Close();
			}
			catch (OperationCanceledException)
			{
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}
    }
}
