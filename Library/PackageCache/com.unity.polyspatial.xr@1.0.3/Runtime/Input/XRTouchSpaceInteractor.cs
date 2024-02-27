#if HAS_XR_INTERACTION_TOOLKIT
using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace Unity.PolySpatial.XR.Input
{
    /// <summary>
    /// Can subscribe to a WorldTouch event from the InputSystem and directly
    /// forward it to XRI interactable components via the ColliderId in the
    /// WorldTouchState struct. This evades re-raycasting inside the app
    /// to determine what collider was interacted with.
    /// </summary>
    public class XRTouchSpaceInteractor : XRBaseInteractor
    {
        [FormerlySerializedAs("m_WorldTouch")]
        [SerializeField]
        InputActionReference m_SpatialPointer;

        SpatialPointerState m_SpatialPointerState;

        protected override void Start()
        {
            base.Start();
            InputSystemUtility.Subscribe(m_SpatialPointer, OnWorldTouchPerformed, OnWorldTouchCancelled);
        }

        protected override void OnDestroy()
        {
            InputSystemUtility.Unsubscribe(m_SpatialPointer, OnWorldTouchPerformed, OnWorldTouchCancelled);
            base.OnDestroy();
        }

        void OnWorldTouchPerformed(InputAction.CallbackContext context)
        {
            m_SpatialPointerState = context.ReadValue<SpatialPointerState>();
            transform.position = m_SpatialPointerState.interactionPosition;
        }

        void OnWorldTouchCancelled(InputAction.CallbackContext context)
        {
            m_SpatialPointerState = context.ReadValue<SpatialPointerState>();
        }

        public override bool isSelectActive
        {
            get
            {
                switch (m_SpatialPointerState.phase)
                {
                    case SpatialPointerPhase.Began:
                    case SpatialPointerPhase.Moved:
                        return base.isSelectActive;
                    case SpatialPointerPhase.Ended:
                    case SpatialPointerPhase.None:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();
            switch (m_SpatialPointerState.phase)
            {
                case SpatialPointerPhase.None:
                    break;
                case SpatialPointerPhase.Began:
                case SpatialPointerPhase.Moved:
                case SpatialPointerPhase.Ended:
                    if (TryGetInteractable(m_SpatialPointerState.targetId, out var interactable))
                        targets.Add(interactable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static bool TryGetInteractable(int colliderId, out XRBaseInteractable interactable)
        {
            // Must get GO but seems can get collider directly at some point once PolySpatialInstanceIds of components are stored
            var go = ObjectBridge.FindObjectFromInstanceID(colliderId) as GameObject;
            if (go == null)
            {
                interactable = null;
                return false;
            }

            interactable = go.GetComponent<XRBaseInteractable>();
            return interactable != null;
        }
    }
}
#endif