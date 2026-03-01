using AnnotationTool.Core.Configuration;
using AnnotationTool.Core.IO;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace AnnotationTool.Core.Services
{
    public class ProjectOptionsService : IProjectOptionsService
    {
        private readonly ProjectOptions options;
        private readonly Dictionary<ProjectFolderType, string> folderMap;

        public ProjectOptionsService(IOptions<ProjectOptions> options)
        {
            this.options = options.Value;

            folderMap = new Dictionary<ProjectFolderType, string>
            {
                { ProjectFolderType.Images, this.options.ImagesFolder },
                { ProjectFolderType.Masks, this.options.MasksFolder },
                { ProjectFolderType.Annotations, this.options.AnnotationsFolder },
                { ProjectFolderType.Results, this.options.ResultsFolder },
                { ProjectFolderType.Logs, this.options.LogsFolder },
                { ProjectFolderType.Models, this.options.ModelsSubFolder },
                { ProjectFolderType.SlicedImages, this.options.SlicedImagesSubFolder },
                { ProjectFolderType.SlicedMasks, this.options.SlicedMasksSubFolder },
                { ProjectFolderType.MasksHeatmaps, this.options.MasksHeatmapsSubFolder }
            };
        }

        public string GetFolderName(ProjectFolderType type)
        {
            if (!folderMap.TryGetValue(type, out var relative))
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Unhandled folder type: {type}");
            }

            return relative;
        }

        public string GetImageExtension()
        {
            return options.ImageExtension;
        }

        public string GetModelExtension()
        {
            return options.ModelExtension;
        }

        public string GetFolderPath(string projectRoot, ProjectFolderType type)
        {
            if (!folderMap.TryGetValue(type, out var relative))
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Unhandled folder type: {type}");
            }

            return Path.Combine(projectRoot, relative);
        }

        public void EnsureFolder(string projectRoot, ProjectFolderType type)
        {
            var path = GetFolderPath(projectRoot, type);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void EnsureAll(string projectRoot)
        {
            foreach (ProjectFolderType type in Enum.GetValues(typeof(ProjectFolderType)))
                EnsureFolder(projectRoot, type);
        }

        public (string, string) GetModelMetadataFilePaths(string folderPath)
        {
            var timestamp = DateTime.Now.ToString(options.DateTimeFormat);
            var modelPath = Path.Combine(folderPath, options.ModelFileName + timestamp + ".bin");
            var jsonSettingsPath = Path.Combine(folderPath, options.TrainingSettingsFileName + timestamp + ".json");

            return (modelPath, jsonSettingsPath);
        }

        public string ExtractMetadataFilePath(string modelPath)
        {

            var directory = Path.GetDirectoryName(modelPath);
            var fileName = Path.GetFileNameWithoutExtension(modelPath);
            if (fileName.StartsWith(options.ModelFileName))
            {
                var timestamp = fileName.Substring(options.ModelFileName.Length);
                var jsonFileName = options.TrainingSettingsFileName + timestamp + ".json";
                return Path.Combine(directory, jsonFileName);
            }
            else
            {
                throw new ArgumentException("Model file name does not follow the expected naming convention.", nameof(modelPath));
            }
        }

        public string GetModelsSubFileName()
        {
            return options.ModelFileName;
        }

        public string GetTrainingSettingsSubFileName()
        {
            return options.TrainingSettingsFileName;
        }
    }

    public sealed class ProjectPaths
    {
        public string Root { get; }
        public string Images { get; }
        public string Masks { get; }
        public string Annotations { get; }
        public string Results { get; }
        public string SlicedImages { get; }
        public string SlicedMasks { get; }
        public string MasksHeatmaps { get; }
        public string Models { get; }
        public string ModelSub { get; }
        public string ModelSettingsSub { get; }
        public string JsonPath { get; }
        public string ImagesExt { get; }
        public string ModelExt { get; }

        public ProjectPaths(string projectRoot, string projectName, IProjectOptionsService options)
        {
            this.Root = projectRoot;
            this.Images = options.GetFolderPath(projectRoot, ProjectFolderType.Images);
            this.Masks = options.GetFolderPath(projectRoot, ProjectFolderType.Masks);
            this.Annotations = options.GetFolderPath(projectRoot, ProjectFolderType.Annotations);
            this.Results = options.GetFolderPath(projectRoot, ProjectFolderType.Results);
            this.SlicedImages = options.GetFolderPath(projectRoot, ProjectFolderType.SlicedImages);
            this.SlicedMasks = options.GetFolderPath(projectRoot, ProjectFolderType.SlicedMasks);
            this.MasksHeatmaps = options.GetFolderPath(projectRoot, ProjectFolderType.MasksHeatmaps);
            this.Models = options.GetFolderPath(projectRoot, ProjectFolderType.Models);
            this.ModelSub = options.GetModelsSubFileName();
            this.ModelSettingsSub = options.GetTrainingSettingsSubFileName();
            this.JsonPath = Path.Combine(projectRoot, projectName + ".json");
            this.ImagesExt = options.GetImageExtension();
            this.ModelExt = options.GetModelExtension();
        }
    }

}
