using AnnotationTool.Ai;
using AnnotationTool.App.Forms;
using AnnotationTool.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;



namespace AnnotationTool.App
{
	public static class ServiceRegistration
	{

        // Only works with newer .NET versions that support Generic Host in WinForms apps
        //public static IHost BuildHost()
        //{
        //    return Host.CreateDefaultBuilder()
        //        .ConfigureLogging(logging =>
        //        {
        //            logging.ClearProviders();
        //            logging.AddDebug();
        //            logging.AddConsole();
        //        })
        //        .ConfigureServices((context, services) =>
        //        {
        //            // Add module registrations
        //            services.AddCoreServices();
        //            services.AddAiServices();

        //            // Forms
        //            services.AddSingleton<MainForm>();  // main window = singleton
        //            services.AddTransient<TrainingForm>();
        //            services.AddTransient<InferenceForm>();
        //            services.AddTransient<TrainedModelsForm>();

        //            // Factories for forms
        //            services.AddTransient<Func<TrainingForm>>(sp => () => sp.GetRequiredService<TrainingForm>());
        //            services.AddTransient<Func<InferenceForm>>(sp => () => sp.GetRequiredService<InferenceForm>());
        //            services.AddTransient<Func<TrainedModelsForm>>(sp => () => sp.GetRequiredService<TrainedModelsForm>());
        //        })
        //        .Build();
        //}


        public static IServiceProvider ConfigureServices()
		{

			var services = new ServiceCollection();

			// Logging
			services.AddLogging(builder =>
			{
				builder.SetMinimumLevel(LogLevel.Information);
			});

			// Add modules
			services.AddCoreServices();
			services.AddAiServices();

			// Forms
			services.AddTransient<MainForm>();
			services.AddTransient<TrainingForm>();
			//services.AddTransient<FeaturesEditor>();
			services.AddTransient<InferenceForm>();
			services.AddTransient<TrainedModelsForm>();

			// DI can resolve Func<Form> -> () => sp.GetRequiredService<Form>()
			services.AddTransient<Func<TrainingForm>>(sp => () => sp.GetRequiredService<TrainingForm>());
			services.AddTransient<Func<InferenceForm>>(sp => () => sp.GetRequiredService<InferenceForm>());
			services.AddTransient<Func<TrainedModelsForm>>(sp => () => sp.GetRequiredService<TrainedModelsForm>());

			return services.BuildServiceProvider();
		}
	}
}
