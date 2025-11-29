using AnnotationTool.App.Controls;
using AnnotationTool.App.Drawing;
using AnnotationTool.App.Forms;
using AnnotationTool.Core.IO;
using AnnotationTool.Core.Logging;
using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Devices;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using static AnnotationTool.Ai.Utils.DatasetStatistics;
using static AnnotationTool.Core.Services.ProjectStore;
using static AnnotationTool.Core.Utils.CoreUtils;
using static TorchSharp.torch;

namespace AnnotationTool.App
{
    /// <summary>
    /// 
    /// Application for complete annotation + training + inference loop
    /// 
    /// Ai and Core class libs target: netstandard 2.0
    /// 
    /// </summary>
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
        private readonly BrushTool brushTool = new();
        private readonly Viewport viewport = new();
        private Guid currentImageGuid;
        private Bitmap? currentImage;
        private Bitmap? currentAnnotation;
        private Bitmap? currentHeatmap;
        private Rectangle currentRoi;
        private List<Feature> currentFeatures = [];
        private string currentSelectedModelFileName = "";
        private bool isPanning;
        private bool isPainting;
        private Point lastMousePos;
        private int lastImgX;
        private int lastImgY;
        private bool showRoi;
        private RoiMode roiMode = RoiMode.None;
        private int currentBrushSize;
        private BrushMode lastClickedBrushMode = BrushMode.None;
        private PipelineLoopState currentPipelineLoopState = PipelineLoopState.Annotation;

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
            this.projectPresenter.ProjectLoaded += Presenter_ProjectLoaded;
            this.projectPresenter.ErrorOccured += Presenter_ErrorOccured;

            this.deepLearningSettingsControl.Initialize(this.projectPresenter);
            this.deepLearningSettingsControl.SettingsChanged += DeepLearningSettingsControl_SettingsChanged;
            this.deepLearningSettingsControl.RefreshBindings();

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
            currentImage?.Dispose();
            currentImage = null;
            currentAnnotation?.Dispose();
            currentAnnotation = null;
            currentImageGuid = Guid.Empty;
            mainPictureBox.Invalidate();
            imagesControl.ClearGrid();

            // Load features
            currentFeatures = this.projectPresenter.Project.Features
                .Select(fd => new Feature { Name = fd.Name, Argb = fd.Argb })
                .ToList();
            featuresControl.UpdateFeatures(currentFeatures);


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
            mainPictureBox.Invalidate();
        }

        private void ImageGridControl_ImageAdded(object? sender, (Guid id, int Width, int Height) e)
        {
            if (e.id == Guid.Empty) return;

            var item = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == e.id);
            if (item == null) return;

            item.Roi = EnsureRoi(item.Roi, e.Width, e.Height);

            // Ensure there is a mask for this image (kept by the repo)
            var (mask, annotation) = imagesRepo.GetOrCreateAnnotationMask(e.id, e.Width, e.Height);

            // Auto-select the first added image if nothing is selected yet
            if (currentImageGuid == Guid.Empty)
                ImageGridControl_ImageSelected(this, e.id);
        }

        private void mainDisplayPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (currentImage == null)
                return;

            var g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var scaledWidth = (int)(currentImage.Width * viewport.Zoom);
            var scaledHeight = (int)(currentImage.Height * viewport.Zoom);

            var baseSize = projectPresenter.Project.Settings.PreprocessingSettings.SliceSize;
            var downSample = projectPresenter.Project.Settings.PreprocessingSettings.DownSample;

            // Draw the original image
            g.DrawImage(currentImage, new Rectangle((int)viewport.Offset.X, (int)viewport.Offset.Y, scaledWidth, scaledHeight));

            // Draw the annotation overlay with 50% transparency
            switch (currentPipelineLoopState)
            {
                case PipelineLoopState.Annotation:

                    if (currentAnnotation != null)
                    {
                        using var ia = new ImageAttributes();

                        var cm = new ColorMatrix { Matrix33 = 0.65f }; // 50% alpha
                        ia.SetColorMatrix(cm);
                        g.DrawImage(currentAnnotation,
                            new Rectangle((int)viewport.Offset.X, (int)viewport.Offset.Y, scaledWidth, scaledHeight),
                            0,
                            0,
                            currentAnnotation.Width,
                            currentAnnotation.Height,
                            GraphicsUnit.Pixel,
                            ia);
                    }
                    break;

                case PipelineLoopState.InferenceResults:

                    if (currentHeatmap != null)
                    {
                        using var ia = new ImageAttributes();

                        var cm = new ColorMatrix { Matrix33 = 1.0f }; // 50% alpha
                        ia.SetColorMatrix(cm);
                        g.DrawImage(currentHeatmap,
                            new Rectangle((int)viewport.Offset.X, (int)viewport.Offset.Y, scaledWidth, scaledHeight),
                            0,
                            0,
                            currentHeatmap.Width,
                            currentHeatmap.Height,
                            GraphicsUnit.Pixel,
                            ia);
                    }
                    break;
            }

            // Draw the ROI if enabled
            if (showRoi)
            {
                var screenRoi = viewport.ImageToScreenRect(currentRoi);

                using var pen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };

                g.DrawRectangle(pen, screenRoi);
            }

            // Draw brush size indicator
            if (lastClickedBrushMode == BrushMode.MouseDown)
            {
                //var effectiveBrushSize = currentBrushSize * (1 << downSample);

                using var pen = new Pen(Color.Blue, 4) { DashStyle = DashStyle.Solid };

                g.DrawEllipse(
                    pen,
                    mainPictureBox.Width / 2 - currentBrushSize / 2,
                    mainPictureBox.Height / 2 - currentBrushSize / 2,
                    (int)(currentBrushSize * viewport.Zoom),
                    (int)(currentBrushSize * viewport.Zoom));
            }

            // Draw feature size rectangle at top-left of the PictureBox
            // imgRect := in image coordinates
            // screenRect := in PictureBox coordinates
            if (projectPresenter.Project.Settings.PreprocessingSettings.SliceSize == 0)
                return;

            var effectiveSize = baseSize * (1 << downSample);
            // Define rectangle in screen coordinates, fixed at (10, 10) relative to PictureBox
            var screenRect = new Rectangle(10, 10, (int)(effectiveSize * viewport.Zoom), (int)(effectiveSize * viewport.Zoom));

            using var p = new Pen(Color.Green, 2);

            g.DrawRectangle(p, screenRect);
        }

        private void BrushSize_Changed(object? sender, int e)
        {
            this.currentBrushSize = e;
            this.lastClickedBrushMode = annotationToolsControl.LastClickedBrushMode;
            mainPictureBox.Invalidate();
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

                // Load/cache full-res bitmap (repo owns lifetime)
                currentImage = imagesRepo.EnsureFull(imageGuid, path);

                var item = imagesRepo.GetRuntime(imageGuid);

                currentAnnotation = item.Annotation!;
                currentHeatmap = item.Heatmap;
                currentRoi = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == imageGuid).Roi;
           

                viewport.ImageSize = currentImage.Size;
                viewport.Zoom = Math.Min((float)mainPictureBox.Height / viewport.ImageSize.Height, (float)mainPictureBox.Width / viewport.ImageSize.Width);
                viewport.Offset = new PointF(0, 0);

                mainPictureBox.Invalidate();

                // Update inference results control for current image if they exist
                var segRes = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == currentImageGuid).SegmentationStats;

                // Check if any results exist
                if (!IsAllZero(segRes))
                    this.inferenceResultsControlCurrentImage.SegmentationStats = segRes;

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
            currentImage?.Dispose();
            currentAnnotation?.Dispose();
            currentHeatmap?.Dispose();
            currentImage = null;
            currentAnnotation = null;
            currentHeatmap = null;

            currentImageGuid = Guid.Empty;

            // Clear UI elems
            imagesControl.ClearGrid();
            currentRoi = Rectangle.Empty;
            this.inferenceResultsControlAllImages.ClearPlot();
            this.inferenceResultsControlCurrentImage.ClearPlot();

            //projectPresenter.CloseProject();

            // Force GC cleanup of disposed Bitmaps (GDI resources)
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                int added = 0, existing = 0, failed = 0;

                foreach (var file in ofd.FileNames)
                {
                    if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                    {
                        failed++;
                        continue;
                    }

                    var beforeCount = projectPresenter.Project.Images.Count;

                    await projectPresenter.AddImageAsync(file);

                    if (projectPresenter.Project.Images.Count > beforeCount) added++; else existing++;

                    var currentItem = FindByPath(projectPresenter.Project, file);

                    imagesControl.AddImage(currentItem.Guid, file);
                    imagesControl.UpdateCategory(currentItem.Guid, DatasetSplit.Train);
                }

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
                    await projectPresenter.RemoveImageAsync(id);

                    if (currentImageGuid == id)
                    {
                        currentImageGuid = Guid.Empty;
                        currentImage = null;
                        currentAnnotation?.Dispose();
                        currentAnnotation = null;
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
                using var editorForm = new FeaturesEditor(currentFeatures);

                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    currentFeatures = editorForm.GetUpdatedFeatures();
                    featuresControl.UpdateFeatures(currentFeatures);
                    projectPresenter.Project.Features = currentFeatures;
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
                showRoi = !showRoi;
                btnToggleRoi.BackgroundImage = showRoi ? Properties.Resources.ToggleRoiClicked : Properties.Resources.ToggleRoi;

                if (showRoi)
                {
                    annotationToolsControl.Enabled = false;
                    annotationToolsControl.EraseActive = false;
                    annotationToolsControl.PaintActive = false;
                    annotationToolsControl.UpdateButtonStates();
                }
                else
                {
                    annotationToolsControl.Enabled = true;
                }


                mainPictureBox.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling ROI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    await projectPresenter.NewProjectAsync(sfd.FileName);
                    await projectPresenter.LoadProjectAsync(sfd.FileName, imagesRepo);

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
                    await projectPresenter.SaveProjectAsync(imagesRepo);

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

                    await projectPresenter.SaveProjectAsync(imagesRepo);

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

                await projectPresenter.LoadProjectAsync(ofd.FileName, imagesRepo);


                var (macro, micro) = AggregateResults(projectPresenter.Project.Images.Select(i => i.SegmentationStats).ToList());

                // Check if any results exist
                if (!IsAllZero(micro))
                    this.inferenceResultsControlAllImages.SegmentationStats = micro;

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

                currentImage?.Dispose();
                currentAnnotation?.Dispose();
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

            // Only for debugging
            var sliceOnly = false;

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
                    await projectPresenter.SaveProjectAsync(imagesRepo);
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

                await trainingForm.StartTrainingAsync(this.projectPresenter, sliceOnly);
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
                    await projectPresenter.SaveProjectAsync(imagesRepo);
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

                    var inferenceForm = inferenceFormFactory();

                    inferenceForm.FormClosed += (s, args) => HideOverlay();
                    inferenceForm.Show(this);


                    await inferenceForm.StartInferenceAsync(this.projectPresenter, modelPath);
                }

                // Show heatmaps
                var heatmapsImages = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsImages);
                var heatmapsOverlays = projectOptionsService.GetFolderPath(this.projectPresenter.ProjectPath, ProjectFolderType.HeatmapsOverlays);

                // Load any saved heatmaps by GUID
                foreach (var it in projectPresenter.Project.Images)
                {
                    var id = it.Guid;
                    var rt = imagesRepo.GetRuntime(id);

                    var heatPng = Path.Combine(heatmapsImages, id + ".png");
                    if (File.Exists(heatPng))
                    {
                        using var tmp = Image.FromFile(heatPng);

                        rt.Heatmap = new Bitmap(tmp); // unlocked copy
                    }
                    // Refresh views
                    imagesControl.UpdateTrainResult(it.Guid, Math.Round(it.SegmentationStats.Dice * 100, 1).ToString());
                }

                var (macro, micro) = AggregateResults(projectPresenter.Project.Images.Select(i => i.SegmentationStats).ToList());

                // Check if any results exist
                if (!IsAllZero(micro))
                    this.inferenceResultsControlAllImages.SegmentationStats = micro;


                this.lblInferenceMeanComputeTime.Text =
                    $"Average compute time: {Math.Round(macro.InferenceMs, 0):F1} ms";

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

        #region Mouse events

        private void mainDisplayPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (annotationToolsControl.PaintActive || annotationToolsControl.EraseActive)
            {
                if (currentAnnotation == null) return;

                var imgX = viewport.ScreenToImage(e.Location).X;
                var imgY = viewport.ScreenToImage(e.Location).Y;

                // If mouse is not over image don't draw
                if (imgX < 0 || imgX >= currentImage.Width || imgY < 0 || imgY >= currentImage.Height) return;

                isPainting = true;
                lastImgX = (int)imgX;
                lastImgY = (int)imgY;
                Color color;
                var compMode = CompositingMode.SourceOver;
                var size = annotationToolsControl.BrushSize;
                byte classId;



                // While erasing
                if (annotationToolsControl.EraseActive)
                {
                    color = Color.Transparent;
                    compMode = CompositingMode.SourceCopy;
                }
                else
                {
                    if (featuresControl.SelectedFeature == null)
                    {
                        MessageBox.Show("Please select a feature to paint.");
                        return;
                    }
                    color = Color.FromArgb(featuresControl.SelectedFeature.Argb);
                }


                if (annotationToolsControl.EraseActive)
                    classId = 0; // background
                else
                {
                    if (featuresControl.SelectedFeature == null)
                    {
                        MessageBox.Show("Please select a feature to paint.");
                        return;
                    }
                    var selected = featuresControl.SelectedFeature;
                    var idx = currentFeatures.FindIndex(f => f.Name == selected.Name);
                    if (idx < 0) { MessageBox.Show("Selected feature not found."); return; }
                    classId = (byte)(idx + 1);
                }


                // Update annotation image and label mask for single clicks / dots
                brushTool.DrawBrush((int)imgX, (int)imgY, currentAnnotation, color, size, compMode);
                var ir = imagesRepo.GetRuntime(currentImageGuid).Mask.FillCircle((int)imgX, (int)imgY, size / 2, classId);

                mainPictureBox.Invalidate();
            }
            else if (showRoi)
            {
                var screenROI = viewport.ImageToScreenRect(currentRoi);
                var hitMode = GetHitMode(e.Location, screenROI);

                if (hitMode.HasValue)
                {
                    roiMode = hitMode.Value;
                    lastMousePos = e.Location;
                }
                else if (screenROI.Contains(e.Location))
                {
                    roiMode = RoiMode.Moving;
                    lastMousePos = e.Location;
                }
            }
            else
            {
                isPanning = true;
                lastMousePos = e.Location;
            }
        }

        private void mainDisplayPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentImageGuid == Guid.Empty || currentImage == null)
                return;

            if (isPanning && e.Button == MouseButtons.Left)
            {
                var dx = e.X - lastMousePos.X;
                viewport.Offset = new PointF(viewport.Offset.X + dx, viewport.Offset.Y + (e.Y - lastMousePos.Y));
                lastMousePos = e.Location;
                mainPictureBox.Invalidate();
            }
            else if (isPainting && e.Button == MouseButtons.Left)
            {
                if (currentAnnotation == null) return;


                var imgPt = viewport.ScreenToImage(e.Location);
                var imgX = (int)imgPt.X;
                var imgY = (int)imgPt.Y;
                Color color;
                var compMode = CompositingMode.SourceOver;
                var size = annotationToolsControl.BrushSize;
                byte classId;

                // If mouse is not over image don't draw
                if (imgX < 0 || imgX >= currentImage.Width || imgY < 0 || imgY >= currentImage.Height) return;

                if (annotationToolsControl.EraseActive)
                {
                    color = Color.Transparent;
                    compMode = CompositingMode.SourceCopy;
                }
                else
                {
                    if (featuresControl.SelectedFeature == null)
                    {
                        MessageBox.Show("Please select a feature to paint.");
                        return;
                    }

                    color = Color.FromArgb(featuresControl.SelectedFeature.Argb);
                }

                if (annotationToolsControl.EraseActive)
                    classId = 0; // background
                else
                {
                    if (featuresControl.SelectedFeature == null)
                    {
                        MessageBox.Show("Please select a feature to paint.");
                        return;
                    }
                    var selected = featuresControl.SelectedFeature;
                    var idx = currentFeatures.FindIndex(f => f.Name == selected.Name);
                    if (idx < 0) { MessageBox.Show("Selected feature not found."); return; }
                    classId = (byte)(idx + 1);
                }

                // Update annotation image and label mask for brush strokes
                brushTool.DrawLine(lastImgX, lastImgY, imgX, imgY, currentAnnotation, color, size, compMode);
                var ir = imagesRepo.GetRuntime(currentImageGuid).Mask.DrawLine(lastImgX, lastImgY, imgX, imgY, classId, size);


                lastImgX = imgX;
                lastImgY = imgY;
                mainPictureBox.Invalidate();
            }
            else if (roiMode != RoiMode.None)
            {
                var deltaScreen = new Point(e.X - lastMousePos.X, e.Y - lastMousePos.Y);
                lastMousePos = e.Location;

                var deltaImgX = deltaScreen.X / viewport.Zoom;
                var deltaImgY = deltaScreen.Y / viewport.Zoom;

                var newROI = currentRoi;

                switch (roiMode)
                {
                    case RoiMode.Moving:
                        newROI.X += (int)deltaImgX;
                        newROI.Y += (int)deltaImgY;
                        break;
                    case RoiMode.ResizingNW:
                        newROI.X += (int)deltaImgX;
                        newROI.Y += (int)deltaImgY;
                        newROI.Width -= (int)deltaImgX;
                        newROI.Height -= (int)deltaImgY;
                        break;
                    case RoiMode.ResizingN:
                        newROI.Y += (int)deltaImgY;
                        newROI.Height -= (int)deltaImgY;
                        break;
                    case RoiMode.ResizingNE:
                        newROI.Y += (int)deltaImgY;
                        newROI.Height -= (int)deltaImgY;
                        newROI.Width += (int)deltaImgX;
                        break;
                    case RoiMode.ResizingW:
                        newROI.X += (int)deltaImgX;
                        newROI.Width -= (int)deltaImgX;
                        break;
                    case RoiMode.ResizingE:
                        newROI.Width += (int)deltaImgX;
                        break;
                    case RoiMode.ResizingSW:
                        newROI.X += (int)deltaImgX;
                        newROI.Width -= (int)deltaImgX;
                        newROI.Height += (int)deltaImgY;
                        break;
                    case RoiMode.ResizingS:
                        newROI.Height += (int)deltaImgY;
                        break;
                    case RoiMode.ResizingSE:
                        newROI.Width += (int)deltaImgX;
                        newROI.Height += (int)deltaImgY;
                        break;
                }

                // Clamp to image bounds
                newROI.X = Math.Max(0, newROI.X);
                newROI.Y = Math.Max(0, newROI.Y);
                newROI.Width = Math.Max(1, Math.Min(newROI.Width, currentImage.Width - newROI.X));
                newROI.Height = Math.Max(1, Math.Min(newROI.Height, currentImage.Height - newROI.Y));

                currentRoi = newROI;

                var item = projectPresenter.Project.Images.FirstOrDefault(i => i.Guid == currentImageGuid);

                item.Roi = currentRoi;

                mainPictureBox.Invalidate();
            }
        }

        private void mainDisplayPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
            isPainting = false;
            roiMode = RoiMode.None;
        }

        private void DisplayPictureBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (currentImage == null)
                return;

            var cursorImg = viewport.ScreenToImage(e.Location);
            float oldZoom = viewport.Zoom;
            float newZoom = Math.Clamp(oldZoom * (e.Delta > 0 ? 1.1f : 0.9f), 0.05f, 16f);
            if (Math.Abs(newZoom - oldZoom) < 1e-6f) return;
            viewport.Zoom = newZoom;

            var newScreen = new PointF(cursorImg.X * newZoom, cursorImg.Y * newZoom);
            viewport.Offset = new PointF(e.Location.X - newScreen.X, e.Location.Y - newScreen.Y);

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