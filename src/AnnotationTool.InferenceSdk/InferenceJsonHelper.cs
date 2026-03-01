using AnnotationTool.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnnotationTool.InferenceSdk
{
    /// <summary>
    /// Copy/pasted from AnnotationTool.Core.Configuration.JsonConfiguration
    /// ToDo: Refactor to avoid code duplication
    /// </summary>
    public static class InferenceJsonHelper
    {
        public static bool TryDeserializeSavedPackage(string json, out DeepLearningSettings settings, out int numClasses)
        {
            settings = null;
            numClasses = 0;

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            // Serialize enums as strings
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            try
            {
                var pkg = JsonSerializer.Deserialize<SavedModelPackage>(json, jsonOptions);
                if (pkg == null || pkg.Settings == null || pkg.NumClasses <= 0)
                    return false;

                settings = pkg.Settings;
                numClasses = pkg.NumClasses;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}