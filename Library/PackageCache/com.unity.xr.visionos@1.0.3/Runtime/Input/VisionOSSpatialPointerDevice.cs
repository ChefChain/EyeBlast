using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace UnityEngine.XR.VisionOS.InputDevices
{
    using InputDevice = UnityEngine.InputSystem.InputDevice;
    using InputSystem = UnityEngine.InputSystem.InputSystem;

    [InputControlLayout(stateType = typeof(VisionOSSpatialPointerDeviceState))]
    public class VisionOSSpatialPointerDevice : InputDevice, IInputStateCallbackReceiver
    {
        public VisionOSSpatialPointerControl primaryInput { get; protected set; }

        public ReadOnlyArray<VisionOSSpatialPointerControl> inputs { get; protected set; }

        readonly Dictionary<int, VisionOSSpatialPointerState> m_BeganEvents = new();
        readonly Dictionary<int, VisionOSSpatialPointerState> m_EndedEvents = new();

        protected override void FinishSetup()
        {
            base.FinishSetup();

            primaryInput = GetChildControl<VisionOSSpatialPointerControl>("primarySpatialPointer");

            var pointerArray = new VisionOSSpatialPointerControl[VisionOSSpatialPointerDeviceState.MaxSpatialPointers];
            var index = 0;
            foreach (var child in children)
            {
                if (child == primaryInput)
                    continue;

                if (child is VisionOSSpatialPointerControl control)
                    pointerArray[index++] = control;
            }

            inputs = new ReadOnlyArray<VisionOSSpatialPointerControl>(pointerArray);
        }

        public void OnNextUpdate()
        {
            // Immediately send a Moved event after every Began event because the system will not send one until the user actually moves their hand
            foreach (var kvp in m_BeganEvents)
            {
                var state = kvp.Value;
                state.phase = VisionOSSpatialPointerPhase.Moved;
                InputSystem.QueueStateEvent(this, state);
            }

            m_BeganEvents.Clear();

            // Immediately send a None event after every Ended event to clear the phase state
            foreach (var kvp in m_EndedEvents)
            {
                var state = kvp.Value;
                state.phase = VisionOSSpatialPointerPhase.None;
                InputSystem.QueueStateEvent(this, state);
            }

            m_EndedEvents.Clear();
        }

        unsafe void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            var stateEventPtr = (StateEvent*)eventPtr.data;

            if (stateEventPtr->stateFormat != VisionOSSpatialPointerState.Format)
                return;

            Debug.Assert(eventPtr.type != DeltaStateEvent.Type);

            VisionOSSpatialPointerState newState;
            if (stateEventPtr->stateSizeInBytes == VisionOSSpatialPointerState.SizeInBytes)
            {
                newState = *(VisionOSSpatialPointerState*)stateEventPtr->state;
            }
            else
            {
                newState = default;
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref newState), stateEventPtr->state, stateEventPtr->stateSizeInBytes);
            }

            var interactionId = newState.interactionId;
            var inputIndex = interactionId - 1;

            if (newState.phase == VisionOSSpatialPointerPhase.Began)
                m_BeganEvents[interactionId] = newState;

            if (newState.phase == VisionOSSpatialPointerPhase.Ended)
                m_EndedEvents[interactionId] = newState;

            InputState.Change(inputs[inputIndex], ref newState, eventPtr: eventPtr);

            var statePtr = currentStatePtr;
            var primaryState = (VisionOSSpatialPointerState*)((byte*)statePtr + primaryInput.stateBlock.byteOffset);
            if (primaryState->isNoneEndedOrCanceled || interactionId == primaryInput.interactionId.ReadValue())
            {
                InputState.Change(primaryInput, ref newState, eventPtr: eventPtr);
            }
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }
    }
}
