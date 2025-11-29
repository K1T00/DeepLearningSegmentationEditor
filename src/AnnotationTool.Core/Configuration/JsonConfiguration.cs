using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnnotationTool.Core.Configuration
{
	public static class JsonConfiguration
	{
		public static IServiceCollection AddJsonConfiguration(this IServiceCollection services)
		{
			var jsonOptions = new JsonSerializerOptions
			{
				// Human-readable formatting
				WriteIndented = true,

				// camelCase property names (same as your old CamelCaseNamingStrategy)
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

				// Ignore null values
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

				// Allow trailing commas in JSON files (optional but convenient)
				AllowTrailingCommas = true,

				// Avoid exceptions on comments in JSON files
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			// Serialize enums as strings (recommended for readability & compatibility)
			jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

			// You may add custom converters here later if needed
			// jsonOptions.Converters.Add(new MyCustomConverter());

			services.AddSingleton(jsonOptions);

			return services;
		}
	}
}
