#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "UnityAppController.h"

#import <CompositorServices/CompositorServices.h>

#if __has_include("RenderMode.h")
#include "RenderMode.h"
#else
#define SINGLE_PASS 0
#endif

@interface VisionOSAppController : UnityAppController
{
    
}
@end

@implementation VisionOSAppController

@end


IMPL_APP_CONTROLLER_SUBCLASS(VisionOSAppController)
