using System;
using System.Collections.Generic;
using static TorchSharp.torch;
using static TorchSharp.torch.utils.data;


namespace AnnotationTool.Ai.Utils
{

    /// <summary>
    /// Workarount to concatenate multiple SegmentationDataset instances into one.
    /// 
    /// PR: https://github.com/dotnet/TorchSharp/pull/1411
    /// </summary>
    public class ConcatSegmentationDataset : Dataset
    {
        private readonly List<Dataset> datasets;
        private readonly List<long> cumulativeSizes;

        public ConcatSegmentationDataset(params Dataset[] datasets)
        {
            this.datasets = new List<Dataset>(datasets);
            this.cumulativeSizes = new List<long>();

            long running = 0;
            foreach (var ds in datasets)
            {
                running += ds.Count;
                cumulativeSizes.Add(running);
            }
        }

        public override long Count
        {
            get
            {
                return this.cumulativeSizes[this.cumulativeSizes.Count - 1];
            }
        }

        public override Dictionary<string, Tensor> GetTensor(long index)
        {
            for (int i = 0; i < datasets.Count; i++)
            {
                if (index < cumulativeSizes[i])
                {
                    long localIndex = i == 0 ? index : index - cumulativeSizes[i - 1];
                    return datasets[i].GetTensor(localIndex);
                }
            }
            throw new IndexOutOfRangeException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var ds in datasets)
                ds.Dispose();
        }
    }
}
