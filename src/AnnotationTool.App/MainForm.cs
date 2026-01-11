using AnnotationTool.App.Controls;
using AnnotationTool.App.Forms;
using AnnotationTool.App.Rendering;
using AnnotationTool.Core.Interaction;
using AnnotationTool.Core.IO;
using AnnotationTool.Core.Logging;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Devices;
using System.Drawing.Drawing2D;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static AnnotationTool.Core.Services.ProjectStore;
using static AnnotationTool.Core.Utils.CoreUtils;
using static TorchSharp.torch;

namespace AnnotationTool.App
{
    public partial class MainForm : Form
    {
        // Data mapped to GUID 
        private readonly IProjectPresenter projectPresenter;
        // Runtime service/cache
        private readonly ImageRepository imagesRepo = new();

        private readonly Func<TrainingForm> trainingFormFactory;
        private readonly Func<InferenceForm> inferenceFormFactory;
        private readonly Func<TrainedModelsForm> trainedModelsFormFactory;
        private readonly ILoggerFactory loggerFactory;
        private readonly IProjectOptionsService projectOptionsService;
        private readonly InteractionModeController interactionModeController = new InteractionModeController();
        private readonly BrushController brushController = new BrushController();
        private readonly ViewportController viewportController = new ViewportController();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();

        private Viewport viewport = new Viewport(1f, PointF.Empty);
        private RoiController roiController;
        private Guid currentImageGuid;
        private ImageRepository.ImageRuntime currentImageRtItem;

        private Dictionary<int, Color> currentFeatureColorMap = new Dictionary<int, Color>();

        private List<Feature> currentFeatures = [];
        private Feature? currentSelectedFeature;
        private string currentSelectedModelFileName = "";

        private int currentBrushSize;
        private BrushMode lastClickedBrushMode = BrushMode.None;
        private PipelineLoopState currentPipelineLoopState = PipelineLoopState.Annotation;
        private const byte overlayAlpha = 128; // 0-255 Annotation overlay alpha

        public MainForm(
            IProjectPresenter projectPresenter,
            ILoggerFactory loggerFactory,
            IProjectOptionsService projectOptionsService,
            Func<TrainingForm> trainingFormFactory,
            Func<InferenceForm> inferenceFormFactory,
            Func<TrainedModelsForm> trainedModelsFormFactory)
        {
            InitializeComponent();

            this.projectPresenter = projectPresenter!;
            this.trainingFormFactory = trainingFormFactory!;
            this.inferenceFormFactory = inferenceFormFactory!;
            this.loggerFactory = loggerFactory!;
            this.projectOptionsService = projectOptionsService!;
            this.trainedModelsFormFactory = trainedModelsFormFactory!;

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
            this.projectPresenter.Project.CpuMemoryBudgetBytes = (long)new ComputerInfo().TotalPhysicalMemory;
            this.lblSystemRam.Text = $"{Math.Round(this.projectPresenter.Project.CpuMemoryBudgetBytes / (1024.0 * 1024.0 * 1024.0), 0)} GB RAM";

            // GPU VRAM
            if (cuda.is_available())
            {
                this.projectPresenter.Project.GpuMemoryBudgetBytes = GetCudaVRam();
                this.lblSystemVram.Text = $"{Math.Round(this.projectPresenter.Project.GpuMemoryBudgetBytes / (1024.0 * 1024.0 * 1024.0), 0)} GB VRAM";
            }
            else
            {
                this.lblSystemVram.Text = "No cuda";
                this.deepLearningSettingsControl.ForceCpuOnly = true;
            }

            // For now ... ToDo
            //this.deepLearningSettingsControl.ForceModelComplexity = true;
            this.lblThreshold.Text = "50";
        }

        #region Methods

        private void FeaturesControl_FeatureSelected(object? sender, Feature feature)
        {
            this.currentSelectedFeature = feature;

            if (this.projectPresenter.ProjectPath != null)
            {

                var heatmapsImages = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsImages);

                if (heatmapsImages != null)
                {
                    // Load any saved heatmaps by GUID
                    foreach (var it in projectPresenter.Project.Images)
                    {
                        var id = it.Guid;
                        var rt = imagesRepo.GetRuntime(id);

                        // Get feature subfolder based on selected feature
                        if (currentSelectedFeature != null)
                        {
                            var suffixes = it.SegmentationStats.Keys.Select(k => k.ToString()).ToList();
                            var featureSubfolder = FindMatchingFolder(heatmapsImages, currentSelectedFeature.Name, suffixes);

                            if (featureSubfolder != null)
                            {
                                var heatPng = Path.Combine(featureSubfolder, id + ".png");
                                if (File.Exists(heatPng))
                                {
                                    using (var tmp = Image.FromFile(heatPng))
                                    {
                                        imagesRepo.SetHeatmap(id, new Bitmap(tmp));
                                    }
                                }
                            }
                        }
                    }
                    imagesControl.SelectImage(currentImageGuid);
                    mainPictureBox.Invalidate();
                }
            }
        }

        private void Presenter_ErrorOccured(object? sender, string e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => MessageBox.Show(e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                return;
            }
            MessageBox.Show(e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Presenter_ProjectLoaded(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Presenter_ProjectLoaded(sender, e)));
                return;
            }

            // Clear runtime caches and per-image volatile data
            currentImageRtItem = null;
            currentImageGuid = Guid.Empty;
            mainPictureBox.Invalidate();
            imagesControl.ClearGrid();

            // Load features
            currentFeatures = this.projectPresenter.Project.Features
                .Select(fd => new Feature { ClassId = fd.ClassId, Name = fd.Name, Argb = fd.Argb })
                .ToList();
            featuresControl.UpdateFeatures(currentFeatures);

            currentFeatureColorMap = currentFeatures
                .Where(f => f.ClassId != 0)
                .GroupBy(f => f.ClassId)
                .ToDictionary(g => g.Key, g => Color.FromArgb(g.First().Argb));

            if (projectPresenter.Project.Images != null)
            {
                foreach (var it in projectPresenter.Project.Images)
                {
                    // Refresh views
                    imagesControl.AddImage(it.Guid, it.Path);
                    imagesControl.UpdateCategory(it.Guid, it.Split);
                }
            }
            deepLearningSettingsControl.RefreshBindings();
        }

        private void DeepLearningSettingsControl_SettingsChanged(object? sender, EventArgs e)
        {
            // Force reset so user can run same model again
            currentSelectedModelFileName = "";
            mainPictureBox.Invalidate();
        }

        private void ImageGridControl_ImageAdded(object? sender, (Guid id, int Width, int Height) e)
        {
            if (e.id == Guid.Empty)
                return;

            var item = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == e.id);
            if (item == null)
                return;

            item.ImageSize = new System.Drawing.Size(e.Width, e.Height);
            item.Roi = Rectangle.Round(ClampRoi(item.Roi, e.Width, e.Height));

            // Ensure there is a mask for this image (kept by the repo)
            var (mask, annotation) = imagesRepo.GetOrCreateAnnotationMask(e.id, e.Width, e.Height);

            // Auto-select the first added image if nothing is selected yet
            if (currentImageGuid == Guid.Empty)
                ImageGridControl_ImageSelected(this, e.id);
        }

        private void mainDisplayPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (currentImageRtItem == null || viewport == null)
                return;

            var g = e.Graphics;
            g.Clear(Color.Black);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            // Draw (original) full size image
            overlayRenderer.DrawImage(g, currentImageRtItem.FullImage, viewport);

            // Draw the annotation overlay or heatmap overlay based on current pipeline state
            switch (currentPipelineLoopState)
            {
                case PipelineLoopState.Annotation:

                    if (currentImageRtItem?.Annotation != null)
                    {
                        lock (currentImageRtItem.AnnotationLock)
                        {
                            overlayRenderer.DrawImage(g, currentImageRtItem.Annotation, viewport);
                        }
                    }

                    break;

                case PipelineLoopState.InferenceResults:

                    if (currentImageRtItem?.Heatmap != null)
                    {
                        lock (currentImageRtItem.HeatmapLock)
                        {
                            overlayRenderer.DrawImage(g, currentImageRtItem.Heatmap, viewport);
                        }
                    }

                    break;
            }

            // Draw the ROI if enabled
            if (interactionModeController.ActiveMode == InteractionMode.Roi)
            {
                overlayRenderer.DrawRoi(g, roiController, viewport);
            }

            // Draw brush size indicator
            if (lastClickedBrushMode == BrushMode.MouseDown)
            {
                overlayRenderer.DrawBrushIndicator(g, roiController, viewport, mainPictureBox, currentBrushSize);
            }

            // Draw slice size rectangle
            overlayRenderer.DrawSliceSizeRectangle(
                g,
                roiController,
                viewport,
                projectPresenter.Project.Settings.PreprocessingSettings.SliceSize,
                projectPresenter.Project.Settings.PreprocessingSettings.DownSample);
        }

        private void BrushSize_Changed(object? sender, int e)
        {
            this.currentBrushSize = e;
            this.lastClickedBrushMode = annotationToolsControl.LastClickedBrushMode;
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

        private void ImageGridControl_ImageSelected(object? sender, Guid imageGuid)
        {
            if (imageGuid == Guid.Empty) return;

            try
            {
                currentImageGuid = imageGuid;

                var path = GetPath(projectPresenter.Project, imageGuid);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    MessageBox.Show(this, $"Image file not found for {imageGuid}.", "Missing file",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create runtime bucket for current image
                currentImageRtItem = imagesRepo.GetRuntime(imageGuid);

                // Load/cache full-res bitmaps (repo owns lifetime)
                currentImageRtItem.FullImage = imagesRepo.EnsureFull(imageGuid, path);

                // Reset viewport
                this.viewport = new Viewport();

                this.viewport.FitToView(
                    currentImageRtItem.FullImage.Size,
                    mainPictureBox.ClientSize);

                // Initialize ROI controller
                this.roiController = new RoiController(projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == imageGuid).Roi);

                mainPictureBox.Invalidate();

                // Update inference results control for current image if they exist
                var segRes = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == currentImageGuid).SegmentationStats.Values.ToList();

                var (macro, micro) = AggregateResults(segRes);

                //Check if any results exist
                if (!IsAllZero(macro))
                    this.inferenceResultsControlCurrentImage.SegmentationStats = macro;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying image {imageGuid}: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            this.lblThreshold.Text = tBThreshold.Value.ToString();

            if (this.projectPresenter.Project != null)
                return;

            this.projectPresenter.Project.Settings.HeatmapThreshold = MapRangeStringToInt(this.lblThreshold.Text, 0, 100, 0, 255);
        }

        private void UnloadCurrentProject()
        {
            // Clear runtime caches and per-image volatile data Bitmap holds GDI handles
            currentImageGuid = Guid.Empty;
            currentImageRtItem = null;

            // Clear UI elems
            imagesControl.ClearGrid();
            //currentRoi = Rectangle.Empty;
            this.inferenceResultsControlAllImages.ClearPlot();
            this.inferenceResultsControlCurrentImage.ClearPlot();

            // Force GC cleanup of disposed Bitmaps (GDI resources)
            GC.Collect();
            GC.WaitForPendingFinalizers();
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

                int added = 0, existing = 0, failed = 0;
                var beforeCount = projectPresenter.Project.Images.Count;

                foreach (var file in ofd.FileNames)
                {
                    if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                    {
                        failed++;
                        continue;
                    }

                    await Task.Run(() => { projectPresenter.AddImage(file); });

                    if (projectPresenter.Project.Images.Count > beforeCount)
                        added++;
                    else
                        existing++;

                    var newAddedImageItem = FindByPath(projectPresenter.Project, file);

                    imagesControl.AddImage(newAddedImageItem.Guid, file);

                    // Always init new image with Train category and full Roi
                    imagesControl.UpdateCategory(newAddedImageItem.Guid, DatasetSplit.Train);
                    newAddedImageItem.Roi = new Rectangle(0, 0, newAddedImageItem.ImageSize.Width, newAddedImageItem.ImageSize.Height);
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
                    await Task.Run(() => { projectPresenter.RemoveImage(id); });

                    if (currentImageGuid == id)
                    {
                        currentImageGuid = Guid.Empty;
                        currentImageRtItem = null;
                    }
                    imagesRepo.DisposeRuntime(id);
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


                    foreach (var img in projectPresenter.Project.Images)
                    {
                        var rt = imagesRepo.GetRuntime(img.Guid);
                        rt.Mask.RemapClasses(classesRemap);



                        overlayRenderer.UpdateAnnotationOverlayRegion(
                            rt.Annotation,
                            rt.Mask,
                            currentFeatureColorMap,
                            new Rectangle(0, 0, rt.Mask.Width, rt.Mask.Height),
                            overlayAlpha);
                    }


                }
                if (currentFeatures.Count > 0 & imagesControl.GetImageIds().Count > 0)
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
                var current = interactionModeController.ActiveMode;

                if (current == InteractionMode.Roi)
                {
                    // Toggle ROI off → return to Pan
                    interactionModeController.SetMode(InteractionMode.Pan);
                }
                else
                {
                    // Toggle ROI on
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
                if (projectPresenter.Project == null)
                {
                    MessageBox.Show("No project loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog();
                sfd.Filter = "Project JSON|*.json|All files|*.*";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    tbProjectPath.Text = sfd.FileName;

                    if (this.projectPresenter.Project != null)
                    {
                        UnloadCurrentProject();
                    }

                    await Task.Run(() => { projectPresenter.NewProject(sfd.FileName); });
                    await Task.Run(() => { projectPresenter.LoadProject(sfd.FileName, imagesRepo); });

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
                if (string.IsNullOrWhiteSpace(this.projectPresenter.ProjectPath))
                {
                    using var sfd = new SaveFileDialog();
                    sfd.Filter = "Project JSON|*.json|All files|*.*";
                    if (sfd.ShowDialog() != DialogResult.OK) return;

                    tbProjectPath.Text = sfd.FileName;

                    this.projectPresenter.ProjectPath = Path.GetDirectoryName(sfd.FileName);
                    this.projectPresenter.ProjectName = Path.GetFileNameWithoutExtension(sfd.FileName);
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
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    tbProjectPath.Text = sfd.FileName;

                    this.projectPresenter.ProjectPath = Path.GetDirectoryName(sfd.FileName);
                    this.projectPresenter.ProjectName = Path.GetFileNameWithoutExtension(sfd.FileName);

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
                if (ofd.ShowDialog() != DialogResult.OK) return;

                tbProjectPath.Text = ofd.FileName;


                if (this.projectPresenter.Project != null)
                {
                    UnloadCurrentProject();
                }
                await Task.Run(() => { projectPresenter.LoadProject(ofd.FileName, imagesRepo); });

                var allFeatureStats = projectPresenter.Project.Images
                    .SelectMany(img => img.SegmentationStats.Values)
                    .ToList();

                var (macro, micro) = AggregateResults(allFeatureStats);

                // Check if any results exist
                if (!IsAllZero(macro))
                    this.inferenceResultsControlAllImages.SegmentationStats = macro;

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

                currentImageRtItem = null;
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
                var annotationsPath = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.Annotations);

                //Load any saved annotations/ masks by GUID
                foreach (var it in projectPresenter.Project.Images)
                {
                    var id = it.Guid;
                    var rt = imagesRepo.GetRuntime(id);

                    var annPng = Path.Combine(annotationsPath, id + ".png");
                    if (File.Exists(annPng))
                    {
                        using var tmp = Image.FromFile(annPng);

                        rt.Annotation = new Bitmap(tmp); // unlocked copy
                    }
                }
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
                    var it = projectPresenter.Project.Images.FirstOrDefault(x => x.Guid == id);

                    it.Split = DatasetSplit.Train;

                    imagesControl.UpdateCategory(id, DatasetSplit.Train);
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
                    var it = projectPresenter.Project.Images.FirstOrDefault(x => x.Guid == id);

                    it.Split = DatasetSplit.Validate;

                    imagesControl.UpdateCategory(id, DatasetSplit.Validate);
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
                    var it = projectPresenter.Project.Images.FirstOrDefault(x => x.Guid == id);

                    it.Split = DatasetSplit.Test;

                    imagesControl.UpdateCategory(id, DatasetSplit.Test);
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
                // Save project first
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

                await trainingForm.StartTrainingRun(this.projectPresenter);
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
                // Save project first
                if (projectPresenter.ProjectPath == null)
                {
                    MessageBox.Show("Please save project first.");
                    return;
                }
                else
                {
                    await Task.Run(() => { projectPresenter.SaveProject(imagesRepo); });
                }

                this.projectPresenter.Project.Settings.HeatmapThreshold =
                    MapRangeStringToInt(this.lblThreshold.Text, 0, 100, 0, 255);

                var trainedModelsForm = trainedModelsFormFactory();

                trainedModelsForm.ModelsPath = projectOptionsService.GetFolderPath(projectPresenter.ProjectPath, ProjectFolderType.Models);
                trainedModelsForm.ModelSubFileName = projectOptionsService.GetModelsSubFileName();
                trainedModelsForm.TrainingSettingsSubFileName = projectOptionsService.GetTrainingSettingsSubFileName();

                if (trainedModelsForm.ShowDialog(this) != DialogResult.OK)
                    return;

                var modelPath = Path.ChangeExtension(Path.Combine(trainedModelsForm.ModelsPath, trainedModelsForm.SelectedModelFileName), ".bin");


                if (trainedModelsForm.SelectedModelFileName != this.currentSelectedModelFileName)
                {
                    this.currentSelectedModelFileName = trainedModelsForm.SelectedModelFileName;

                    // Hide MainForms overlay
                    ShowOverlay();


                    var heatmapsImagesOld = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsImages);
                    var heatmapsOverlaysOld = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsOverlays);

                    TryDeleteDirectoryContents(heatmapsImagesOld, out var errImgs);
                    TryDeleteDirectoryContents(heatmapsOverlaysOld, out var errOverlays);

                    var inferenceForm = inferenceFormFactory();

                    inferenceForm.FormClosed += (s, args) => HideOverlay();
                    inferenceForm.Show(this);

                    await inferenceForm.StartInferenceRun(this.projectPresenter, modelPath);
                }

                // Show heatmaps
                var heatmapsImages = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsImages);
                var heatmapsOverlays = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsOverlays);

                var allFeatureStats = projectPresenter.Project.Images.SelectMany(img => img.SegmentationStats.Values).ToList();

                var (macro, micro) = AggregateResults(allFeatureStats);

                // Check if any results exist
                if (!IsAllZero(macro))
                    this.inferenceResultsControlAllImages.SegmentationStats = macro;

                if (projectPresenter.Project.Images.Count > 0)
                {
                    var averageInferenceMs = projectPresenter.Project.Images.Skip(1).Average(img => img.InferenceMs);

                    this.lblInferenceMeanComputeTime.Text =
                        $"Average compute time: {Math.Round(averageInferenceMs, 0):F1} ms";
                }

                // Load any saved heatmaps by GUID
                foreach (var it in projectPresenter.Project.Images)
                {
                    var id = it.Guid;
                    var rt = imagesRepo.GetRuntime(id);

                    // Get feature subfolder based on selected feature
                    if (currentSelectedFeature != null)
                    {
                        var suffixes = it.SegmentationStats.Keys.Select(k => k.ToString()).ToList();
                        var featureSubfolder = FindMatchingFolder(heatmapsImages, currentSelectedFeature.Name, suffixes);

                        if (featureSubfolder != null)
                        {
                            var heatPng = Path.Combine(featureSubfolder, id + ".png");
                            if (File.Exists(heatPng))
                            {
                                using (var tmp = Image.FromFile(heatPng))
                                {
                                    imagesRepo.SetHeatmap(id, new Bitmap(tmp));
                                }
                            }
                        }
                    }

                    // Update inference results control for current image if they exist
                    var (itMacro, itMicro) = AggregateResults(it.SegmentationStats.Values.ToList());

                    //Check if any results exist
                    if (!IsAllZero(itMacro))
                        this.inferenceResultsControlCurrentImage.SegmentationStats = itMacro;

                    imagesControl.UpdateTrainResult(it.Guid, Math.Round(itMacro.Dice * 100, 1).ToString());
                }

                // Select first image
                var ids = imagesControl.GetImageIds();
                if (ids.Count > 0)
                    imagesControl.SelectImage(ids[0]);

                // Select first feature
                if (this.currentFeatures.Count > 0)
                {
                    this.featuresControl.SelectFeature(this.currentFeatures[0]);
                }

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

        #region Mouse events

        private void mainDisplayPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentImageRtItem.Annotation == null)
                return;
            if (e.Button != MouseButtons.Left)
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

                        brushController.BeginStroke(
                            e.Location,
                            viewport,
                            annotationToolsControl.BrushSize,
                            (byte)currentSelectedFeature.ClassId);

                        if (brushController.UpdateStroke(
                            e.Location,
                            viewport,
                            imagesRepo.GetRuntime(currentImageGuid).Mask,
                            out var dirtyImageRect))
                        {

                            overlayRenderer.UpdateAnnotationOverlayRegion(
                            currentImageRtItem.Annotation,
                            imagesRepo.GetRuntime(currentImageGuid).Mask,
                            currentFeatureColorMap,
                            dirtyImageRect,
                            overlayAlpha); // alpha

                            var dirtyScreenRect = viewport.ImageToScreenRect(dirtyImageRect);
                            mainPictureBox.Invalidate(new Region(dirtyScreenRect));
                        }
                        break;
                    }
                case InteractionMode.Erase:
                    {
                        brushController.BeginStroke(
                            e.Location,
                            viewport,
                            annotationToolsControl.BrushSize,
                            (byte)0); // background

                        if (brushController.UpdateStroke(
                            e.Location,
                            viewport,
                            imagesRepo.GetRuntime(currentImageGuid).Mask,
                            out var dirtyImageRect))
                        {

                            overlayRenderer.UpdateAnnotationOverlayRegion(
                            currentImageRtItem.Annotation,
                            imagesRepo.GetRuntime(currentImageGuid).Mask,
                            currentFeatureColorMap,
                            dirtyImageRect,
                            overlayAlpha); // alpha

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
            if (currentImageRtItem == null)
                return;

            switch (interactionModeController.ActiveMode)
            {
                case InteractionMode.Paint:
                case InteractionMode.Erase:
                    {
                        if (brushController.UpdateStroke(
                            e.Location,
                            viewport,
                            imagesRepo.GetRuntime(currentImageGuid).Mask,
                            out var dirtyImageRect))
                        {

                            overlayRenderer.UpdateAnnotationOverlayRegion(
                                currentImageRtItem.Annotation,
                                imagesRepo.GetRuntime(currentImageGuid).Mask,
                                currentFeatureColorMap,
                                dirtyImageRect,
                                overlayAlpha); // alpha

                            var dirtyScreenRect = viewport.ImageToScreenRect(dirtyImageRect);
                            mainPictureBox.Invalidate(new Region(dirtyScreenRect));
                        }

                        break;
                    }
                case InteractionMode.Roi:
                    {
                        if (roiController == null)
                            break;

                        roiController.MouseMove(
                            e.Location,
                            viewport,
                            new SizeF(currentImageRtItem.FullImage.Width, currentImageRtItem.FullImage.Height));

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

            // Finalize brush stroke (paint / erase)
            brushController.EndStroke();

            // Finalize ROI interaction
            if (roiController != null)
            {
                roiController.MouseUp();

                if (interactionModeController.ActiveMode == InteractionMode.Roi)
                {
                    // Commit ROI to project model
                    var item = projectPresenter.Project.Images
                        .FirstOrDefault(i => i.Guid == currentImageGuid);

                    if (item != null)
                    {
                        item.Roi = roiController.GetRoundedRoi();

                        //currentRoi = item.Roi;
                    }
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
            if (currentImageRtItem == null)
                return;
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
            if (overlayPanel != null)
            {
                overlayPanel.Dispose();
                overlayPanel = null;
            }
        }

        #endregion

    }
}