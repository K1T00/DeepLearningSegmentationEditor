using AnnotationTool.Core.Models;
using System.Diagnostics;

namespace AnnotationTool.Ai.Training
{
	public class TrainingStopMonitor
	{
		private readonly TrainingStoppingSettings settings;
		private readonly Stopwatch sw = new Stopwatch();
		private float bestValLoss = float.MaxValue;
		private int epochsSinceImprovement = 0;

		public TrainingStopMonitor(TrainingStoppingSettings settings)
		{
			this.settings = settings;
			sw.Start();
		}

		public double ElapsedMinutes
		{
			get { return sw.Elapsed.TotalMinutes; }
		}

		public float BestValLoss
		{
			get { return bestValLoss; }
		}

		public bool ShouldStop(int epoch, float valLoss, out string reason)
		{
			reason = string.Empty;

			if (valLoss < bestValLoss)
			{
				bestValLoss = valLoss;
				epochsSinceImprovement = 0;
			}
			else
			{
				epochsSinceImprovement++;
			}

			if (settings.MaxIterationCount > 0 && epoch >= settings.MaxIterationCount)
			{
				reason = string.Format("Reached max iteration count ({0}).", settings.MaxIterationCount);
				return true;
			}

			if (settings.IterationWithoutImprovement > 0 &&
				epochsSinceImprovement >= settings.IterationWithoutImprovement)
			{
				reason = string.Format("No improvement for {0} epochs.", settings.IterationWithoutImprovement);
				return true;
			}

			if (settings.MaxTrainingTime > 0 && ElapsedMinutes >= settings.MaxTrainingTime)
			{
				reason = string.Format("Exceeded max training time ({0} minutes).", settings.MaxTrainingTime);
				return true;
			}

			if (settings.MinValidationError > 0 && valLoss <= settings.MinValidationError)
			{
				reason = string.Format("Validation loss reached target ({0}).", settings.MinValidationError);
				return true;
			}

			return false;
		}
	}
}
