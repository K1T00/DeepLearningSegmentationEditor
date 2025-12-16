cmake_minimum_required(VERSION 3.21)

# ------------------------------------------------------------
# Project
# ------------------------------------------------------------
project(NativeTorchCudaOps LANGUAGES CXX)

# ------------------------------------------------------------
# C++ standard
# ------------------------------------------------------------
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ------------------------------------------------------------
# Build type (IMPORTANT on Windows)
# Default to Release if not explicitly specified
# ------------------------------------------------------------
if (NOT CMAKE_BUILD_TYPE)
    set(
        CMAKE_BUILD_TYPE
        Release
        CACHE STRING "Build type (Release or Debug)"
        FORCE
    )
endif()

message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")

# ------------------------------------------------------------
# MSVC runtime (VERY IMPORTANT)
# Force /MD for Release, /MDd for Debug
# This avoids CRT mismatches with TorchSharp
# ------------------------------------------------------------
if (MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
endif()

# ------------------------------------------------------------
# LibTorch
# ------------------------------------------------------------
# IMPORTANT:
# This must point to the LibTorch you downloaded:
# libtorch-win-shared-with-deps-2.7.1+cu128
#
# You can override this on the command line if needed.
#
if (NOT Torch_DIR)
    set(
        Torch_DIR
        "D:/_DEV/Ai/libtorch-win-shared-with-deps-2.7.1+cu128/libtorch/share/cmake/Torch"
        CACHE PATH "Path to LibTorch CMake config"
    )
endif()

find_package(Torch REQUIRED)

message(STATUS "Found Torch version: ${Torch_VERSION}")

# ------------------------------------------------------------
# Library target
# ------------------------------------------------------------
add_library(NativeTorchCudaOps SHARED
    nativeops.cpp
)

# ------------------------------------------------------------
# Include directories
# ------------------------------------------------------------
target_include_directories(NativeTorchCudaOps PRIVATE
    ${TORCH_INCLUDE_DIRS}
)

# ------------------------------------------------------------
# Link against LibTorch components
# ------------------------------------------------------------
target_link_libraries(NativeTorchCudaOps PRIVATE
    torch
    torch_cuda
    c10
    c10_cuda
)

# ------------------------------------------------------------
# Windows DLL export behavior
# ------------------------------------------------------------
if (WIN32)
    set_target_properties(NativeTorchCudaOps PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
endif()

# ------------------------------------------------------------
# Output name (optional but explicit)
# ------------------------------------------------------------
set_target_properties(NativeTorchCudaOps PROPERTIES
    OUTPUT_NAME "NativeTorchCudaOps"
)

# ------------------------------------------------------------
# Helpful summary
# ------------------------------------------------------------
message(STATUS "Torch include dirs: ${TORCH_INCLUDE_DIRS}")
message(STATUS "Torch libraries: ${TORCH_LIBRARIES}")
