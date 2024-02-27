using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.XR.VisionOS.InputDevices
{
    using InputDevice = UnityEngine.InputSystem.InputDevice;
    using InputSystem = UnityEngine.InputSystem.InputSystem;

    /// <summary>
    /// Receives serialized PolySpatialInput events and feeds them to the InputSystem
    /// </summary>
    class VisionOSSpatialPointerEventListener
    {
        readonly Dictionary<int, VisionOSSpatialPointerState> m_Inputs = new();
        readonly Dictionary<int, Vector3> m_StartInputDevicePositions = new();
        readonly VisionOSSpatialPointerDevice m_PointerDevice;
        static readonly Lazy<VisionOSSpatialPointerEventListener> k_Instance = new(() => new VisionOSSpatialPointerEventListener());

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void VisionOSInputEventCallback(int eventCount, void* eventsPtr);

        [MonoPInvokeCallback(typeof(VisionOSInputEventCallback))]
        static unsafe void OnInputEvent(int eventCount, void* eventsPtr)
        {
            var inputEvents = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<VisionOSSpatialPointerEvent>(
                eventsPtr,
                eventCount,
                Allocator.None);

            k_Instance.Value.OnInputEvents(inputEvents);
        }

        internal static void OnInputEvent(VisionOSSpatialPointerEvent @event)
        {
            k_Instance.Value.ProcessEvent(@event);
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static unsafe void SetupNativeCallback()
        {
            SetUpInputEventHandler(OnInputEvent);
        }

        [DllImport("__Internal", EntryPoint = "UnityVisionOS_SetUpInputEventHandler")]
        static extern void SetUpInputEventHandler(VisionOSInputEventCallback callback);
#endif

        public VisionOSSpatialPointerEventListener()
        {
            AddDevice(out VisionOSSpatialPointerDevice inputDevice);
            m_PointerDevice = inputDevice;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode)
                return;

            var devices = InputSystem.devices;
            for (var i = devices.Count - 1; i > -1; i--)
            {
                if (InputSystem.devices[i].name.Contains("VisionOSSpatialPointerDevice"))
                {
                    InputSystem.RemoveDevice(InputSystem.devices[i]);
                }
            }
        }
#endif

        static void AddDevice<T>(out T device) where T : InputDevice
        {
            var deviceName = typeof(T).ToString();
            device = InputSystem.AddDevice<T>(deviceName);
            if (device == null)
            {
                Debug.LogError($"Failed to create {deviceName}.");
            }
        }

        VisionOSSpatialPointerState GetPointerId(int rawId)
        {
            if (m_Inputs.TryGetValue(rawId, out var input))
            {
                return input;
            }

            var newId = m_Inputs.Count + 1; // touch id must be non-zero for actual touch, so +1
            var inputEvent = new VisionOSSpatialPointerState
            {
                interactionId = newId
            };

            m_Inputs.Add(rawId, inputEvent);
            return inputEvent;
        }

        void OnInputEvents(NativeArray<VisionOSSpatialPointerEvent> events)
        {
            foreach (var pointerEvent in events)
            {
                ProcessEvent(pointerEvent);
            }
        }

        void ProcessEvent(VisionOSSpatialPointerEvent inputEvent)
        {
            var state = GetPointerId(inputEvent.interactionId);
            var phase = inputEvent.phase;
            var inputDevicePosition = inputEvent.inputDevicePosition;
            var interactionId = state.interactionId;
            if (phase == VisionOSSpatialPointerPhase.Began)
                m_StartInputDevicePositions[interactionId] = inputDevicePosition;

            // Compute interaction ray rotation based on device position
            var rayOrigin = inputEvent.rayOrigin;
            var rayDirection = inputEvent.rayDirection;
            if (m_StartInputDevicePositions.TryGetValue(interactionId, out var startInputDevicePosition))
            {
                // Calculate start position at arbitrary distance, roughly within arms' reach
                var gazeRay = new Ray(rayOrigin, rayDirection);
                var startPosition = gazeRay.GetPoint(0.5f);

                // Update current position based on distance inputDevicePosition has moved
                var currentPosition = startPosition + (inputDevicePosition - startInputDevicePosition);
                var interactionRayDirection = Vector3.Normalize(currentPosition - rayOrigin);
                state.interactionRayRotation = Quaternion.LookRotation(interactionRayDirection);
            }

            switch (phase)
            {
                case VisionOSSpatialPointerPhase.Cancelled:
                case VisionOSSpatialPointerPhase.Ended:
                case VisionOSSpatialPointerPhase.None:
                    m_StartInputDevicePositions.Remove(interactionId);
                    break;
            }

            state.phaseId = (byte)phase;
            state.startRayOrigin = inputEvent.rayOrigin;
            state.startRayDirection = rayDirection;
            state.startRayRotation = rayDirection == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rayDirection);
            state.kindId = (byte)inputEvent.kind;
            state.modifierKeys = (ushort)inputEvent.modifierKeys;
            state.inputDevicePosition = inputEvent.inputDevicePosition;
            state.inputDeviceRotation = inputEvent.inputDeviceRotation;
            var isTracked = phase == VisionOSSpatialPointerPhase.Began || phase == VisionOSSpatialPointerPhase.Moved;
            state.isTracked = isTracked;
            state.trackingState = isTracked ? InputTrackingState.Position | InputTrackingState.Rotation : InputTrackingState.None;
            InputSystem.QueueStateEvent(m_PointerDevice, state);
        }
    }
}
