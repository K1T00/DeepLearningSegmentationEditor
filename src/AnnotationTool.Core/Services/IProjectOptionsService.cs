using AnnotationTool.Core.IO;

namespace AnnotationTool.Core.Services
{
	public interface IProjectOptionsService
	{
		/// <summary>
		/// Returns the full path to a standard folder within the project.
		/// </summary>
		string GetFolderPath(string projectRoot, ProjectFolderType type);

		/// <summary>
		/// Ensures that all standard folders exist under the project root.
		/// </summary>
		void EnsureAll(string projectRoot);

		/// <summary>
		/// Ensures a single folder exists.
		/// </summary>
		void EnsureFolder(string projectRoot, ProjectFolderType type);

		(string, string) GetModelMetadataFilePaths(string folderPath);

		string ExtractMetadataFilePath(string modelPath);

		string GetModelsSubFileName();

		string GetTrainingSettingsSubFileName();
	}
}
