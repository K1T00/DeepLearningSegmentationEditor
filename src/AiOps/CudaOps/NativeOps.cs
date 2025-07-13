using System;
using System.Runtime.InteropServices;

namespace AiOps.CudaOps
{
	public static class NativeOps
	{

		[DllImport("NativeTorchCudaOps.dll")]
		internal static extern IntPtr torch_check_last_err();

		[DllImport("NativeTorchCudaOps.dll")]
		internal static extern void torch_empty_cache();

		public static void CheckForErrors()
		{
			var error = torch_check_last_err();
			if (error != IntPtr.Zero)
				throw new ExternalException(Marshal.PtrToStringAnsi(error));
		}

		public static void EmptyCudaCache()
		{
			torch_empty_cache();
			CheckForErrors();
		}
	}
}
