import os
import sys
import subprocess
import shutil
import logging
from pathlib import Path

import torch
import torch.utils.cpp_extension as torch_ext
import setuptools._distutils._msvccompiler as msvc


# c10::cuda::CUDACachingAllocator::emptyCache() -> cudaFree in CUDA API

# Constants
BUILD_NAME = "NativeTorchCudaOps"
COMPUTE_CAPABILITIES = {75, 80, 86, 89, 90}

# Setup logging
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")


def get_compute_capabilities():
    for i in range(torch.cuda.device_count()):
        major, minor = torch.cuda.get_device_capability(i)
        cc = major * 10 + minor
        if cc < 75:
            raise RuntimeError("GPUs with compute capability less than 7.5 are not supported.")

    capability_flags = []
    for cap in COMPUTE_CAPABILITIES:
        capability_flags += ["-gencode", f"arch=compute_{cap},code=sm_{cap}"]

    return capability_flags


def check_nvcc_version():
    try:
        result = subprocess.run(
            ["nvcc", "--version"],
            capture_output=True,
            text=True,
            check=True
        )
        logging.info("nvcc version detected:\n%s", result.stdout)
        return result.stdout
    except FileNotFoundError:
        raise EnvironmentError("'nvcc' not found. Ensure CUDA is installed and 'nvcc' is in the system PATH.")
    except subprocess.CalledProcessError as e:
        raise EnvironmentError(f"Failed to run 'nvcc --version'. Details: {e}") from e


def append_env(env_var: str, paths: str):
    existing_paths = os.environ.get(env_var, "").split(os.pathsep)
    for path in paths.split(os.pathsep):
        if path and path not in existing_paths:
            existing_paths.insert(0, path)
    os.environ[env_var] = os.pathsep.join(existing_paths)


def print_includes_and_exit():
    import json
    logging.info("Torch include and library paths:")
    print(json.dumps(torch_ext.include_paths(cuda=True), indent=2))
    print(json.dumps(torch_ext.library_paths(cuda=True), indent=2))
    sys.exit(0)


def clean_build(build_path: Path):
    if build_path.exists():
        shutil.rmtree(build_path, ignore_errors=True)
        logging.info("Cleaned build directory: %s", build_path)
    else:
        logging.info("Build directory does not exist, nothing to clean.")
    sys.exit(0)


def setup_environment(vc_env):
    append_env("path", vc_env.get("path", ""))
    append_env("include", vc_env.get("include", ""))
    append_env("lib", vc_env.get("lib", ""))


# flags for CUDA compiler nvcc
def build_extension(build_path: Path, sources):
    extra_cflags = ["/Ox", "/std:c++17"]
    extra_cuda_cflags = [
        "-O3",
        "-std=c++17",
        "-DUSE_NVTX=ON",
        "-U__CUDA_NO_HALF_OPERATORS__",
        "-U__CUDA_NO_HALF_CONVERSIONS__",
        "-U__CUDA_NO_BFLOAT16_OPERATORS__",
        "-U__CUDA_NO_BFLOAT16_CONVERSIONS__",
        "-U__CUDA_NO_BFLOAT162_OPERATORS__",
        "-U__CUDA_NO_BFLOAT162_CONVERSIONS__",
        "--use_fast_math",
    ]
    extra_cuda_cflags += get_compute_capabilities()

    torch_ext.load(
        BUILD_NAME,
        sources=[str(s) for s in sources],
        build_directory=str(build_path),
        extra_cflags=extra_cflags,
        extra_cuda_cflags=extra_cuda_cflags,
        extra_ldflags=[],
        extra_include_paths=[],
        with_cuda=True,
        is_python_module=False,
        verbose=True
    )


def copy_build_output(build_path: Path, runtimes_path: Path):
    build_target = build_path / (BUILD_NAME + torch_ext.LIB_EXT)
    runtimes_target = runtimes_path / "win-x64" / "native" / (BUILD_NAME + torch_ext.CLIB_EXT)
    runtimes_target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy(build_target, runtimes_target)
    logging.info("Copied build output to: %s", runtimes_target)


def main():
    root = Path(__file__).parent
    build_path = root / "build"
    sources = [root / "src/nativeops.cpp"]
    runtimes_path = root / "runtimes"

    try:
        check_nvcc_version()
    except EnvironmentError as e:
        logging.error(str(e))
        sys.exit(1)

    vc_env = msvc._get_vc_env("x86_amd64")

    if "include" in sys.argv:
        print_includes_and_exit()

    if "clean" in sys.argv:
        clean_build(build_path)

    setup_environment(vc_env)
    build_path.mkdir(parents=True, exist_ok=True)

    try:
        build_extension(build_path, sources)
        copy_build_output(build_path, runtimes_path)
    except Exception as e:
        logging.error("Build failed: %s", str(e))
        sys.exit(1)

    logging.info("Build succeeded.")


if __name__ == "__main__":
    main()
