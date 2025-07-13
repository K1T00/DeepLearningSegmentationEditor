using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;

namespace AiOps.AiModels.SimpleModel
{
	public sealed class SimpleModel : Module<Tensor, Tensor>
	{

        private readonly Module<Tensor, Tensor> lin1;
        private readonly Module<Tensor, Tensor> lin2;
		private readonly Module<Tensor, Tensor> lin3;

        public SimpleModel(Device device, int inputSize) : base(nameof(SimpleModel))
		{
            lin1 = Linear(inputSize, 100, true, device: device);
            lin2 = Linear(100, 10, true, device: device);
            lin3 = Linear(10, 1, true, device: device);

            RegisterComponents();

            this.to(device: device);
		}

		public override Tensor forward(Tensor input)
		{
			using (var _ = NewDisposeScope())
			{
				var layer1 = lin1.call(input);
				var layer2 = relu(layer1);
				var layer3 = lin2.call(layer2);
				var layer4 = relu(layer3);

				return lin3.call(layer4).MoveToOuterDisposeScope(); // Return last layer result
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lin1.Dispose();
				lin2.Dispose();
				lin3.Dispose();
				ClearModules();
			}
			base.Dispose(disposing);
		}

	}
}
