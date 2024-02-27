#import <Foundation/Foundation.h>

// This actually won't do anything, because it seems like if you use -exported_symbols on
// the linker command line, it overrides _all_ visibility attributes (not just on those
// symbols).
#define EXPORTED_SYMBOL __attribute__((visibility("default")))  __attribute__((__used__))

extern "C" {

static void *g_api = NULL;
static int g_api_size = 0;

// see PolySpatialRealityKitAccess.swift

void EXPORTED_SYMBOL SetPolySpatialNativeAPIImplementation(const void* lightweightApi, int size)
{
    g_api = malloc(size);
    g_api_size = size;
    memcpy(g_api, lightweightApi, size);
}

void EXPORTED_SYMBOL GetPolySpatialNativeAPI(void* lightweightApi)
{
    // TODO size check
    memcpy(lightweightApi, g_api, g_api_size);
}

} // extern "C"
