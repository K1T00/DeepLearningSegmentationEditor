#include "c10/util/Optional.h"
#include "utils.hpp"
#include "c10/cuda/CUDACachingAllocator.h"

thread_local char *torch_last_err = nullptr;

EXPORT_API(const char *)
torch_check_last_err()
{
    char *tmp = torch_last_err;
    torch_last_err = nullptr;
    return tmp;
}


EXPORT_API(void)
torch_empty_cache()
{
    c10::cuda::CUDACachingAllocator::emptyCache();
}
