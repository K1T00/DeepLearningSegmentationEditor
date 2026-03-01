using AnnotationTool.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AnnotationTool.Core.Services
{
    public interface IProjectPresenter
    {
        event EventHandler ProjectLoaded;
        event EventHandler<string> ErrorOccured;

        DeepLearningProject Project { get; }
        string ProjectPath { get; set; }
        ProjectPaths Paths { get; }

        void LoadProject(string jsonPath);
        void UpdateTrainingSettings(string jsonPath);
        void SaveProject(ImageRepository imagesRepo);
        ImageItem AddImage(string imagePath, Size imageSize);
        void RemoveImage(Guid imageId);
        void NewProject(string jsonPath);
        void SaveJsonFile();
        IReadOnlyList<(string imagePath, string maskPath)> GetSlicedTrainingPairs(DatasetSplit split);
        string ResolveImagePath(Guid imageGuid);
        bool TryGetImageItem(Guid id, out ImageItem imageItem);
    }
}
