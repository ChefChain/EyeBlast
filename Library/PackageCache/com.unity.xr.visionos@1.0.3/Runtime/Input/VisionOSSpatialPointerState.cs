    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.InputSystem.Layouts;
    using UnityEngine.InputSystem.LowLevel;
    using UnityEngine.InputSystem.Utilities;

    namespace UnityEngine.XR.VisionOS.InputDevices
    {
        [StructLayout(LayoutKind.Explicit, Size = SizeInBytes)]
        public struct VisionOSSpatialPointerState : IInputStateTypeInfo
        {
            const int k_SizeOfVector3 = sizeof(float) * 3;
            const int k_SizeOfQuaternion = sizeof(float) * 4;
            const int k_InteractionIDOffset = 0;
            const int k_StartRayOriginOffset = k_InteractionIDOffset + sizeof(int);
            const int k_StartRayDirectionOffset = k_StartRayOriginOffset + k_SizeOfVector3;
            const int k_StartRayRotationOffset = k_StartRayDirectionOffset + k_SizeOfVector3;
            const int k_InteractionRayRotationOffset = k_StartRayRotationOffset + k_SizeOfQuaternion;
            const int k_InputDevicePositionOffset = k_InteractionRayRotationOffset + k_SizeOfQuaternion;
            const int k_InputDeviceRotationOffset = k_InputDevicePositionOffset + k_SizeOfVector3;
            const int k_ModifierKeysOffset = k_InputDeviceRotationOffset + k_SizeOfQuaternion;
            const int k_KindIdOffset = k_ModifierKeysOffset + sizeof(ushort);
            const int k_PhaseIdOffset = k_KindIdOffset + sizeof(byte);
            const int k_IsTrackedOffset = k_PhaseIdOffset + sizeof(byte);
            const int k_TrackingStateOffset = k_IsTrackedOffset + sizeof(bool);

            public const string LayoutName = "VisionOSSpatialPointer";
            public const int SizeInBytes = k_TrackingStateOffset + sizeof(int);

            public static FourCC Format => new('V', 'O', 'P', 'S');

            [InputControl(displayName = "Interaction ID", layout = "Integer", synthetic = true, dontReset = true)]
            [FieldOffset(k_InteractionIDOffset)]
            public int interactionId;

            [InputControl(displayName = "Start Ray Origin", noisy = true, dontReset = true)]
            [FieldOffset(k_StartRayOriginOffset)]
            public Vector3 startRayOrigin;

            [InputControl(displayName = "Start Ray Direction", noisy = true, dontReset = true)]
            [FieldOffset(k_StartRayDirectionOffset)]
            public Vector3 startRayDirection;

            [InputControl(displayName = "Start Ray Rotation", noisy = true, dontReset = true)]
            [FieldOffset(k_StartRayRotationOffset)]
            public Quaternion startRayRotation;

            [InputControl(displayName = "Interaction Ray Rotation", noisy = true, dontReset = true)]
            [FieldOffset(k_InteractionRayRotationOffset)]
            public Quaternion interactionRayRotation;

            [InputControl(displayName = "Input Device Position", noisy = true, dontReset = true)]
            [FieldOffset(k_InputDevicePositionOffset)]
            public Vector3 inputDevicePosition;

            [InputControl(displayName = "Input Device Rotation", noisy = true, dontReset = true)]
            [FieldOffset(k_InputDeviceRotationOffset)]
            public Quaternion inputDeviceRotation;

            [InputControl(displayName = "Modifier Keys", layout = "Integer", synthetic = true)]
            [FieldOffset(k_ModifierKeysOffset)]
            public ushort modifierKeys;

            [InputControl(name = "kind", displayName = "Kind", layout = "Integer", synthetic = true)]
            [FieldOffset(k_KindIdOffset)]
            public byte kindId;

            [InputControl(name = "phase", displayName = "Phase", layout = "TouchPhase", synthetic = true)]
            [FieldOffset(k_PhaseIdOffset)]
            public byte phaseId;

            [InputControl(name = "isTracked", displayName = "IsTracked", layout = "Button", synthetic = true)]
            [FieldOffset(k_IsTrackedOffset)]
            public bool isTracked;

            [InputControl(name = "trackingState", displayName = "TrackingState", layout = "Integer", synthetic = true)]
            [FieldOffset(k_TrackingStateOffset)]
            public InputTrackingState trackingState;

            public VisionOSSpatialPointerKind Kind
            {
                get => (VisionOSSpatialPointerKind)kindId;
                set => kindId = (byte)value;
            }

            public bool IsModifierKeyPressed(VisionOSSpatialPointerModifierKeys key)
            {
                return (modifierKeys & (ushort)key) != 0;
            }

            public FourCC format => Format;

            public void SetModifierKey(bool state, ushort modifierKey)
            {
                if (state)
                    modifierKeys |= modifierKey;
                else
                    modifierKeys &= (ushort)~modifierKey;
            }

            public VisionOSSpatialPointerPhase phase
            {
                get => (VisionOSSpatialPointerPhase)phaseId;
                set => phaseId = (byte)value;
            }

            public bool isNoneEndedOrCanceled
            {
                get
                {
                    switch (phase)
                    {
                        case VisionOSSpatialPointerPhase.None:
                        case VisionOSSpatialPointerPhase.Ended:
                        case VisionOSSpatialPointerPhase.Cancelled:
                            return true;
                        default:
                            return false;
                    }
                }
            }
        }
    }
