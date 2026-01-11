using AnnotationTool.Ai.Training;
using AnnotationTool.Core.IO;
using AnnotationTool.Core.Logging;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Plottables;
using System.ComponentModel;
using static AnnotationTool.Core.Utils.CoreUtils;
using static AnnotationTool.Ai.Training.TrainingTileGenerator;

namespace AnnotationTool.App.Forms
{
    public partial class TrainingForm : Form, ITrainingLogBridge
    {
        private readonly SegmentationTrainingPipeline pipeline;
        private readonly IProjectOptionsService projectOptionsService;
        private readonly ILogger<TrainingForm> logger;
        private CancellationTokenSource cts;
        private DataLogger? trainLossLogger;
        private DataLogger? validationLossLogger;

        public TrainingForm(SegmentationTrainingPipeline pipeline, IProjectOptionsService projectOptionsService, ILogger<TrainingForm> logger)
        {
            // Allows designer to open the form without DI
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                InitializeComponent();
                return;
            }

            InitializeComponent();
            InitializeDataPlots();

            this.pipeline = pipeline;
            this.projectOptionsService = projectOptionsService;
            this.logger = logger;
        }

        public async Task StartTrainingRun(IProjectPresenter projectPresenter)
        {
            btnClose.Enabled = false;
            btnStopTraining.Enabled = true;

            pBTrainingForms.Style = ProgressBarStyle.Continuous;

            logger.LogInformation("Preprocessing images...");

            cts?.Dispose();
            cts = new CancellationTokenSource();

            var progress = new Progress<int>(percent =>
            {
                if (!IsDisposed && pBTrainingForms.IsHandleCreated)
                    pBTrainingForms.Value = Math.Clamp(percent, pBTrainingForms.Minimum, pBTrainingForms.Maximum);
            });

            try
            {
                // Create folder structure if missing and get paths and clean up any old slices
                projectOptionsService.EnsureAll(projectPresenter.ProjectPath);
                var slicedImgDir = projectOptionsService.GetFolderPath(projectPresenter.ProjectPath, ProjectFolderType.SlicedImages);
                var slicedMaskDir = projectOptionsService.GetFolderPath(projectPresenter.ProjectPath, ProjectFolderType.SlicedMasks);
                PrepareOutputDirectory(slicedImgDir);
                PrepareOutputDirectory(slicedMaskDir);

                // Preprocess
                await Task.Run(() =>
                {
                    GenerateTrainingTiles(
                        projectPresenter,
                        slicedImgDir,
                        slicedMaskDir,
                        progress,
                        cts.Token
                        );
                }, cts.Token);

                logger.LogInformation("Preprocessing complete! Ready to train.");

                cts.Token.ThrowIfCancellationRequested();

                logger.LogInformation("Starting training ...");

                var trainLossProgressReport = new Progress<LossReport>();
                trainLossProgressReport.ProgressChanged += ProcessLossData;

                // Train
                await Task.Run(async () =>
                {
                    await pipeline.RunTraining(projectPresenter, trainLossProgressReport, cts.Token);
                }, cts.Token);

                logger.LogInformation("Training finished!");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Training stopped.");
                if (!IsDisposed && pBTrainingForms.IsHandleCreated)
                    pBTrainingForms.Value = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Training failed.");
                logger.LogInformation(ex.ToString());
            }
            finally
            {
                btnStopTraining.Enabled = false;
                btnClose.Enabled = true;
            }
        }

        private void InitializeDataPlots()
        {
            trainLossLogger = formsPlotTrainLoss.Plot.Add.DataLogger();
            validationLossLogger = formsPlotTrainLoss.Plot.Add.DataLogger();

            var axisL = (LeftAxis)formsPlotTrainLoss.Plot.Axes.Left;
            var axisB = (BottomAxis)formsPlotTrainLoss.Plot.Axes.Bottom;

            trainLossLogger.Axes.YAxis = axisL;
            validationLossLogger.Axes.YAxis = axisL;

            trainLossLogger.Axes.XAxis = axisB;
            validationLossLogger.Axes.XAxis = axisB;

            if (Application.SystemColorMode == SystemColorMode.Dark)
            {
                formsPlotTrainLoss.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Empty);
                formsPlotTrainLoss.Plot.Axes.Color(ScottPlot.Color.FromColor(System.Drawing.Color.White));
            }

            formsPlotTrainLoss.Plot.XLabel("Epochs");
            formsPlotTrainLoss.Plot.YLabel("Loss");

            trainLossLogger.LegendText = "Train";
            validationLossLogger.LegendText = "Validation";

            formsPlotTrainLoss.Plot.Legend.Alignment = Alignment.UpperRight;

            trainLossLogger.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Blue);
            validationLossLogger.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Brown);

            trainLossLogger.LineWidth = 2;
            validationLossLogger.LineWidth = 2;

            trainLossLogger.ManageAxisLimits = true;
            validationLossLogger.ManageAxisLimits = false;
        }

        // UI-safe forwarding
        private void ProcessLossData(object? sender, LossReport e)
        {
            if (IsDisposed || Disposing)
                return;

            if (tbLogTrainForm == null || tbLogTrainForm.IsDisposed)
                return;

            this.formsPlotTrainLoss.BeginInvoke(() =>
            {
                trainLossLogger?.Add(e.Epoch, e.TrainLoss > 1 ? 1 : e.TrainLoss);
                validationLossLogger?.Add(e.Epoch, e.ValidationLoss > 1 ? 1 : e.ValidationLoss);

                formsPlotTrainLoss.Plot.Axes.AutoScale();
                formsPlotTrainLoss.Refresh();
            });
        }

        // UI-safe forwarding (called by TrainingFormLoggerProvider)
        public void Append(string message)
        {
            if (IsDisposed || Disposing)
                return;

            if (tbLogTrainForm == null || tbLogTrainForm.IsDisposed)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(Append), message);
                }
                catch (ObjectDisposedException)
                {
                    // Form is closing, ignore
                }
                return;
            }

            tbLogTrainForm.AppendText(message + Environment.NewLine);
        }

        private void btnStopTraining_Click(object sender, EventArgs e)
        {
            tbLogTrainForm.AppendText(Environment.NewLine + "Stopping, please wait ...");

            btnStopTraining.Enabled = false;
            cts?.Cancel();
            btnClose.Enabled = true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            this.Close();
        }
    }
}
