#if defined(__APPLE__)
#include <TargetConditionals.h>

#if TARGET_OS_XR
extern "C" int _iOSStepCounterIsAvailable()
{
    return 0;
}

extern "C" int _iOSStepCounterGetAuthorizationStatus()
{
    return 0;
}

extern "C" int _iOSStepCounterEnable(int deviceId, void* callbacks, int sizeOfCallbacks)
{
    return -1;
}

extern "C" int _iOSStepCounterDisable(int deviceId)
{
    return -1;
}

extern "C" int _iOSStepCounterIsEnabled(int deviceId)
{
    return 0;
}

#endif
#endif
