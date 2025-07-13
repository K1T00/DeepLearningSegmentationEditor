using System;
using System.Collections.Generic;
using OpenCvSharp;
using static TorchSharp.torch;
using static AiOps.AiUtils.TensorImageCalcHelper;
using AiOps.AiUtils;

namespace AiOps.AiModels.UNet
{
    /// <summary>
    /// Represents a dataset for training and evaluating U-Net models, providing access to image and mask tensors.
    /// </summary>
    /// <remarks>This dataset is designed to handle paired image and mask data, applying optional
    /// transformations during retrieval. It supports grayscale and RGB images, as well as multi-feature masks, based on
    /// the provided model parameters. The dataset ensures that each image has a corresponding mask or set of masks, as
    /// required by the U-Net model.</remarks>
    public class UNetDataset : utils.data.Dataset
    {
        private readonly List<Tensor> data = new List<Tensor>();
        private readonly List<Tensor> masks = new List<Tensor>();
        private readonly IPairedTransform transforms;
		private readonly int count;
		private readonly Device device;
		private readonly List<ImageEntry> imagesPaths;
		private readonly List<MaskEntry> masksPaths;
		private readonly UNetModelParameter modelPara;

		public UNetDataset(List<ImageEntry> imagesPaths, List<MaskEntry> masksPaths, UNetModelParameter modelPara, Device device, IPairedTransform transforms)
		{
			this.imagesPaths = imagesPaths;
			this.masksPaths = masksPaths;
			this.device = device;
			this.modelPara = modelPara;
			this.transforms = transforms;
			this.count = ReadFiles();
		}

        private int ReadFiles()
        {
            // For every image there needs to be one mask per feature
            for (var imIdx = 0; imIdx < imagesPaths.Count; imIdx++)
            {
                // Images
                if (this.modelPara.TrainImagesAsGreyscale)
                {
                    using (var img = Cv2.ImRead(imagesPaths[imIdx].Path, ImreadModes.Grayscale))
                    {
                        data.Add(GreyImageToTensor(img, this.device, this.modelPara.TrainPrecision));
                    }
                }
                else
                {
                    using (var img = Cv2.ImRead(imagesPaths[imIdx].Path, ImreadModes.Color))
                    {
                        data.Add(RgbImageToTensor(img, this.device, this.modelPara.TrainPrecision));
                    }
                }
                // Masks
                if (this.modelPara.Features == 1)
                {
                    using (var mskGrey = Cv2.ImRead(masksPaths[imIdx].Path, ImreadModes.Grayscale))
                    {
                        masks.Add(MaskToTensor(mskGrey, this.device, this.modelPara.TrainPrecision));
                    }
                }
                else
                {
                    var tensorLabelsAr = new Tensor[this.modelPara.Features];

                    for (var ma = 0; ma < this.modelPara.Features; ma++)
                    {
                        using (var mskGrey = Cv2.ImRead(masksPaths[imIdx * this.modelPara.Features + ma].Path, ImreadModes.Unchanged))
                        {
                            tensorLabelsAr[ma] = MaskToTensor(mskGrey, this.device, this.modelPara.TrainPrecision);
                        }
                    }
                    masks.Add(cat(tensorLabelsAr, 0));
                }
            }
            return data.Count;
        }

		/// <summary>
		/// Get tensor according to index
		/// </summary>
		/// <param name="index">Index for tensor</param>
		/// <returns>Tensors of index.</returns>
		public override Dictionary<string, Tensor> GetTensor(long index)
		{
			var image = data[(int)index];
			var mask = masks[(int)index];

			if (transforms != null)
			{
				(image, mask) = transforms.Apply(image, mask);
			}

			return new Dictionary<string, Tensor>
			{
				{ "data", image },
				{ "masks", mask }
			};
		}

		public override long Count => count;

		protected override void Dispose(bool disposing)
		{
			if (!disposing) return;
			data.ForEach(d => d.Dispose());
			masks.ForEach(d => d.Dispose());
		}
	}
}
