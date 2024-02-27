---
uid: psl-vos-polyspatial-input
---

# PolySpatial Input
The PolySpatial package adds a new input device `SpatialPointerDevice` that developers can use with Input System action maps and APIs.

You can leverage the [Enhanced Touch API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html) for polling touch phases.

> [!NOTE]
> * SpatialPointerState phase will never be valid to use when polling the state. Instead, use the EnhancedTouch API to get active touches and query the phase.
> * When filtering input based on the Spatial Pointer Kind enum for `Kind.DirectPinch` it's recomended to not check this when in a Began touch phase. This is because a `Kind.Touch` will usually be called before a direct pinch is detected and direct pinch will be set in a later touch phase.

## Spatial Pointer Device Data
The SpatialPointerDevice mirrors the [SpatialEventCollection](https://developer.apple.com/documentation/swiftui/spatialeventcollection/event) data from SwiftUI and is the primary way to interact with content on visionOS. The input data is limited depending on your app's mode (bounded vs unbounded) and when you are requesting the data. Some data is only valid for the first frame in which the input was performed.

The input is registered by the user looking at an object and peforming a pinch gesture with their index finger and thumb. This type of input will register as an `Indirect Pinch`. Input can also be performed with a direct poke (`Touch`) or direct pinch (`Direct Pinch`) on an object.
* Up to two inputs can be registered at the same time. 
* The object a user is looking at must have a collider on it to capture input.
* The `inputDevicePosition` and `inputDeviceRotation` properties describe the pose of the input device controlling the interaction. Typically, this is based on the user's pinch (a point between their finger and thumb).
* By default, the Interaction Ray is based on the user's eye gaze.


The `SpatialPointerDevice` provides the following properties:
* `.interactionPosition`: the position of the interaction in world space. This value will updated while the input is maintained and will always start on a collider.
* `.deltaInteractionPosition`: the difference between the starting interaction position and the current interaction position.
* `.startInteractionPosition`: the starting position of the interaction. This will always be on a collider and will only be set when the input occurs.
* `.startInteractionRayOrigin`: the ray origin based on the user's eye gaze. This is only available when an app is in unbounded mode and will only be available when the input occurs.
* `.startInteractionRayDirection`: the ray direction based on the user's eye gaze. This is only available when an app is in unbounded mode and will only be available when the input occurs.
* `.inputDevicePosition`: the position of the user's pinch (between the user's thumb and index finger). This value will be updated while the input is maintained.
* `.inputDeviceRotation`: the rotation of the user's pinch (between the user's thumb and index finger). This value will be updated while the input is maintained.
* `.kind`: the interaction kind, Touch (poke), In Direct Pinch, Direct Pinch, Pointer, Stylus.
* `.targetId`: the instance ID of the object being interacted with.
* `.phase`: the spatial pointer touch phase of the current interaction.
* `.modifierKeys`: any modifer keys that are active while the interaction is happening.
* `.targetObject`: a direct reference to the game object the interaction targets.

## Use a spatial pointer in an Action Map
The `Spatial Pointer Device` is listed in the `Other` section of Input Action maps. There is a `Primary Spatial Pointer` for detecting the primary interaction and a `Spatial Pointer #0` and `Spatial Pointer #1` for the first and second interactions respectively.

## Use a spatial pointer in direct code
A script that surfaces `SpatialPointerState` data based on active touch input.

```
using Unity.PolySpatial.InputDevices;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputScript : MonoBehaviour
{
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        var activeTouches = Touch.activeTouches;

        // You can determine the number of active inputs by checking the count of activeTouches
        if (activeTouches.Count > 0)
        {
            // For getting access to PolySpatial (visionOS) specific data you can pass an active touch into the EnhancedSpatialPointerSupport()
            SpatialPointerState primaryTouchData = EnhancedSpatialPointerSupport.GetPointerState(activeTouches[0]);

            SpatialPointerKind interactionKind = primaryTouchData.Kind;
            GameObject objectBeingInteractedWith = primaryTouchData.targetObject;
            Vector3 interactionPosition = primaryTouchData.interactionPosition;
        }
    }
}
```

## Limit interaction by kind
You can check the spatial pointer device data to limit the kinds of interactions to permit. The following example only executes its input logic for indirect pinch and poke interactions:

```
var activeTouches = Touch.activeTouches;

if (activeTouches.Count > 0)
{
    SpatialPointerState primaryTouchData = EnhancedSpatialPointerSupport.GetPointerState(activeTouches[0]);

    if (primaryTouchData.Kind == SpatialPointerKind.IndirectPinch || primaryTouchData.Kind == SpatialPointerKind.Touch)
    {
        // do input logic here
    }
}
```

## Select, move, and rotate an object
You can update the position and rotation of an object based on the interaction position and device rotation. The following example shows how to select an object, translate and rotate the object and unselect the object based on touch phases:

```
using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;


public class InputScript : MonoBehaviour
{
    GameObject m_SelectedObject;

    void Update()
    {
        var activeTouches = Touch.activeTouches;

        if (activeTouches.Count > 0)
        {
            SpatialPointerState primaryTouchData = EnhancedSpatialPointerSupport.GetPointerState(activeTouches[0]);

            if (activeTouches[0].phase == TouchPhase.Began)
            {
                // if the targetObject is not null, set it to the selected object
                m_SelectedObject = primaryTouchData.targetObject != null ? primaryTouchData.targetObject : null;
            }

            if (activeTouches[0].phase == TouchPhase.Moved)
            {
                if (m_SelectedObject != null)
                {
                    m_SelectedObject.transform.SetPositionAndRotation(primaryTouchData.interactionPosition, primaryTouchData.deviceRotation);
                }
            }

            if(activeTouches[0].phase == TouchPhase.Ended || activeTouches[0].phase == TouchPhase.Canceled)
            {
                m_SelectedObject = null;
            }
        }
    }
}
```