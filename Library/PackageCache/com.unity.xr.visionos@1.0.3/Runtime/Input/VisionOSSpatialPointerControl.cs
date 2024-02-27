using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace UnityEngine.XR.VisionOS.InputDevices
{
    using TouchPhase = UnityEngine.InputSystem.TouchPhase;

    [InputControlLayout(displayName = "VisionOSSpatialPointerControl", stateType = typeof(VisionOSSpatialPointerState))]
    public class VisionOSSpatialPointerControl : InputControl<VisionOSSpatialPointerState>
    {
        public IntegerControl interactionId { get; set; }

        public Vector3Control startRayOrigin { get; set; }

        public Vector3Control startRayDirection { get; set; }

        public QuaternionControl startRayRotation { get; set; }

        public QuaternionControl interactionRayRotation { get; set; }

        public Vector3Control inputDevicePosition { get; set; }

        public QuaternionControl inputDeviceRotation { get; set; }

        public IntegerControl kind { get; set; }

        public TouchPhaseControl phase { get; set; }

        public IntegerControl trackingState { get; set; }

        public ButtonControl isTracked { get; set; }

        public bool isInProgress
        {
            get
            {
                switch (phase.value)
                {
                    case TouchPhase.Began:
                    case TouchPhase.Moved:
                        return true;
                }
                return false;
            }
        }

        public VisionOSSpatialPointerControl()
        {
            m_StateBlock.format = VisionOSSpatialPointerState.Format;
        }

        protected override void FinishSetup()
        {
            interactionId = GetChildControl<IntegerControl>("interactionId");
            startRayOrigin = GetChildControl<Vector3Control>("startRayOrigin");
            startRayDirection = GetChildControl<Vector3Control>("startRayDirection");
            startRayRotation = GetChildControl<QuaternionControl>("startRayRotation");
            interactionRayRotation = GetChildControl<QuaternionControl>("interactionRayRotation");
            inputDevicePosition = GetChildControl<Vector3Control>("inputDevicePosition");
            inputDeviceRotation = GetChildControl<QuaternionControl>("inputDeviceRotation");
            kind = GetChildControl<IntegerControl>("kind");
            phase = GetChildControl<TouchPhaseControl>("phase");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");

            base.FinishSetup();
        }

        public override unsafe VisionOSSpatialPointerState ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (VisionOSSpatialPointerState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        // ReSharper disable once ParameterHidesMember
        public override unsafe void WriteValueIntoState(VisionOSSpatialPointerState value, void* statePtr)
        {
            var valuePtr = (VisionOSSpatialPointerState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<VisionOSSpatialPointerState>());
        }
    }
}
