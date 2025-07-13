using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using TorchSharp;
using TorchSharp.Modules;
using AiOps.AiUtils;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.utils.data;
using static TorchSharp.torch.optim.lr_scheduler.impl;
using static AiOps.AiUtils.LossFunctions;
using static AiOps.AiUtils.HelperFunctions;
using static AiOps.AiUtils.LearningRateOptimizer;


namespace AiOps.AiModels.UNet
{
    public class UNetTrain
	{
	
		private Device device;
		

		public void Run(UNetModelParameter modelPara, UNetData dataPara, IProgress<string> logProgress, IProgress<LossReport> lossProgress, CancellationToken token)
		{
			this.device = new Device(modelPara.TrainOnDevice);

			logProgress.Report(DateTime.Now.ToString("HH:mm:ss tt") + "\r\n" + "Started training UNet model on device: " + this.device);

            // To make RNG reproducible between training runs
            random.manual_seed(42);
            var epoch = 1;
            var valLoss = float.MaxValue;

            // Split: Train -> [0] / Validation -> [1]
            var trainAndValidateImages = SplitList(dataPara.DatasetImages, Convert.ToInt16(dataPara.DatasetImages.Count * modelPara.SplitTrainValidationSet)).ToList();
            var trainAndValidateMasks = SplitList(dataPara.DatasetMasks, Convert.ToInt16(dataPara.DatasetMasks.Count * modelPara.SplitTrainValidationSet)).ToList();
            // At least one training image/mask
            if (trainAndValidateImages.Count == 1)
            {
                trainAndValidateImages.Add(new List<ImageEntry> { dataPara.DatasetImages[0] });
				trainAndValidateMasks.Add(new List<MaskEntry> { dataPara.DatasetMasks[0] });
			}

            if (trainAndValidateImages.Count == 0 | trainAndValidateMasks.Count == 0)
            {
	            throw new TrainImagesException("Train images or masks could not be loaded.");
            }

            using (var model = new UNetModel(modelPara))
            using (var trainingDataSet = new UNetDataset(trainAndValidateImages[0], trainAndValidateMasks[0], modelPara, this.device, UNetAugmentations.BuildTrainingAugmentations(modelPara)))
            using (var validationDataSet = new UNetDataset(trainAndValidateImages[1], trainAndValidateMasks[1], modelPara, this.device, UNetAugmentations.BuildValidationAugmentations(modelPara)))
            using (var trainData = DataLoader(trainingDataSet, modelPara.BatchSize, false, this.device, num_worker: 3))
            using (var validationData = DataLoader(validationDataSet, modelPara.BatchSize, false, this.device, num_worker: 3))
            using (var optimizer = UNetLearningRateOptimizer(model.parameters(), modelPara.LearningRate))
            {
                var lrScheduler = UNetLearningRateScheduler(optimizer, modelPara.MaxEpochs);

                while (epoch <= modelPara.MaxEpochs & valLoss > modelPara.StopAtLoss)
                {
                    var trainLoss = Train(model, optimizer, trainData);

                    if (lrScheduler is ReduceLROnPlateau rlr)
                    {
	                    rlr.step(trainLoss);
					}
                    else
                    {
						lrScheduler.step();
					}

                    valLoss = Validate(model, validationData);

                    lossProgress.Report(new LossReport(){Epoch = epoch, TrainLoss = (float)Math.Round(trainLoss, 4), ValidationLoss = (float)Math.Round(valLoss, 4) });
                    ReportProgress(logProgress, epoch, trainLoss, valLoss, optimizer.ParamGroups.ToList().FirstOrDefault()?.LearningRate);

                    if (epoch % 5 == 0)
                    {
                        model.save(Path.Combine(dataPara.ModelPath, dataPara.ModelWeightsFile));
                        logProgress.Report("Current model saved.");
                    }
                    epoch++;

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
                model.save(Path.Combine(dataPara.ModelPath, dataPara.ModelWeightsFile));
                logProgress.Report(DateTime.Now.ToString("HH:mm:ss tt") + Environment.NewLine + "Training done, model saved.");
            }
		}

		private static float Train(Module<Tensor, Tensor> model, optim.Optimizer optimizer, DataLoader trainData)
		{
			var totalLoss = 0.0f;
			var batchCount = 0;
			model.train();

            using (var d = NewDisposeScope())
            using (var useGrad = enable_grad())
            {
                //using var inferenceMode = inference_mode(false);

                // Run each batch
                foreach (var data in trainData)
                {
                    // Execute model
                    var prediction = model.call(data["data"]);

					// Compute the loss: Comparing result tensor with ground truth tensor
					var computedLoss = CalculateUNetLoss(prediction, data["masks"]);

					totalLoss += computedLoss.ToSingle();
					batchCount++;

					// Clear the gradients before doing the back-propagation
					optimizer.zero_grad();

                    // Do back-propagation, which computes all the gradients
                    computedLoss.backward();

                    // Adjust the weights using the (newly calculated) gradients
                    optimizer.step();

                    d.DisposeEverything();
                }
                return totalLoss / batchCount;
            }
		}

		private static float Validate(Module<Tensor, Tensor> model, DataLoader validationData)
		{
			var totalLoss = 0.0f;
			var batchCount = 0;
			model.eval();

            using (var d = NewDisposeScope())
            using (var noGrad = no_grad())
            {
                //using var inferenceMode = inference_mode(true);

                foreach (var data in validationData)
                {
                    var prediction = model.call(data["data"]);

                    var computedLoss = CalculateUNetLoss(prediction, data["masks"]);

                    totalLoss += computedLoss.ToSingle();
                    batchCount++;

					d.DisposeEverything();
                }
                return totalLoss / batchCount; ;
            }
		}

		private static void ReportProgress(IProgress<string> logger, int epoch, float trainLoss, float valLoss, double? learningRate)
		{
			logger.Report(
				$"Epoch: {epoch}" + Environment.NewLine +
				$"Train loss: {trainLoss}" + Environment.NewLine +
				$"Val loss: {valLoss}" + Environment.NewLine +
				$"Learning rate: {learningRate}");
		}
	}
}
