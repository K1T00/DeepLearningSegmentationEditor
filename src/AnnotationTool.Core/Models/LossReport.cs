

namespace AnnotationTool.Core.Models
{
	public class LossReport
	{
		public int Epoch { get; set; }
		public float TrainLoss { get; set; }
		public float ValidationLoss { get; set; }
		public float LearningRate { get; set; }
	}
}
