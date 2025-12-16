using System;
using System.Runtime.InteropServices;

internal static class NativeTorchCudaOps
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
        IntPtr errPtr = torch_check_last_err();
        if (errPtr != IntPtr.Zero)
        {
            string msg = Marshal.PtrToStringAnsi(errPtr);
            throw new InvalidOperationException(msg);
        }
    }
}
