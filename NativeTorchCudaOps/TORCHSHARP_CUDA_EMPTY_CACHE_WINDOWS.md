# TorchSharp CUDA empty_cache on Windows (Cpp + CMake + C#)

This document explains **from scratch** how to implement
`torch.cuda.empty_cache()` for **TorchSharp on Windows**, using:

- CMake
- LibTorch
- MSVC Build Tools
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

### 2.1 Visual Studio 2022 C++ Build Tools

CUDA 12.8 only supports **MSVC v143 (VS 2022)**.

Download:
https://visualstudio.microsoft.com/visual-cpp-build-tools/

During installation select:
- ✔ C++ Build Tools
- ✔ MSVC v143 – VS 2022 C++ x64/x86 build tools
- ✔ Windows 10/11 SDK



For older versions: 

https://visualstudio.microsoft.com/de/downloads/

scroll down -> "older downloads"

https://visualstudio.microsoft.com/de/visual-cpp-build-tools/


Verify:

x64 Native Tools Command Prompt for VS 2022 -> command: cl

### 2.2 CUDA Toolkit

Download:
https://developer.nvidia.com/cuda-12-8-0-download-archive

Verify:

x64 Native Tools Command Prompt for VS 2022 -> command: nvcc --version


### 2.3 CMake

Download:
https://cmake.org/download/

Verify:

x64 Native Tools Command Prompt for VS 2022 -> command: cmake --version


### 2.4 LibTorch (build-time only)

LibTorch is required only for headers and .lib files.

e.g. 

https://download.pytorch.org/libtorch/cu128/ e.g. for v12.8  


 -> libtorch-win-shared-with-deps-2.7.1%2Bcu128.zip / libtorch-win-shared-with-deps-2.7.1+cu128.zip e.g. for libtorch 2.7.1


### 3. Build the native DLL

open x64 Native Tools Command Prompt for VS 2022

cd .. build folder

rmdir /s /q build

cmake -S . -B build -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release

cmake --build build

Result:

build\NativeTorchCudaOps.dll

