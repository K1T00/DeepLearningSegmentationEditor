using System;
using System.Runtime.InteropServices;

namespace AnnotationTool.Ai.Utils.CudaOps
{
    /// <summary>
    /// https://github.com/dotnet/TorchSharp/pull/1524
    /// ToDo: Add mem_get_info() memory_reserved() ...
    /// </summary>
    public static class NativeTorchCudaOps
    {
        private const string DllName = "NativeTorchCudaOps.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr torch_check_last_err();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void torch_empty_cache();

        // Public safe wrapper
        public static void EmptyCudaCache()
        {
            torch_empty_cache();
            CheckLastError();
        }

        private static void CheckLastError()
        {
            var errPtr = torch_check_last_err();
            if (errPtr != IntPtr.Zero)
            {
                string msg = Marshal.PtrToStringAnsi(errPtr);
                throw new InvalidOperationException(msg);
            }
        }
    }
}