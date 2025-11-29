using System;
using System.Linq;
using AnnotationTool.Core.Models;


namespace AnnotationTool.Core.Services
{
	public static class ProjectStore
	{
		public static ImageItem EnsureItemForPath(DeepLearningProject project, string path)
		{
			var existing = project.Images
				.FirstOrDefault(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
			if (existing != null) return existing;

			var item = new ImageItem
			{
				Path = path,
				Split = DatasetSplit.Train
			};
			project.Images.Add(item);
			return item;
		}

		public static string GetPath(DeepLearningProject project, Guid id)
			=> project.Images.FirstOrDefault(i => i.Guid == id)?.Path;

		public static ImageItem FindById(DeepLearningProject project, Guid id)
			=> project.Images.FirstOrDefault(i => i.Guid == id);

        public static ImageItem FindByPath(DeepLearningProject project, string path)
            => project.Images.FirstOrDefault(i => i.Path == path);

    }
}
