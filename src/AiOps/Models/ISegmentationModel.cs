using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Models
{
	/// <summary>
	/// Abstraction for any segmentation model (UNet, UNet++, etc.).
	/// Wraps the underlying TorchSharp module and persistence operations.
	/// </summary>
	public interface ISegmentationModel
	{
		/// <summary>
		/// Forward pass for a batch of images.
		/// </summary>
		Tensor forward(Tensor input);

		/// <summary>
		/// Returns the underlying TorchSharp module (for optimizer, etc.).
		/// </summary>
		Module<Tensor, Tensor> AsModule();

		/// <summary>
		/// Move model to the given device.
		/// </summary>
		void To(Device device);

		/// <summary>
		/// Save model weights to file.
		/// </summary>
		void Save(string filePath);

		/// <summary>
		/// Load model weights from file.
		/// </summary>
		void Load(string filePath);
	}
}
