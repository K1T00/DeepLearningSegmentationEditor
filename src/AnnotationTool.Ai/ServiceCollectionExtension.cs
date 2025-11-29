using AnnotationTool.Ai.Models.UNet;
using AnnotationTool.Ai.Inference;
using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Training;
using Microsoft.Extensions.DependencyInjection;

namespace AnnotationTool.Ai
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddAiServices(this IServiceCollection services)
		{
            // Model factories
            services.AddSingleton<IModelComplexityConfigProvider, ModelComplexityConfigProvider>();
            services.AddSingleton<ISegmentationModelFactory, UNetModelFactory>();

			// Training + inference pipeline
			services.AddSingleton<SegmentationTrainer>();
			services.AddSingleton<SegmentationTrainingPipeline>();
			services.AddSingleton<ISegmentationInferencePipeline, SegmentationInferencePipeline>();

			// Other AI-related utilities
			services.AddSingleton<TrainingContext>();
			services.AddSingleton<TrainingStopMonitor>();

			return services;
		}
	}
}
