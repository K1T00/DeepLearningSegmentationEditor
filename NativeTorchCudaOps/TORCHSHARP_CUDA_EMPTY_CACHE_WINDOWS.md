# TorchSharp CUDA empty_cache on Windows (C++ + CMake + C#)

This document explains **from scratch** how to implement
`torch.cuda.empty_cache()` for **TorchSharp on Windows**, using:

- CMake
- LibTorch
- MSVC Build Tools (no Visual Studio IDE)
- A tiny native C++ shim DLL
- C# P/Invoke

No Python is required.


## 1. Problem statement

TorchSharp does not expose `torch.cuda.empty_cache()`.

PyTorch does **not export a callable C ABI** for this function, so it cannot be
called directly from C#.

The solution is to build a **small native C++ DLL** that calls:

c10::cuda::CUDACachingAllocator::emptyCache()


and expose it via a C ABI, then call it from C#.


## 2. Required software

### 2.1 Visual Studio 2022 C++ Build Tools (REQUIRED)

CUDA 12.8 only supports **MSVC v143 (VS 2022)**.

Download:
https://visualstudio.microsoft.com/visual-cpp-build-tools/

During installation select:
- ✔ C++ Build Tools
- ✔ MSVC v143 – VS 2022 C++ x64/x86 build tools
- ✔ Windows 10/11 SDK

Verify:
cl

### 2.2 CUDA Toolkit 12.8

Download:
https://developer.nvidia.com/cuda-12-8-0-download-archive

Verify:

nvcc --version

### 2.3 CMake

Download:
https://cmake.org/download/

Verify:

cmake --version


### 3. Download LibTorch (build-time only)

LibTorch is required only for headers and .lib files.

e.g. 

https://download.pytorch.org/libtorch/cu128/

### 4. Build the native DLL

Open
x64 Native Tools Command Prompt for VS 2022

rmdir /s /q build
cmake -S . -B build -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release
cmake --build build

Result

build\NativeTorchCudaOps.dll

