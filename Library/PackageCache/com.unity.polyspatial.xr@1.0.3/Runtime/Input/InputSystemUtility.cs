using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.PolySpatial.XR.Input
{
    public static class InputSystemUtility
    {
        public static void Subscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            Debug.Assert(action != null);
            if (performed != null)
                action.performed += performed;
            if (canceled != null)
                action.canceled += canceled;
        }

        public static void Unsubscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            Debug.Assert(action != null);
            if (performed != null)
                action.performed -= performed;
            if (canceled != null)
                action.canceled -= canceled;
        }

        public static InputAction GetInputAction(InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}
