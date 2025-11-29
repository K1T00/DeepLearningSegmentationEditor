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

        Task LoadProjectAsync(string jsonPath, ImageRepository imagesRepo);
		Task UpdateTrainingSettingsAsync(string jsonPath);
		Task SaveProjectAsync(ImageRepository imagesRepo);
		Task AddImageAsync(string imagePath);
		Task RemoveImageAsync(Guid imageId);
        Task NewProjectAsync(string jsonPath);
        IReadOnlyList<(string imagePath, string maskPath)> GetSlicedTrainingPairs(DatasetSplit split);
    }
}
