using AnnotationTool.Core.Configuration;
using AnnotationTool.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AnnotationTool.Core
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddCoreServices(this IServiceCollection services)
		{
			// Options and configurations
			services.AddJsonConfiguration();
			services.Configure<ProjectOptions>(o => { });

			// Core services
			services.AddSingleton<IImageService, ImageService>();
			services.AddSingleton<IProjectOptionsService, ProjectOptionsService>();

			
			services.AddSingleton<IProjectPresenter, ProjectPresenter>(sp =>
			{
				return new ProjectPresenter(
					sp.GetRequiredService<IImageService>(),
					sp.GetRequiredService<JsonSerializerOptions>(),
					sp.GetRequiredService<IProjectOptionsService>()
				);
			});

			services.AddLogging();

			return services;
		}
	}
}
