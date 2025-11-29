using AnnotationTool.Core.IO;
using AnnotationTool.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static AnnotationTool.Core.Services.ProjectStore;
using static AnnotationTool.Core.Utils.CoreUtils;


namespace AnnotationTool.Core.Services
{
	public class ProjectPresenter : IProjectPresenter
	{
		public event EventHandler ProjectLoaded;
		public event EventHandler<string> ErrorOccured;

		private readonly IImageService imageService;
        private readonly JsonSerializerOptions jsonOptions;
		private readonly IProjectOptionsService projectOptionsService;

		public ProjectPresenter(IImageService imageService, JsonSerializerOptions jsonOptions, IProjectOptionsService projectOptionsService)
		{
            this.imageService = imageService;
			this.jsonOptions = jsonOptions;
			this.projectOptionsService = projectOptionsService;
			this.Project = new DeepLearningProject();
        }

        public DeepLearningProject Project { get; private set; }

        public string ProjectPath { get; set; }

        public string ProjectName { get; set; }

        public Task LoadProjectAsync(string jsonPath, ImageRepository imagesRepo)
		{
			return Task.Run(() =>
			{
				try
				{
					this.ProjectPath = Path.GetDirectoryName(jsonPath);
					this.ProjectName = Path.GetFileNameWithoutExtension(jsonPath);


					// Create folder structure if missing and get paths
					projectOptionsService.EnsureAll(this.ProjectPath);
					var imagesPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Images);
					var annotationsPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Annotations);
					var masksPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Masks);

					if (!File.Exists(jsonPath))
					{
						ErrorOccured?.Invoke(this, $"Project file not found: {jsonPath}");
						return;
					}

					var json = File.ReadAllText(jsonPath);
					var loadedTemp = JsonSerializer.Deserialize<DeepLearningProject>(json, jsonOptions);

					this.Project.CopyFrom(loadedTemp);

					imagesRepo.Dispose();

					// Load any saved annotations/masks by GUID
					if (this.Project.Images != null)
					{
						foreach (var it in Project.Images)
						{
							// Prefer a copied file under /images/{guid}.* if present; otherwise use the JSON path.
							it.Path = ResolveImagePathFallback(imagesPath, it.Guid, it.Path, this.ProjectPath);

							var id = it.Guid;
							var rt = imagesRepo.GetRuntime(id);

							var annPng = Path.Combine(annotationsPath, id + ".png");
							if (File.Exists(annPng))
							{
								using (var tmp = Image.FromFile(annPng))
								{
									rt.Annotation = new Bitmap(tmp); // unlocked copy
								}
							}

							var maskPng = Path.Combine(masksPath, id + ".png");
							if (File.Exists(maskPng))
							{
								LabelMask.TryLoadPng8(maskPng, out var lm);
								rt.Mask = lm;
							}
						}
					}

					// Thumbnail generation for UI responsiveness
					if (this.Project.Images != null)
					{
						foreach (var img in this.Project.Images)
						{
							// kick off async thumbnail generation
							_ = imageService.EnsureThumbnailAsync(img.Guid, img.Path);
						}
					}

					ProjectLoaded?.Invoke(this, EventArgs.Empty);
				}
				catch (OperationCanceledException) { /* canceled, ignore */ }
				catch (Exception ex)
				{
					ErrorOccured?.Invoke(this, ex.Message);
				}
			});

			
		}

		public Task UpdateTrainingSettingsAsync(string jsonPath)
		{
			return Task.Run(() =>
			{
				try
				{
					var json = File.ReadAllText(jsonPath);
					var settingsTemp = JsonSerializer.Deserialize<DeepLearningSettings>(json, jsonOptions);

					this.Project.Settings.CopyFrom(settingsTemp);
				}
				catch (Exception ex)
				{
					ErrorOccured?.Invoke(this, ex.Message);
				}
			});
		}

		public Task SaveProjectAsync(ImageRepository imagesRepo)
		{            
			if (string.IsNullOrWhiteSpace(this.ProjectPath) || string.IsNullOrWhiteSpace(this.ProjectName))
				throw new ArgumentException("Project path is empty.");

			return Task.Run(() =>
			{
				try
				{
					// Create folder structure if missing and get paths
					projectOptionsService.EnsureAll(this.ProjectPath);
					var imagesPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Images);
					var annotationsPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Annotations);
					var masksPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Masks);
					var jsonPath = Path.Combine(this.ProjectPath, this.ProjectName + ".json");

					// Copy source images into /images/{guid}.{ext}
					foreach (var item in this.Project.Images)
					{
						var id = item.Guid;
						var srcPath = item.Path;

						if (string.IsNullOrWhiteSpace(srcPath) || !File.Exists(srcPath))
							continue; // skip missing/empty

						var ext = Path.GetExtension(srcPath);
						if (string.IsNullOrEmpty(ext)) ext = ".png"; // fallback

						var dst = Path.Combine(imagesPath, id + ext);
						if (!File.Exists(dst) || !FilesAreSame(srcPath, dst))
							File.Copy(srcPath, dst, overwrite: true);

						item.Path = dst;

						var rtItem = imagesRepo.GetRuntime(id);

						if (rtItem.Annotation != null)
						{
							var path = Path.Combine(annotationsPath, id + ".png");
							rtItem.Annotation.Save(path, ImageFormat.Png);
						}
						if (rtItem.Mask != null)
						{
							var path = Path.Combine(masksPath, id + ".png");
							LabelMask.SavePng8(path, rtItem.Mask, palette: null);
							item.MaskPath = path;
						}
					}

					var json = JsonSerializer.Serialize(this.Project, jsonOptions);
					File.WriteAllText(jsonPath, json);
				}
				catch (Exception ex)
				{
					ErrorOccured?.Invoke(this, ex.Message);
				}
			});
		}

		public Task AddImageAsync(string imagePath)
		{
			return Task.Run(() =>
			{
				try
				{
					var item = EnsureItemForPath(this.Project, imagePath);

					// Kick off thumbnail generation
					imageService.EnsureThumbnailAsync(item.Guid, item.Path).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					ErrorOccured?.Invoke(this, ex.Message);
				}
			});
		}

		public Task RemoveImageAsync(Guid imageId)
		{
			if (this.Project == null) return Task.CompletedTask;

			var toRemove = this.Project.Images.FirstOrDefault(i => i.Guid == imageId);
			if (toRemove != null)
			{
                this.Project.Images.Remove(toRemove);
				imageService.DropThumbnail(imageId);
			}

            // Project not saved yet, just return
            if (this.ProjectPath == null) return Task.CompletedTask;

            var imagesPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Images);
            var annotationsPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Annotations);
            var masksPath = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.Masks);

            // Get all files that contain the substring in their filename, in theory should be only one
            var imagesToDelete = Directory.GetFiles(imagesPath, $"*{imageId}*");
            var masksToDelete = Directory.GetFiles(masksPath, $"*{imageId}*");
            var annotationsToDelete = Directory.GetFiles(annotationsPath, $"*{imageId}*");

            // Delete each file
            foreach (var f in imagesToDelete)
            {
                SafeDeleteFile(f);
            }
            foreach (var f in masksToDelete)
            {
                SafeDeleteFile(f);
            }
            foreach (var f in annotationsToDelete)
            {
                SafeDeleteFile(f);
            }

            return Task.CompletedTask;
		}

        public Task NewProjectAsync(string jsonPath)
        {
            return Task.Run(() =>
            {
				try
				{
					if (string.IsNullOrWhiteSpace(jsonPath))
						throw new ArgumentException("targetProjectJsonPath is empty.", nameof(jsonPath));

					this.ProjectPath = Path.GetDirectoryName(jsonPath);
					this.ProjectName = Path.GetFileNameWithoutExtension(jsonPath);

					// Create folder structure if missing and get paths
					projectOptionsService.EnsureAll(this.ProjectPath);

					this.Project.CopyFrom(new DeepLearningProject());

					// Serialize json project file
					var json = JsonSerializer.Serialize(this.Project, jsonOptions);
					File.WriteAllText(jsonPath, json);
				}
				catch (Exception ex)
				{
					ErrorOccured?.Invoke(this, $"Error creating new project: {ex.Message}");
					throw;
				}
			});
        }

        public IReadOnlyList<(string imagePath, string maskPath)> GetSlicedTrainingPairs(DatasetSplit split)
        {
            if (this.Project == null)
                throw new InvalidOperationException("Project not loaded.");

            var result = new List<(string imagePath, string maskPath)>();

            var slicedImagesFolder = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.SlicedImages);
            var slicedMasksFolder = projectOptionsService.GetFolderPath(this.ProjectPath, ProjectFolderType.SlicedMasks);

            // Gather all GUIDs that are marked for training
            var trainGuids = this.Project.Images
                .Where(img => img.Split == split)
                .Select(img => img.Guid);

            var allSliceImages = Directory.EnumerateFiles(slicedImagesFolder, "*.png");
            var allSliceMasks = Directory.EnumerateFiles(slicedMasksFolder, "*.png"); 


            foreach (var id in trainGuids)
            {
                // Get all slice images/masks for this GUID

                var sliceImages = allSliceImages
                    .Where(i => i.Contains(id.ToString())).ToList();

                var sliceMasks = allSliceMasks
                    .Where(i => i.Contains(id.ToString())).ToList();


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

    }
}
