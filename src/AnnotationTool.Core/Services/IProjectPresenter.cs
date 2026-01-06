using AnnotationTool.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnnotationTool.Core.Services
{
    public interface IProjectPresenter
    {

        event EventHandler ProjectLoaded;
        event EventHandler<string> ErrorOccured;

        DeepLearningProject Project { get; }
        string ProjectPath { get; set; }
        string ProjectName { get; set; }

        void LoadProject(string jsonPath, ImageRepository imagesRepo);
        void UpdateTrainingSettings(string jsonPath);
        void SaveProject(ImageRepository imagesRepo);
        void AddImage(string imagePath);
        void RemoveImage(Guid imageId);
        void NewProject(string jsonPath);
        IReadOnlyList<(string imagePath, string maskPath)> GetSlicedTrainingPairs(DatasetSplit split);
    }
}
