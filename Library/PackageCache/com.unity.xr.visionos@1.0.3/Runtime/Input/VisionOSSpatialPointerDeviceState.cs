using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
// ReSharper disable MemberCanBePrivate.Global

namespace UnityEngine.XR.VisionOS.InputDevices
{
    [StructLayout(LayoutKind.Explicit)]
    unsafe struct VisionOSSpatialPointerDeviceState : IInputStateTypeInfo
    {
        public static FourCC Format => new('V', 'O', 'P', 'D');

        public const int MaxSpatialPointers = 2;

        [InputControl(name = "primarySpatialPointer", displayName = "Primary Spatial Pointer", layout = VisionOSSpatialPointerState.LayoutName, synthetic = true)]
        [InputControl(name = "startRayOrigin", useStateFrom = "primarySpatialPointer/startRayOrigin", layout = "Vector3")]
        [InputControl(name = "startRayDirection", useStateFrom = "primarySpatialPointer/startRayDirection", layout = "Vector3")]
        [InputControl(name = "startRayRotation", useStateFrom = "primarySpatialPointer/startRayRotation", layout = "Quaternion")]
        [InputControl(name = "interactionRayRotation", useStateFrom = "primarySpatialPointer/interactionRayRotation", layout = "Quaternion")]
        [InputControl(name = "inputDevicePosition", useStateFrom = "primarySpatialPointer/inputDevicePosition", layout = "Vector3")]
        [InputControl(name = "inputDeviceRotation", useStateFrom = "primarySpatialPointer/inputDeviceRotation", layout = "Quaternion")]
        [InputControl(name = "kind", useStateFrom = "primarySpatialPointer/kind", layout = "Integer")]
        [InputControl(name = "phase", useStateFrom = "primarySpatialPointer/phase", layout = "Integer")]
        [InputControl(name = "modifierKeys", useStateFrom = "primarySpatialPointer/modifierKeys", layout = "Integer")]
        [InputControl(name = "trackingState", useStateFrom = "primarySpatialPointer/trackingState", layout = "Integer")]
        [InputControl(name = "isTracked", useStateFrom = "primarySpatialPointer/isTracked", layout = "Button")]
        [FieldOffset(0)]
        public fixed byte primarySpatialPointerData[VisionOSSpatialPointerState.SizeInBytes];

        [InputControl(name = "spatialPointer", displayName = "Spatial Pointer", layout = VisionOSSpatialPointerState.LayoutName, arraySize = MaxSpatialPointers)]
        [FieldOffset(VisionOSSpatialPointerState.SizeInBytes)]
        public fixed byte spatialPointerData[MaxSpatialPointers * VisionOSSpatialPointerState.SizeInBytes];

        public VisionOSSpatialPointerState* primarySpatialPointer
        {
            get
            {
                fixed (byte* ptr = primarySpatialPointerData)
                    return (VisionOSSpatialPointerState*)ptr;
            }
        }

        public VisionOSSpatialPointerState* spatialPointers
        {
            get
            {
                fixed (byte* ptr = spatialPointerData)
                    return (VisionOSSpatialPointerState*)ptr;
            }
        }

        public FourCC format => Format;
    }
}
