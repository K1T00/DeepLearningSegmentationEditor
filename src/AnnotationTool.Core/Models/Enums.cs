namespace AnnotationTool.Core.Models
{
    public enum RoiMode
    {
        None,
        Moving,
        ResizingNW,
        ResizingN,
        ResizingNE,
        ResizingW,
        ResizingE,
        ResizingSW,
        ResizingS,
        ResizingSE
    }
    public enum DatasetSplit { Train, Validate, Test }
    public enum ComputeDevice { Cpu, Gpu }
    public enum BrushMode { None, MouseDown, MouseUp }
    public enum PipelineLoopState { Annotation, Training, InferenceResults }
    public enum ModelComplexity { L0, L1, L2, L3, L4, L5 }
    public enum AugmentationMode { Standard, Duplication, FeatureAware }
    public enum SegmentationMode { Binary, Multiclass } // Multilabel not implemented yet

    /// <summary>
    /// Defines the active interaction mode for the main image view.
    /// Exactly one mode should be active at a time.
    /// </summary>
    public enum InteractionMode
    {
        None = 0,

        /// <summary>
        /// Free panning of the image / viewport.
        /// </summary>
        Pan,

        /// <summary>
        /// Painting foreground / feature pixels.
        /// </summary>
        Paint,

        /// <summary>
        /// Erasing painted pixels (background).
        /// </summary>
        Erase,

        /// <summary>
        /// Region-of-interest manipulation (move / resize).
        /// </summary>
        Roi
    }

    public enum SegmentationArchitecture
    {
        UNet,
        UNetPlusPlus
    }
}
