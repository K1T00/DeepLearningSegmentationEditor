using System;

namespace AiOps.AiUtils
{

	[Serializable]
	public class TrainImagesException : Exception
	{
		public TrainImagesException() { }

		public TrainImagesException(string message)
			: base(message)
		{

		}
	}
}
