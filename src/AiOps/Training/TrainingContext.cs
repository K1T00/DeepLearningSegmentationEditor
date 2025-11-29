using AnnotationTool.Ai.Models;
using AnnotationTool.Core.Models;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler;

namespace AnnotationTool.Ai.Training
{
	/// <summary>
	/// Training context shared between trainer and pipeline.
	/// </summary>
	public class TrainingContext
	{
		public Device Device { get; set; }
		public ISegmentationModel Model { get; set; }
		public (Optimizer optimizer, LRScheduler scheduler) Optimizer { get; set; }
		public DataLoader TrainLoader { get; set; }
		public DataLoader ValLoader { get; set; }
		public DeepLearningSettings Settings { get; set; }
		public TrainingStopMonitor StoppingMonitor { get; set; }
	}
}
