#if UNITY_EDITOR && UNITY_VISIONOS
using UnityEngine;
using UnityEngine.InputSystem;

#if INCLUDE_UNITY_XRI
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif
#endif

namespace UnityEngine.XR.VisionOS.InputDevices
{
    public class VisionOSPlayModeInput : MonoBehaviour
    {
        [SerializeField]
        bool m_DrawDebugRay;

#if UNITY_EDITOR && UNITY_VISIONOS
        Vector3 m_StartRayOrigin;
        Vector3 m_StartRayDirection;
        Vector2 m_PreviousMousePosition;

#if INCLUDE_UNITY_XRI
        readonly List<XRUIInputModule> m_PreviouslyEnabledModules = new();

        void OnEnable()
        {
            // Disable mouse input on input modules in order to prevent doubling up of events
            m_PreviouslyEnabledModules.Clear();
            foreach (var inputModule in FindObjectsByType<XRUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (inputModule.enableMouseInput)
                {
                    m_PreviouslyEnabledModules.Add(inputModule);
                    inputModule.enableMouseInput = false;
                }
            }
        }

        void OnDisable()
        {
            // Re-enable mouse input on input modules if it was previously enabled
            foreach (var module in m_PreviouslyEnabledModules)
            {
                if (module != null)
                    module.enableMouseInput = true;
            }

            m_PreviouslyEnabledModules.Clear();
        }
#endif

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var inputCamera = Camera.main;
            if (inputCamera == null)
                inputCamera = FindAnyObjectByType<Camera>();

            if (inputCamera == null)
                return;

            var mousePosition = mouse.position.ReadValue();

            // Reject clicks outside of game view
            if (mousePosition.x < 0 || mousePosition.y < 0 || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
                return;

            var ray = inputCamera.ScreenPointToRay(mousePosition);

            var leftButton = mouse.leftButton;
            var rayOrigin = ray.origin;
            var rayDirection = ray.direction;
            if (m_DrawDebugRay)
            {
                var direction = rayDirection * inputCamera.farClipPlane;
                var color = leftButton.isPressed ? Color.red : Color.blue;
                var duration = leftButton.wasPressedThisFrame ? 3f : 0.05f;
                Debug.DrawRay(rayOrigin, direction, color, duration);
            }

            VisionOSSpatialPointerPhase phase;
            if (leftButton.wasPressedThisFrame)
            {
                phase = VisionOSSpatialPointerPhase.Began;
                m_StartRayOrigin = rayOrigin;
                m_StartRayDirection = rayDirection;
            }
            else if (leftButton.wasReleasedThisFrame)
            {
                phase = VisionOSSpatialPointerPhase.Ended;
            }
            else if (leftButton.isPressed)
            {
                // Do not send events if mouse hasn't moved to more accurately represent device behavior
                if (mousePosition == m_PreviousMousePosition)
                    return;

                phase = VisionOSSpatialPointerPhase.Moved;
            }
            else
            {
                return;
            }

            // Create a fake input device pose position roughly at arm's length along the ray
            var inputDevicePosition = ray.GetPoint(0.5f);
            VisionOSSpatialPointerEventListener.OnInputEvent(new VisionOSSpatialPointerEvent
            {
                interactionId = 0,
                phase = phase,
                inputDevicePosition = inputDevicePosition,
                inputDeviceRotation = Quaternion.LookRotation(rayDirection),
                rayOrigin = m_StartRayOrigin,
                rayDirection = m_StartRayDirection,
                kind = VisionOSSpatialPointerKind.IndirectPinch
            });

            m_PreviousMousePosition = mousePosition;
        }
#endif
    }
}
