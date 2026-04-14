using AnnotationTool.App.Controls;
using AnnotationTool.App.Forms;
using AnnotationTool.App.Rendering;
using AnnotationTool.Core.Interaction;
using AnnotationTool.Core.Logging;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Devices;
using System.Drawing.Drawing2D;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static AnnotationTool.Core.Utils.CoreUtils;
using static TorchSharp.torch.cuda;


namespace AnnotationTool.App
{
    public partial class MainForm : Form
    {
        // ===== Dependencies =====

        // Whole project state
        private readonly IProjectPresenter projectPresenter;

        private readonly IImageRuntimeLoader imageRuntimeLoader;
        private readonly ILoggerFactory loggerFactory;
        private readonly Func<TrainingForm> trainingFormFactory;
        private readonly Func<InferenceForm> inferenceFormFactory;
        private readonly Func<TrainedModelsForm> trainedModelsFormFactory;

        // ===== Runtime service/cache =====
        private readonly ImageRepository imagesRepo = new();

        // ===== Rendering + interaction =====
        private Viewport viewport = new Viewport(1f, PointF.Empty);
        private readonly InteractionModeController interactionModeController = new InteractionModeController();
        private readonly BrushController brushController = new BrushController();
        private readonly ViewportController viewportController = new ViewportController();
        private RoiController? roiController;

        // ===== UI state =====
        private Guid currentSelectedImageGuid = Guid.Empty;
        private Feature? currentSelectedFeature;
        private string currentSelectedModelFileName = "";
        private int currentHeatmapThreshold = 50;
        private int currentBrushSize;
        private BrushMode lastClickedBrushMode = BrushMode.None;
        private PipelineLoopState currentPipelineLoopState = PipelineLoopState.Annotation;
        private Dictionary<int, Color> currentFeatureColorMap = new Dictionary<int, Color>();
        private List<Feature> currentFeatures = [];

        readonly long cpuMemoryBudgetBytes;
        readonly long gpuMemoryBudgetBytes;
        private const byte overlayAlpha = 128; // 0-255 Annotation overlay alpha

        public MainForm(
            IProjectPresenter projectPresenter,
            IImageRuntimeLoader imageRuntimeLoader,
            ILoggerFactory loggerFactory,
            Func<TrainingForm> trainingFormFactory,
            Func<InferenceForm> inferenceFormFactory,
            Func<TrainedModelsForm> trainedModelsFormFactory)
        {
            InitializeComponent();

            this.projectPresenter = projectPresenter!;
            this.imageRuntimeLoader = imageRuntimeLoader!;
            this.trainingFormFactory = trainingFormFactory!;
            this.inferenceFormFactory = inferenceFormFactory!;
            this.loggerFactory = loggerFactory!;
            this.trainedModelsFormFactory = trainedModelsFormFactory!;

            // Bind runtime repository so dirty edits are persisted on repository eviction
            if (this.projectPresenter is ProjectPresenter pp)
                pp.BindRepository(this.imagesRepo);

            this.imagesControl.ImageSelected += ImageGridControl_ImageSelected;
            this.imagesControl.ImageAdded += ImageGridControl_ImageAdded;
            this.mainPictureBox.MouseWheel += DisplayPictureBox_MouseWheel;
            this.annotationToolsControl.BrushSizeChanged += BrushSize_Changed;
            this.annotationToolsControl.ModeRequested += AnnotationButton_Clicked;
            this.projectPresenter.ProjectLoaded += Presenter_ProjectLoaded;
            this.projectPresenter.ErrorOccured += Presenter_ErrorOccured;
            this.featuresControl.FeatureSelected += FeaturesControl_FeatureSelected;

            this.deepLearningSettingsControl.Initialize(this.projectPresenter);
            this.deepLearningSettingsControl.SettingsChanged += DeepLearningSettingsControl_SettingsChanged;
            this.deepLearningSettingsControl.RefreshBindings();

            this.interactionModeController.SetMode(InteractionMode.Pan); // Default to Pan

            UpdateButtonsPipeLineLoopState();

            // PC RAM
            this.cpuMemoryBudgetBytes = (long)new ComputerInfo().TotalPhysicalMemory;
            this.lblSystemRam.Text = $"{Math.Round(this.cpuMemoryBudgetBytes / (1024.0 * 1024.0 * 1024.0), 0)} GB RAM";

            //var libtorch = torch.__version__;
            //var cudnn = cuda.is_cudnn_available();
            if (is_available())
            {
                // GPU VRAM
                this.gpuMemoryBudgetBytes = GetCudaVRam();
                this.lblSystemVram.Text = $"{Math.Round(this.gpuMemoryBudgetBytes / (1024.0 * 1024.0 * 1024.0), 0)} GB VRAM";
            }
            else
            {
                this.lblSystemVram.Text = "No cuda";
                this.deepLearningSettingsControl.ForceCpuOnly = true;
            }
            this.lblThreshold.Text = "50";
        }

        #region Methods

        private void FeaturesControl_FeatureSelected(object? sender, Feature feature)
        {
            currentSelectedFeature = feature;

            if (currentSelectedImageGuid != Guid.Empty)
                imagesControl.SelectImage(currentSelectedImageGuid);

            mainPictureBox.Invalidate();
        }

        private void Presenter_ErrorOccured(object? sender, string e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => MessageBox.Show(e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                return;
            }
            MessageBox.Show(e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void Presenter_ProjectLoaded(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Presenter_ProjectLoaded(sender, e)));
                return;
            }

            // Clear runtime caches and per-image volatile data
            currentSelectedImageGuid = Guid.Empty;
            mainPictureBox.Invalidate();

            // Clear thumbnails (ImageGrid owns and disposes them)
            imagesControl.ClearGrid();

            // Load features
            currentFeatures = projectPresenter.Project.Features
                .Select(fd => new Feature { ClassId = fd.ClassId, Name = fd.Name, Argb = fd.Argb })
                .ToList();
            featuresControl.UpdateFeatures(currentFeatures);

            currentFeatureColorMap = currentFeatures
                .Where(f => f.ClassId != 0)
                .GroupBy(f => f.ClassId)
                .ToDictionary(g => g.Key, g => Color.FromArgb(g.First().Argb));

            // Fill image grid
            if (projectPresenter.Project.Images != null)
            {
                foreach (var it in projectPresenter.Project.Images)
                {
                    var imgPath = projectPresenter.ResolveImagePath(it.Guid);
                    var thumb = await imageRuntimeLoader.CreateThumbnailAsync(imgPath, imagesControl.Width);
                    imagesControl.AddImage(it.Guid, thumb);
                    imagesControl.UpdateCategory(it.Guid, it.Split);
                }
            }
            deepLearningSettingsControl.RefreshBindings();

            if (imagesControl.GetImageIds().Count > 0)
            {
                btnToggleRoi.Visible = true;
                btnToggleRoi.Enabled = true;

                if (currentFeatures.Count > 0)
                {
                    annotationToolsControl.Visible = true;
                    annotationToolsControl.Enabled = true;
                }
            }
        }

        private void DeepLearningSettingsControl_SettingsChanged(object? sender, EventArgs e)
        {
            // Reset name so user can run same model again
            currentSelectedModelFileName = "";
            mainPictureBox.Invalidate();
        }

        private void ImageGridControl_ImageAdded(object? sender, Guid e)
        {
            if (e == Guid.Empty)
                return;

            // Auto-select the first added image if nothing is selected yet
            if (currentSelectedImageGuid == Guid.Empty)
                ImageGridControl_ImageSelected(this, e);
        }

        private void mainDisplayPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!imagesRepo.TryGetRuntime(currentSelectedImageGuid, out var rt))
                return;

            if (!rt.HasFullImage)
                return;

            var g = e.Graphics;
            g.Clear(Color.Black);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            // Draw (original) full size image
            OverlayRenderer.DrawImage(g, rt.FullImage, viewport);

            // Draw the semitransparent annotation or heatmap overlay based on current pipeline state
            switch (currentPipelineLoopState)
            {
                case PipelineLoopState.Annotation:
                    rt.MutateAnnotation(bmp => OverlayRenderer.DrawAnnotation(g, bmp, viewport));
                    break;

                case PipelineLoopState.InferenceResults:
                    rt.MutateHeatmap(bmp => OverlayRenderer.DrawHeatmap(g, bmp, viewport));
                    break;
            }

            if (roiController == null)
                return;

            // Draw the ROI if enabled
            if (interactionModeController.ActiveMode == InteractionMode.Roi)
            {
                OverlayRenderer.DrawRoi(g, roiController, viewport);
            }

            // Draw brush size indicator
            if (lastClickedBrushMode == BrushMode.MouseDown)
            {
                OverlayRenderer.DrawBrushIndicator(g, roiController, viewport, mainPictureBox, currentBrushSize);
            }

            // Draw slice size rectangle
            OverlayRenderer.DrawSliceSizeRectangle(
                g,
                roiController,
                viewport,
                projectPresenter.Project.Settings.PreprocessingSettings.SliceSize,
                projectPresenter.Project.Settings.PreprocessingSettings.DownSample);
        }

        private void BrushSize_Changed(object? sender, int e)
        {
            currentBrushSize = e;
            lastClickedBrushMode = annotationToolsControl.LastClickedBrushMode;
            mainPictureBox.Invalidate();
        }

        private void AnnotationButton_Clicked(object? sender, InteractionMode requestedMode)
        {
            var current = interactionModeController.ActiveMode;

            // Toggle behavior: clicking active tool returns to Pan
            if (current == requestedMode)
            {
                interactionModeController.SetMode(InteractionMode.Pan);
            }
            else
            {
                interactionModeController.SetMode(requestedMode);
            }

            UpdateInteractionUi();
        }

        private async void ImageGridControl_ImageSelected(object? sender, Guid imageGuid)
        {
            if (imageGuid == Guid.Empty)
                return;

            if (!projectPresenter.TryGetImageItem(imageGuid, out var item) || item == null)
                return;

            currentSelectedImageGuid = imageGuid;

            // If user selects image first time it is added to fifo cache, otherwise get from cache
            var rt = await imageRuntimeLoader.EnsureFullImageLoadedAsync(item, imagesRepo, projectPresenter);

            // If project was saved we have to load annotation, masks or heatmaps one time from disk into runtime cache
            if (!string.IsNullOrEmpty(projectPresenter.ProjectPath))
            {
                switch (currentPipelineLoopState)
                {
                    case PipelineLoopState.Annotation:
                        if (!rt.AnnotationLoadedOnce || !rt.MaskLoadedOnce)
                        {
                            await imageRuntimeLoader.EnsureAnnotationAndMaskLoadedAsync(item, imagesRepo, projectPresenter);
                        }
                        break;

                    case PipelineLoopState.InferenceResults:

                        if (currentSelectedFeature != null)
                        {
                            await imageRuntimeLoader.EnsureHeatmapLoadedAsync(item, imagesRepo, projectPresenter, currentSelectedFeature.Name, tBThreshold.Value);

                            // Update inference results control for current image if they exist
                            var segRes = item.SegmentationStats.Values.ToList();
                            var (macro, micro) = AggregateResults(segRes);

                            //Check if any results exist
                            if (!IsAllZero(macro))
                                inferenceResultsControlCurrentImage.SegmentationStats = macro;
                        }
                        break;
                    default:
                        break;
                }
            }

            // Initialize ROI controller
            roiController = new RoiController(item.Roi);

            // Reset viewport and show image at 100% zoom, centered
            viewport = new Viewport();
            viewport.FitToView(rt.FullImage.Size, mainPictureBox.ClientSize);
            mainPictureBox.Invalidate();
        }

        private void UpdateButtonsPipeLineLoopState()
        {
            btnAnnotate.BackColor =
                currentPipelineLoopState == PipelineLoopState.Annotation ? Color.Coral : Color.PeachPuff;

            btnTrain.BackColor =
                currentPipelineLoopState == PipelineLoopState.Training ? Color.Coral : Color.PeachPuff;

            btnTrainingResults.BackColor =
                currentPipelineLoopState == PipelineLoopState.InferenceResults ? Color.Coral : Color.PeachPuff;
        }

        private void tBThreshold_ValueChanged(object sender, EventArgs e)
        {
            lblThreshold.Text = tBThreshold.Value.ToString();

            if (projectPresenter.Project == null)
                return;
            if (currentPipelineLoopState != PipelineLoopState.InferenceResults)
                return;
            if (currentSelectedImageGuid == Guid.Empty || currentSelectedFeature == null)
                return;
            if (!projectPresenter.TryGetImageItem(currentSelectedImageGuid, out var item) || item == null)
                return;

            projectPresenter.Project.Settings.HeatmapThreshold = MapRangeStringToInt(lblThreshold.Text, 0, 100, 0, 255);

            var rt = imagesRepo.GetRuntime(item);
            if (!rt.HasHeatmapCertainty(currentSelectedFeature.Name))
                return;

            int thrByte = (int)Math.Round(tBThreshold.Value * 255.0 / 100.0);
            rt.SetHeatmapThreshold(thrByte);
            rt.RegenerateHeatmapTurbo(currentSelectedFeature.Name);

            mainPictureBox.Invalidate();
        }

        private Task UnloadCurrentProjectAsync()
        {
            // Clear UI thumbnails
            if (InvokeRequired)
                BeginInvoke(new Action(() => imagesControl.ClearGrid()));
            else
                imagesControl.ClearGrid();

            // Clear runtime caches
            imagesRepo.Clear();
            currentSelectedImageGuid = Guid.Empty;

            // Clear UI elems
            inferenceResultsControlAllImages.ClearPlot();
            inferenceResultsControlCurrentImage.ClearPlot();

            // GC.Collect();
            // GC.WaitForPendingFinalizers();

            return Task.CompletedTask;
        }

        // Centralized UI update for interaction mode changes
        private void UpdateInteractionUi()
        {
            var mode = interactionModeController.ActiveMode;

            // Annotation tools enabled if not in roi mode
            bool annotationEnabled = mode != InteractionMode.Roi;

            annotationToolsControl.Enabled = annotationEnabled;
            annotationToolsControl.SetActiveMode(mode);

            // ROI button visual state
            btnToggleRoi.BackgroundImage =
                mode == InteractionMode.Roi
                    ? Properties.Resources.ToggleRoiClicked
                    : Properties.Resources.ToggleRoi;


            //switch (mode)
            //{
            //    case InteractionMode.Paint:
            //        mainDisplayPictureBox.Cursor = Cursors.Cross;
            //        break;

            //    case InteractionMode.Erase:
            //        mainDisplayPictureBox.Cursor = Cursors.No;
            //        break;

            //    case InteractionMode.Roi:
            //        mainDisplayPictureBox.Cursor = Cursors.SizeAll;
            //        break;

            //    case InteractionMode.Pan:
            //        mainDisplayPictureBox.Cursor = Cursors.Hand;
            //        break;

            //    default:
            //        mainDisplayPictureBox.Cursor = Cursors.Default;
            //        break;
            //}
        }

        #endregion

        #region Buttons

        private async void btnAddImages_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                using var ofd = new OpenFileDialog();
                ofd.Title = "Add images";
                ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff";
                ofd.Multiselect = true;
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                int added = 0, failed = 0;
                var beforeCount = projectPresenter.Project.Images.Count;

                foreach (var file in ofd.FileNames)
                {

                    if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                    {
                        failed++;
                        continue;
                    }

                    var imageSize = await imageRuntimeLoader.ReadImageSizeAsync(file);
                    var newItem = projectPresenter.AddImage(file, imageSize);

                    if (projectPresenter.Project.Images.Count > beforeCount)
                        added++;
                    if (newItem == null)
                        continue;

                    var thumb = await imageRuntimeLoader.CreateThumbnailAsync(file, imagesControl.Width);

                    imagesControl.AddImage(newItem.Guid, thumb);
                    imagesControl.UpdateCategory(newItem.Guid, newItem.Split);
                }

                // Force reset so user can run same model again
                currentSelectedModelFileName = "";

                imagesControl.Refresh();

                if (imagesControl.GetImageIds().Count > 0)
                {
                    btnToggleRoi.Visible = true;
                    btnToggleRoi.Enabled = true;

                    if (currentFeatures.Count > 0)
                    {
                        annotationToolsControl.Visible = true;
                        annotationToolsControl.Enabled = true;
                    }
                }
                MessageBox.Show($"{added} images added.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnDeleteImages_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                var ids = imagesControl.GetSelectedImageIds();

                if (ids.Count == 0)
                {
                    MessageBox.Show(this, "Select one or more images to delete.", "Nothing selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(this,
                    $"Remove {ids.Count} image(s) from the project?\n\n" +
                    "This removes them from the project and clears cached bitmaps/masks.\n",
                    "Confirm delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (confirm != DialogResult.Yes) return;

                foreach (var id in ids)
                {
                    if (currentSelectedImageGuid == id)
                        currentSelectedImageGuid = Guid.Empty;

                    await Task.Run(() => { projectPresenter.RemoveImage(id); });
                    imagesRepo.Remove(id);
                }

                if (!string.IsNullOrEmpty(projectPresenter.ProjectPath))
                {
                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                }

                mainPictureBox.Invalidate();
                imagesControl.RemoveSelectedImages();

                if (imagesControl.GetImageIds().Count == 0)
                {
                    annotationToolsControl.Visible = false;
                    annotationToolsControl.Enabled = false;
                    btnToggleRoi.Visible = false;
                    btnToggleRoi.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnEditFeatures_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                var oldFeaturesSnapshot = currentFeatures
                    .Select(f => new Feature
                    {
                        Name = f.Name,
                        ClassId = f.ClassId,
                        Argb = f.Argb
                    }).ToList();

                using var editorForm = new FeaturesEditor(currentFeatures);

                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    currentFeatures = editorForm.GetUpdatedFeatures();
                    featuresControl.UpdateFeatures(currentFeatures);
                    projectPresenter.Project.Features = currentFeatures;

                    var classesRemap = BuildClassRemapByName(oldFeaturesSnapshot, currentFeatures);

                    currentFeatureColorMap = currentFeatures
                        .Where(f => f.ClassId != 0)
                        .GroupBy(f => f.ClassId)
                        .ToDictionary(g => g.Key, g => Color.FromArgb(g.First().Argb));
                }
                if (currentFeatures.Count > 0 && imagesControl.GetImageIds().Count > 0)
                {
                    annotationToolsControl.Visible = true;
                    annotationToolsControl.Enabled = true;
                }
                else
                {
                    annotationToolsControl.Visible = false;
                    annotationToolsControl.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing features: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnToggleRoi_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (interactionModeController.ActiveMode == InteractionMode.Roi)
                {
                    interactionModeController.SetMode(InteractionMode.Pan);
                }
                else
                {
                    interactionModeController.SetMode(InteractionMode.Roi);
                }
                UpdateInteractionUi();
                mainPictureBox.Invalidate();
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnNewProject_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                using var sfd = new SaveFileDialog();
                sfd.Filter = "Project JSON|*.json|All files|*.*";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    if (string.IsNullOrWhiteSpace(sfd.FileName))
                        return;

                    tbProjectPath.Text = sfd.FileName;

                    await UnloadCurrentProjectAsync();

                    await Task.Run(() => { projectPresenter.NewProject(sfd.FileName); });
                    await Task.Run(() => { projectPresenter.LoadProject(sfd.FileName); });

                    currentPipelineLoopState = PipelineLoopState.Annotation;
                    UpdateButtonsPipeLineLoopState();

                    imagesControl.Refresh();
                    mainPictureBox.Invalidate();

                    MessageBox.Show($"Project created successfully.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    btnToggleRoi.Visible = false;
                    btnToggleRoi.Enabled = false;
                    annotationToolsControl.Visible = false;
                    annotationToolsControl.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create project: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnSaveProject_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(projectPresenter.ProjectPath))
                {
                    using var sfd = new SaveFileDialog();
                    sfd.Filter = "Project JSON|*.json|All files|*.*";
                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    tbProjectPath.Text = sfd.FileName;
                    projectPresenter.ProjectPath = Path.GetDirectoryName(sfd.FileName);
                    projectPresenter.Project.Name = Path.GetFileNameWithoutExtension(sfd.FileName);
                }

                try
                {
                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                    MessageBox.Show("Project saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Save canceled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnSaveAs_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                using var sfd = new SaveFileDialog();
                sfd.Filter = "Project JSON|*.json|All files|*.*";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    tbProjectPath.Text = sfd.FileName;
                    projectPresenter.ProjectPath = Path.GetDirectoryName(sfd.FileName);
                    projectPresenter.Project.Name = Path.GetFileNameWithoutExtension(sfd.FileName);

                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                    MessageBox.Show("Project saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Save canceled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnLoadProject_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                using var ofd = new OpenFileDialog();
                ofd.Title = "Load Project File";
                ofd.Filter = "Project JSON|*.json|All files|*.*";
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                await UnloadCurrentProjectAsync();

                tbProjectPath.Text = ofd.FileName;
                projectPresenter.Project.Name = ofd.FileName;
                projectPresenter.ProjectPath = Path.GetDirectoryName(ofd.FileName);

                // SHOW progress form
                var progress = new ProgressForm
                {
                    Text = "Loading project..."
                };
                progress.Show(this);
                progress.Refresh(); // force immediate draw

                await Task.Run(() => { projectPresenter.LoadProject(ofd.FileName); });

                progress.Close();
                progress = null;

                var allFeatureStats = projectPresenter.Project.Images
                    .SelectMany(img => img.SegmentationStats?.Values ?? Enumerable.Empty<SegmentationStats>())
                    .ToList();

                var (macro, micro) = AggregateResults(allFeatureStats);

                // Check if any results exist
                if (!IsAllZero(macro))
                    inferenceResultsControlAllImages.SegmentationStats = macro;

                currentPipelineLoopState = PipelineLoopState.Annotation;
                UpdateButtonsPipeLineLoopState();

                // Select first image
                var ids = imagesControl.GetImageIds();
                if (ids.Count > 0)
                    imagesControl.SelectImage(ids[0]);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                var result = MessageBox.Show("Are you sure you want to exit?", "Exit Confirmation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;

                imagesRepo?.Dispose();
                Application.Exit();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exiting application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnAnnotate_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                currentPipelineLoopState = PipelineLoopState.Annotation;
                UpdateButtonsPipeLineLoopState();

                // Select first image
                var ids = imagesControl.GetImageIds();
                if (ids.Count > 0)
                    imagesControl.SelectImage(ids[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error training images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                currentPipelineLoopState = PipelineLoopState.Annotation;
                UpdateButtonsPipeLineLoopState();

                (sender as Button)!.Enabled = true;
            }
        }

        private void btnUpdateCategoryToTrain_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (!imagesControl.HasSelectedImages)
                {
                    MessageBox.Show("No images are selected to categorize.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (var id in imagesControl.GetSelectedImageIds())
                {
                    if (!projectPresenter.TryGetImageItem(id, out var it))
                        return;

                    it.Split = DatasetSplit.Train;
                    imagesControl.UpdateCategory(id, it.Split);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting category to Train: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnUpdateCategoryToValidate_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (!imagesControl.HasSelectedImages)
                {
                    MessageBox.Show("No images are selected to categorize.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (var id in imagesControl.GetSelectedImageIds())
                {
                    if (!projectPresenter.TryGetImageItem(id, out var it))
                        return;

                    it.Split = DatasetSplit.Validate;
                    imagesControl.UpdateCategory(id, it.Split);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting category to Validate: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private void btnUpdateCategoryToTest_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (!imagesControl.HasSelectedImages)
                {
                    MessageBox.Show("No images are selected to categorize.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                foreach (var id in imagesControl.GetSelectedImageIds())
                {
                    if (!projectPresenter.TryGetImageItem(id, out var it))
                        return;

                    it.Split = DatasetSplit.Test;
                    imagesControl.UpdateCategory(id, it.Split);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting category to Test: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnTrain_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (projectPresenter.ProjectPath == null)
                {
                    MessageBox.Show("Please save project first.");
                    return;
                }
                else
                {
                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                }

                var trainCount = projectPresenter.Project.Images.Count(i => i.Split == DatasetSplit.Train);
                var validateCount = projectPresenter.Project.Images.Count(i => i.Split == DatasetSplit.Validate);

                if (trainCount == 0 || validateCount == 0)
                {
                    MessageBox.Show("At least one train image and one validate image needed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Hide MainForms overlay
                ShowOverlay();

                var trainingForm = trainingFormFactory();

                // Register logger provider for TrainingForm - logs from training pipeline will be sent to the TrainingForm
                var loggerProvider = new TrainingFormLoggerProvider(trainingForm);
                loggerFactory.AddProvider(loggerProvider);

                trainingForm.FormClosed += (s, args) =>
                {
                    loggerProvider.Dispose();  // unregister -> Every new TrainingForm/training session will create a new provider
                    HideOverlay();
                };
                trainingForm.Show(this);

                await trainingForm.StartTrainingRun(projectPresenter, cpuMemoryBudgetBytes, gpuMemoryBudgetBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error training images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                currentPipelineLoopState = PipelineLoopState.Training;
                UpdateButtonsPipeLineLoopState();
                (sender as Button)!.Enabled = true;
            }
        }

        private async void btnTrainingResults_Click(object sender, EventArgs e)
        {
            (sender as Button)!.Enabled = false;

            try
            {
                if (projectPresenter.ProjectPath == null)
                {
                    MessageBox.Show("Please save project first.");
                    return;
                }
                else
                {
                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                }

                var paths = projectPresenter.Paths;

                var trainedModelsForm = trainedModelsFormFactory();
                trainedModelsForm.Paths = paths;

                // If user cancels model selection, switch back to annotation mode
                if (trainedModelsForm.ShowDialog(this) != DialogResult.OK)
                {
                    currentPipelineLoopState = PipelineLoopState.Annotation;
                    UpdateButtonsPipeLineLoopState();
                    return;
                }

                currentPipelineLoopState = PipelineLoopState.InferenceResults;
                projectPresenter.Project.Settings.HeatmapThreshold = MapRangeStringToInt(lblThreshold.Text, 0, 100, 0, 255);

                if (trainedModelsForm.SelectedModelFileName is not string modelFileName)
                    return;

                var modelPath = Path.ChangeExtension(Path.Combine(paths.Models, modelFileName), paths.ModelExt);

                // Selected the same model as last time - just show heatmaps without re-running inference
                if (modelFileName != currentSelectedModelFileName || projectPresenter.Project.Settings.HeatmapThreshold != currentHeatmapThreshold)
                {
                    currentSelectedModelFileName = modelFileName;
                    currentHeatmapThreshold = projectPresenter.Project.Settings.HeatmapThreshold;

                    // Hide MainForms overlay
                    ShowOverlay();

                    TryDeleteDirectoryContents(paths.MasksHeatmaps, out var errOverlays);

                    var inferenceForm = inferenceFormFactory();

                    inferenceForm.FormClosed += (s, args) => HideOverlay();
                    inferenceForm.Show(this);

                    await inferenceForm.StartInferenceRun(projectPresenter, modelPath);
                }

                // Show statistic results //

                var (macro, micro) = AggregateResults(projectPresenter.Project.Images.SelectMany(img => img.SegmentationStats.Values).ToList());

                if (!IsAllZero(macro))
                    inferenceResultsControlAllImages.SegmentationStats = macro;

                if (projectPresenter.Project.Images.Count > 0)
                {
                    // Skip the first image as it often contains outliers (e.g. long inference time due to one-time setup overhead) that skew the average compute time
                    var averageInferenceMs = projectPresenter.Project.Images.Skip(1).Average(img => img.InferenceMs);

                    lblInferenceMeanComputeTime.Text =
                        $"Average compute time: {Math.Round(averageInferenceMs, 0):F1} ms";
                }

                // Select first feature
                if (currentFeatures.Count > 0)
                {
                    featuresControl.SelectFeature(currentFeatures[0]);
                }

                // Select first image
                var ids = imagesControl.GetImageIds();
                if (ids.Count > 0)
                    imagesControl.SelectImage(ids[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error training images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                currentPipelineLoopState = PipelineLoopState.InferenceResults;
                UpdateButtonsPipeLineLoopState();

                (sender as Button)!.Enabled = true;
            }
        }

        #endregion

        #region Mouse events on Main PictureBox interactions

        private void mainDisplayPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (!imagesRepo.TryGetRuntime(currentSelectedImageGuid, out var rt))
                return;

            switch (interactionModeController.ActiveMode)
            {
                case InteractionMode.Paint:
                    {
                        if (currentSelectedFeature == null)
                        {
                            MessageBox.Show("Please select a feature to paint.");
                            return;
                        }

                        brushController.BeginStroke(e.Location, viewport, annotationToolsControl.BrushSize, (byte)currentSelectedFeature.ClassId);


                        if (brushController.UpdateStroke(e.Location, viewport, rt.Mask, out var dirtyImageRect))
                        {
                            rt.MutateAnnotation(bmp =>
                            {
                                OverlayRenderer.UpdateAnnotationOverlayRegion(
                                    bmp,
                                    rt.Mask,
                                    currentFeatureColorMap,
                                    dirtyImageRect,
                                    overlayAlpha);
                            });


                            rt.MutateMask(mask =>
                            {
                                var changed = brushController.UpdateStroke(e.Location, viewport, mask, out dirtyImageRect);
                            });



                            var dirtyScreenRect = viewport.ImageToScreenRect(dirtyImageRect);
                            mainPictureBox.Invalidate(new Region(dirtyScreenRect));
                        }
                        break;
                    }
                case InteractionMode.Erase:
                    {
                        brushController.BeginStroke(e.Location, viewport, annotationToolsControl.BrushSize, (byte)0); // background

                        if (brushController.UpdateStroke(e.Location, viewport, rt.Mask, out var dirtyImageRect))
                        {
                            rt.MutateAnnotation(bmp =>
                            {
                                OverlayRenderer.UpdateAnnotationOverlayRegion(
                                    bmp,
                                    rt.Mask,
                                    currentFeatureColorMap,
                                    dirtyImageRect,
                                    overlayAlpha);
                            });

                            rt.MutateMask(mask =>
                            {
                                var changed = brushController.UpdateStroke(e.Location, viewport, mask, out dirtyImageRect);
                            });

                            var dirtyScreenRect = viewport.ImageToScreenRect(dirtyImageRect);
                            mainPictureBox.Invalidate(new Region(dirtyScreenRect));
                        }
                        break;
                    }
                case InteractionMode.Roi:
                    {
                        if (roiController == null)
                            return;

                        roiController.MouseDown(e.Location, viewport);

                        // If ROI did not capture interaction, fall back to pan
                        if (roiController.Mode == RoiMode.None)
                        {
                            viewportController.BeginPan(e.Location);
                            interactionModeController.PushTemporaryMode(InteractionMode.Pan);
                        }

                        mainPictureBox.Invalidate();
                        break;
                    }
                case InteractionMode.Pan:
                    {
                        viewportController.BeginPan(e.Location);
                        mainPictureBox.Invalidate();
                        break;
                    }
                default:
                    break;

            }
        }

        private void mainDisplayPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (!imagesRepo.TryGetRuntime(currentSelectedImageGuid, out var rt))
                return;

            switch (interactionModeController.ActiveMode)
            {
                case InteractionMode.Paint:
                case InteractionMode.Erase:
                    {
                        if (rt.Mask == null)
                            return;

                        if (brushController.UpdateStroke(e.Location, viewport, rt.Mask, out var dirtyImageRect))
                        {
                            rt.MutateAnnotation(bmp =>
                            {
                                OverlayRenderer.UpdateAnnotationOverlayRegion(
                                    bmp,
                                    rt.Mask,
                                    currentFeatureColorMap,
                                    dirtyImageRect,
                                    overlayAlpha);
                            });


                            rt.MutateMask(mask =>
                            {
                                var changed = brushController.UpdateStroke(e.Location, viewport, mask, out dirtyImageRect);
                            });

                            var dirtyScreenRect = viewport.ImageToScreenRect(dirtyImageRect);
                            mainPictureBox.Invalidate(new Region(dirtyScreenRect));
                        }
                        break;
                    }
                case InteractionMode.Roi:
                    {
                        if (roiController == null)
                            break;

                        roiController.MouseMove(e.Location, viewport, new SizeF(rt.FullImage.Width, rt.FullImage.Height));

                        mainPictureBox.Invalidate();
                        break;
                    }
                case InteractionMode.Pan:
                    {
                        viewportController.UpdatePan(e.Location, viewport);
                        mainPictureBox.Invalidate();
                        break;
                    }

                default:
                    break;
            }
        }

        private void mainDisplayPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (!imagesRepo.TryGetRuntime(currentSelectedImageGuid, out var rt))
                return;

            // Finalize brush stroke (paint / erase)
            brushController.EndStroke();

            // Finalize ROI interaction
            if (roiController != null)
            {
                roiController.MouseUp();

                if (interactionModeController.ActiveMode == InteractionMode.Roi)
                {
                    if (!projectPresenter.TryGetImageItem(currentSelectedImageGuid, out var it))
                        return;
                    it.Roi = roiController.GetRoundedRoi();
                }
            }

            // Finalize viewport panning
            viewportController.EndPan();

            // Restore base interaction mode
            // (e.g. after temporary Pan override)
            interactionModeController.PopTemporaryMode();

            mainPictureBox.Invalidate();
        }

        private void DisplayPictureBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            viewportController.Zoom(e.Delta, e.Location, viewport);
            mainPictureBox.Invalidate();
        }

        #endregion

        #region Forms Overlay to lock out user interaction during long operations

        private Panel? overlayPanel;

        private void ShowOverlay()
        {
            if (overlayPanel != null)
                return;

            overlayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                Enabled = false
            };

            var spinner = new PulsingSpinner
            {
                AccentColor = Color.DeepSkyBlue, // Change this to match your theme
                Radius = 20,
                DotSize = 8
            };

            var label = new Label
            {
                Text = "In progress...",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true
            };

            overlayPanel.Controls.Add(spinner);
            overlayPanel.Controls.Add(label);

            overlayPanel.Resize += (s, e) =>
            {
                spinner.Left = (overlayPanel.Width - spinner.Width) / 2;
                spinner.Top = (overlayPanel.Height / 2) - 40;

                label.Left = (overlayPanel.Width - label.Width) / 2;
                label.Top = spinner.Bottom + 10;
            };

            Controls.Add(overlayPanel);
            overlayPanel.BringToFront();
        }

        private void HideOverlay()
        {
            overlayPanel?.Dispose();
            overlayPanel = null;
        }

        #endregion

    }
}