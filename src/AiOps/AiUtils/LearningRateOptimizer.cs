using System;
using System.Collections.Generic;
using TorchSharp.Modules;
using static TorchSharp.torch.optim;
using static TorchSharp.torch.optim.lr_scheduler;


namespace AiOps.AiUtils
{
    public static class LearningRateOptimizer
    {
	    public static Optimizer UNetLearningRateOptimizer(IEnumerable<Parameter> parameters, double learningRate)
	    {
		    return SGD(parameters, learningRate, 0.9, 0, learningRate / 10); // lr: 0.05

			//return AdamW(parameters, learningRate); // lr: 0.001
		 //   return Adam(parameters, learningRate); // lr: 0.001
		}

		public static LRScheduler UNetLearningRateScheduler(Optimizer optimizer, int maxEpochs)
		{
			return StepLR(optimizer, 15, 0.75); // lr: 0.05

			//return StepLR(optimizer, Convert.ToInt32(0.8 * maxEpochs), 0.6);
			//return ReduceLROnPlateau(optimizer, "min", 0.5, 5, verbose: true);
			//return OneCycleLR(optimizer, 1e-3, 5, 90, anneal_strategy: impl.OneCycleLR.AnnealStrategy.Cos);
			//return CosineAnnealingLR(optimizer, 25);
		}
	}
}