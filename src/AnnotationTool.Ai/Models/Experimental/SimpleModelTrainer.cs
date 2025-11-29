using System;
using System.Threading;
using AnnotationTool.Core.Models;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;


namespace AnnotationTool.Ai.Models.Experimental
{
	/// <summary>
	/// This is just for testing a simple model training loop
	/// </summary>
	public class SimpleModelTrainer
	{
		private Device device;

		public void Run(DeviceType deviceType, IProgress<string> logProgress, IProgress<LossReport> lossProgress, CancellationToken token)
		{
			device = new Device(deviceType);

			logProgress.Report(DateTime.Now.ToString("HH:mm:ss tt") + "\r\n" + "Started training simple model on device: " + device);

			// Create random Data
            var dataBatch = rand(32, 1000, device: device); // Input data
	        var targetBatch = rand(32, 1, device: device); // Ground truth

	        // Init model
	        var model = new SimpleModel(device, 1000);

			// Loss function compares the output of the model with the ground truth of the labels
			var mseLoss = MSELoss();

	        var optimizer = optim.SGD(model.parameters(), 0.01f);

	        //  Add variable learning rate (SGD optimizer has fixed learning rate)
	        var scheduler = optim.lr_scheduler.StepLR(optimizer, 25, 0.95);

            const int epochs = 100;
            var epoch = 1;

            while (epoch < epochs)
            {
	            var loss = float.MaxValue;

				// Run one batch and compute the loss by comparing the result to the ground truth
				var modelLoss = model.call(dataBatch);
				var outputLoss = mseLoss.call(modelLoss, targetBatch);

                loss = outputLoss.ToSingle();

                // Clear the gradients before doing the back-propagation
                model.zero_grad();

                // Do back-propagation, which computes all the gradients
                outputLoss.backward();

                // Adjust the weights using the gradients
                optimizer.step();
                scheduler.step();

				lossProgress.Report(new LossReport(){Epoch = epoch, TrainLoss = loss, ValidationLoss = loss });

                epoch++;

		        Thread.Sleep(25);

		        if (token.IsCancellationRequested)
		        {
			        break;
		        }
	        }
            logProgress.Report(DateTime.Now.ToString("HH:mm:ss tt") + "\r\n" + "Training simple model done.");
		}
	}
}
