using AnnotationTool.Core.Services;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models.UNet
{
	public class UNetModelFactory : ISegmentationModelFactory
	{

        public UNetModelFactory()
        {
        }

        public string Name => "UNet";

		public ISegmentationModel Create(IProjectPresenter project, Device device, SegmentationModelConfig cfg)
		{
            return new UNetModel(project, device, cfg);
		}
	}
}
