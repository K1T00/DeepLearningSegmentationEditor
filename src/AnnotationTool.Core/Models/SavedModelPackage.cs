using System;

namespace AnnotationTool.Core.Models
{
    /// <summary>
    /// // Wrapper to save relevant metadata (that may be used for sdk)
    /// </summary>
    public class SavedModelPackage
    {
        public DeepLearningSettings Settings { get; set; }
        public int NumClasses { get; set; } // Features
    }
}
