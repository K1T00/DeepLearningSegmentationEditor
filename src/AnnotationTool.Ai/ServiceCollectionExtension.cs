using AnnotationTool.Ai.Inference;
using AnnotationTool.Ai.Models;
using AnnotationTool.Ai.Models.UNet;
using AnnotationTool.Ai.Training;
using Microsoft.Extensions.DependencyInjection;

namespace AnnotationTool.Ai
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAiServices(this IServiceCollection services)
        {
            // Stateless configuration/services can stay singleton.
            services.AddSingleton<IModelComplexityConfigProvider, ModelComplexityConfigProvider>();
            services.AddSingleton<ISegmentationModelFactory, UNetModelFactory>();

            // Runtime orchestration classes should not be singletons because they coordinate per-run state.
            services.AddTransient<SegmentationTrainer>();
            services.AddTransient<SegmentationTrainingPipeline>();
            services.AddTransient<ISegmentationInferencePipeline, SegmentationInferencePipeline>();

            return services;
        }
    }
}