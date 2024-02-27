#if INCLUDE_UNITY_INPUT_SYSTEM
using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARSubsystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.VisionOS.InputDevices
{
    using InputSystem = UnityEngine.InputSystem.InputSystem;

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class InputLayoutLoader
    {
#if UNITY_EDITOR
        static InputLayoutLoader()
        {
            RegisterLayouts();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterLayouts()
        {
            InputSystem.RegisterLayout<XRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("(visionOS_HMD)")
                );

            InputSystem.RegisterLayout<HandheldARInputDevice>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("(visionOS_HandheldARDevice)")
            );

            InputSystem.RegisterLayout<VisionOSSpatialPointerControl>(name: VisionOSSpatialPointerState.LayoutName);
            InputSystem.RegisterLayout<VisionOSSpatialPointerDevice>();
        }
    }
}
#endif
