using AnnotationTool.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using static AnnotationTool.Core.Utils.CoreUtils;


namespace AnnotationTool.Core.Services
{
    /// <summary>
    /// Runtime project presenter/manager.
    /// </summary>
    public class ProjectPresenter : IProjectPresenter
    {
        /// <summary>
        /// Gets the deep learning project associated with this instance.
        /// </summary>
        public DeepLearningProject Project { get; private set; }


        public event EventHandler ProjectLoaded;
        public event EventHandler<string> ErrorOccured;

        private readonly JsonSerializerOptions jsonOptions;
        private readonly IProjectOptionsService projectOptionsService;
        private ProjectPaths paths;
        private string projectPath;


        public ProjectPresenter(JsonSerializerOptions jsonOptions, IProjectOptionsService projectOptionsService)
        {
            this.jsonOptions = jsonOptions;
            this.projectOptionsService = projectOptionsService;
            this.Project = new DeepLearningProject();
        }

        /// <summary>
        /// After new/load/save project this is the current project path
        /// </summary>
        public string ProjectPath
        {
            get => this.projectPath;
            set
            {
                if (this.projectPath == value)
                    return;

                this.projectPath = value;

                // Invalidate derived paths
                this.paths = null;
            }
        }

        /// <summary>
        /// File paths depending on current ProjectPath and ProjectOptions
        /// </summary>
        public ProjectPaths Paths
        {
            get
            {
                if (this.paths == null)
                {
                    if (string.IsNullOrWhiteSpace(this.ProjectPath))
                        throw new InvalidOperationException("ProjectPath not set.");

                    this.paths = new ProjectPaths(this.ProjectPath, this.Project.Name, this.projectOptionsService);
                }
                return this.paths;
            }
        }

        public void LoadProject(string jsonPath, ImageRepository imagesRepo)
        {
            try
            {
                ProjectPath = Path.GetDirectoryName(jsonPath);
                Project.Name = Path.GetFileNameWithoutExtension(jsonPath);

                // Create folder structure if missing
                projectOptionsService.EnsureAll(ProjectPath);

                // Resolve all paths
                var paths = Paths;

                // Load project json
                var json = File.ReadAllText(paths.JsonPath);
                var loadedTemp = JsonSerializer.Deserialize<DeepLearningProject>(json, jsonOptions);

                Project.CopyFrom(loadedTemp);

                // Clear runtime data
                imagesRepo.Clear();

                if (Project.Images == null)
                    return;

                foreach (var it in Project.Images)
                {
                    var id = it.Guid;
                    var rt = imagesRepo.GetRuntime(id);
                    var imgPng = Path.Combine(paths.Images, id + paths.ImagesExt);
                    var annPng = Path.Combine(paths.Annotations, id + paths.ImagesExt);
                    var maskPng = Path.Combine(paths.Masks, id + paths.ImagesExt);

                    // Load annotation bitmap if present
                    if (File.Exists(annPng))
                    {
                        using (var fs = File.OpenRead(annPng))
                        using (var tmp = Image.FromStream(fs))
                        {
                            rt.EnsureAnnotation(tmp.Width, tmp.Height);

                            rt.WithAnnotation(bmp =>
                            {
                                using (var g = Graphics.FromImage(bmp))
                                {
                                    g.DrawImageUnscaled(tmp, 0, 0);
                                }
                            });
                        }
                    }

                    // Load label mask if present
                    if (File.Exists(maskPng))
                    {
                        LabelMask.TryLoadPng8(maskPng, out var lm);
                        rt.EnsureMask(lm.Width, lm.Height);
                        rt.Mask.CopyFrom(lm);
                    }
                }

                ProjectLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) { /* canceled, ignore */ }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, ex.Message);
            }
        }

        public void UpdateTrainingSettings(string jsonPath)
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var settingsTemp = JsonSerializer.Deserialize<SavedModelPackage>(json, jsonOptions);

                Project.Settings.CopyFrom(settingsTemp.Settings);
            }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, ex.Message);
            }
        }

        public void SaveJsonFile()
        {
            var paths = Paths;

            // Save json project file
            var json = JsonSerializer.Serialize(Project, jsonOptions);
            File.WriteAllText(paths.JsonPath, json);
        }

        public void SaveProject(ImageRepository imagesRepo)
        {
            try
            {
                // Resolve all paths and create folder structure if missing
                var paths = Paths;
                projectOptionsService.EnsureAll(paths.Root);

                // Copy/save into folder/{guid}.{ext}
                foreach (var item in Project.Images)
                {
                    var id = item.Guid;
                    var rt = imagesRepo.GetRuntime(id);

                    // All source images are expected to be in this folder, if not -> copy from original path to project folder
                    var imgProjectPath = Path.Combine(paths.Images, id + paths.ImagesExt);

                    if (!File.Exists(imgProjectPath))
                    {
                        if (File.Exists(item.Path))
                        {
                            File.Copy(item.Path, imgProjectPath, overwrite: true);
                        }
                        else
                        {
                            ErrorOccured?.Invoke(this, $"Image file not found for image {id}: {item.Path}");
                            continue;

                        }
                    }
                    item.Path = imgProjectPath;

                    // Save annotations
                    var annPath = Path.Combine(paths.Annotations, id + paths.ImagesExt);
                    rt.WithAnnotation(bmp =>
                    {
                        bmp.Save(annPath, ImageFormat.Png);
                    });

                    // Save masks
                    var maskPath = Path.Combine(paths.Masks, id + paths.ImagesExt);
                    LabelMask.SavePng8(maskPath, rt.Mask);
                }
                // Save json project file
                var json = JsonSerializer.Serialize(Project, jsonOptions);
                File.WriteAllText(paths.JsonPath, json);
            }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, ex.Message);
            }
        }

        public ImageItem AddImage(string imageSourcePath)
        {
            try
            {
                var item = new ImageItem() { Path = imageSourcePath };
                Project.Images.Add(item);

                return item;
            }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, ex.Message);
                return null;
            }
        }

        public void RemoveImage(Guid imageId)
        {
            if (Project == null)
                return;

            // Remove from project
            var toRemove = FindById(Project, imageId);
            if (toRemove != null)
            {
                Project.Images.Remove(toRemove);
            }

            // If project not saved yet, just return
            if (string.IsNullOrWhiteSpace(ProjectPath))
                return;

            var paths = Paths;

            // Remove associated files
            SafeDeleteFile(Path.Combine(paths.Images, imageId + paths.ImagesExt));
            SafeDeleteFile(Path.Combine(paths.Annotations, imageId + paths.ImagesExt));
            SafeDeleteFile(Path.Combine(paths.Masks, imageId + paths.ImagesExt));
        }

        public void NewProject(string jsonPath)
        {
            try
            {
                // Set up paths and create folder structure
                ProjectPath = Path.GetDirectoryName(jsonPath);
                Project.Name = Path.GetFileNameWithoutExtension(jsonPath);
                projectOptionsService.EnsureAll(ProjectPath);

                Project.CopyFrom(new DeepLearningProject());

                // Serialize json project file
                var json = JsonSerializer.Serialize(Project, jsonOptions);
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                ErrorOccured?.Invoke(this, $"Error creating new project: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get SLICED image/mask pairs for training based on the current project and specified dataset split.
        /// </summary>
        /// <param name="split"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IReadOnlyList<(string imagePath, string maskPath)> GetSlicedTrainingPairs(DatasetSplit split)
        {
            if (Project == null)
                throw new InvalidOperationException("Project not loaded.");

            var result = new List<(string imagePath, string maskPath)>();
            var paths = Paths;

            // Gather all GUIDs that are marked for training
            var trainGuids = Project.Images.Where(img => img.Split == split).Select(img => img.Guid);

            var allSliceImages = Directory.EnumerateFiles(paths.SlicedImages, "*" + paths.ImagesExt);
            var allSliceMasks = Directory.EnumerateFiles(paths.SlicedMasks, "*" + paths.ImagesExt);

            foreach (var id in trainGuids)
            {
                // Get all slice images/masks for this GUID
                var sliceImages = allSliceImages.Where(i => i.Contains(id.ToString())).ToList();
                var sliceMasks = allSliceMasks.Where(i => i.Contains(id.ToString())).ToList();

                // There needs to be always the same number of images and masks
                if (sliceImages.Count == sliceMasks.Count && sliceImages.Count > 0)
                {
                    for (int i = 0; i < sliceImages.Count; i++)
                    {
                        result.Add((sliceImages[i], sliceMasks[i]));
                    }
                }
            }
            return result;
        }

        public string ResolveImagePath(Guid imageGuid)
        {
            var img = Project.Images.FirstOrDefault(i => i.Guid == imageGuid);
            if (img == null)
                return null;

            var imgPath = string.Empty;


            // Project not saved yet -> no Projectpath -> use original image path
            if (ProjectPath == null)
            {
                imgPath = img.Path;
            }
            else
            {
                var paths = Paths;
                var itemPath = Path.Combine(Paths.Images, imageGuid + paths.ImagesExt);

                // Use relative path if image exists in project folder
                if (File.Exists(itemPath))
                {
                    imgPath = itemPath;
                }
                else
                {
                    // Project saved -> added new images -> new images not saved yet in project -> use original path
                    imgPath = img.Path;
                }
            }
            return imgPath;
        }

    }
}
