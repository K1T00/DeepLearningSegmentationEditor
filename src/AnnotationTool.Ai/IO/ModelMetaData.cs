using AnnotationTool.Ai.Training;
using AnnotationTool.Core.IO;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using System.Text.Json;

namespace AnnotationTool.Ai.IO
{
	public static class ModelMetaData
	{
		internal static Task SaveModelAndMetadataAsync(
			Module<Tensor, Tensor> model, 
			IProjectPresenter project,
			IProjectOptionsService projectOptionsService, 
			JsonSerializerOptions jsonOptions,
			ILogger<SegmentationTrainingPipeline> logger)
		{
			return Task.Run(() =>
			{
				try
				{
					var (modelPath, jsonSettingsPath) =
						projectOptionsService.GetModelMetadataFilePaths(projectOptionsService.GetFolderPath(project.ProjectPath, ProjectFolderType.Models));

					model.save(modelPath);

					var projectJson = JsonSerializer.Serialize(project.Project.Settings, jsonOptions);
					File.WriteAllText(jsonSettingsPath, projectJson);

					logger.LogInformation("Model saved.");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error saving model and metadata.");
				}
			});
		}
	}
}
