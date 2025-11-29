using AnnotationTool.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnnotationTool.Ai.Inference
{
	public interface ISegmentationInferencePipeline
	{
		/// <summary>
		/// Runs the full inference pipeline asynchronously:
		///   - loads model
		///   - preprocesses image
		///   - performs forward pass
		///   - postprocesses output
		/// </summary>
		Task RunAsync(
			IProjectPresenter project,
			string modelPath,
			IProgress<int> progress,
			CancellationToken ct);
	}
}
