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
	public enum ModelComplexity{ Low, Medium, High}
    public enum AugmentationMode { Standard, Duplication, FeatureAware }

}
