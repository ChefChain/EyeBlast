using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS.InputDevices
{
    [StructLayout(LayoutKind.Explicit, Size = 60)]
    public struct VisionOSSpatialPointerEvent
    {
        [FieldOffset(0)]
        public int interactionId;

        [FieldOffset(4)] // 0+4
        public Vector3 rayOrigin;

        [FieldOffset(16)] // 4+12
        public Vector3 rayDirection;

        [FieldOffset(28)] // 16+12
        public Vector3 inputDevicePosition;

        [FieldOffset(40)] // 28+12
        public Quaternion inputDeviceRotation;

        [FieldOffset(56)] // 40+16
        public VisionOSSpatialPointerModifierKeys modifierKeys;

        [FieldOffset(58)] // 56+2
        public VisionOSSpatialPointerKind kind;

        [FieldOffset(59)] // 58+1
        public VisionOSSpatialPointerPhase phase;
    }
}
