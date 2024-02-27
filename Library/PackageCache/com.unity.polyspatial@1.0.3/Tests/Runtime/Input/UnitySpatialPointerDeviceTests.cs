#if POLYSPATIAL_INPUT_TESTS
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityObject = UnityEngine.Object;

namespace Tests.Runtime.Functional.Input
{
    [Obsolete]
    [TestFixture]
    public class UnitySpatialPointerDeviceTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            InputSystem.RegisterLayout<PolySpatialTouchscreen>();
            InputSystem.RegisterLayout<SpatialPointerControl>(name: SpatialPointerState.LayoutName);
            InputSystem.RegisterLayout<SpatialPointerDevice>();
        }


        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            // Fully reset unitySim per each test in case left over state causes problems
            PolySpatialSimulationHostImpl.ResetInstance();
        }

        [Test]
        public void Test_CheckPhaseState()
        {
            SpatialPointerState? state = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                state = context.ReadValue<SpatialPointerState>();
            }

            var primaryAction = new InputAction(binding: "<SpatialPointerDevice>/primarySpatialPointer");
            primaryAction.performed += OnActionChange;
            primaryAction.Enable();

            SendEvents(SpatialPointerPhase.Began, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsNotNull(state, "Expected to receive a SpatialPointerState.");
            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Began, "Expected Phase.Began");

            SendEvents(SpatialPointerPhase.Moved, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Moved, "Expected Phase.Moved");

            SendEvents(SpatialPointerPhase.Ended, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Ended, "Expected Phase.Ended");

            primaryAction.Disable();
        }

        [Test]
        public void Test_CheckPositionState()
        {
            SpatialPointerState? state = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                state = context.ReadValue<SpatialPointerState>();
            }

            var primaryAction = new InputAction(binding: "<SpatialPointerDevice>/primarySpatialPointer");
            primaryAction.performed += OnActionChange;
            primaryAction.Enable();

            Vector3 testPosition = Vector3.one * 10.0f;
            int testColliderId = 10;

            SendEvents(SpatialPointerPhase.Began, testPosition, testColliderId);
            InputSystem.Update();

            // TODO -- this test depends on whatever the current opened scene is and its volume camera.
            // It should ensure a specific scene is open!
            var curVolCam = UnityObject.FindObjectOfType<VolumeCamera>();
            var expectedPosition = curVolCam.VolumeSpaceToWorldSpaceMatrix.MultiplyPoint3x4(testPosition);

            Assert.IsNotNull(state, "Expected to receive a SpatialPointerState.");
            Assert.AreEqual(expectedPosition, state.Value.interactionPosition, "Expected correct interactionPosition");
            Assert.AreEqual(testColliderId, state.Value.targetId, "Expected correct targetId");

            primaryAction.Disable();
        }

        unsafe void SendEvents(SpatialPointerPhase phase, Vector3 position, int colliderId)
        {
            NativeArray<PolySpatialPointerEvent> events = new(2, Allocator.Temp);
            var pointerEvent = new PolySpatialPointerEvent()
            {
                targetId = new PolySpatialInstanceID { id = colliderId },
                interactionId = 1,
                interactionPosition = position,
                phase = (PolySpatialPointerPhase)phase
            };

            events[0] = pointerEvent;
            pointerEvent.interactionId = 2;
            events[1] = pointerEvent;

            var port = 0;
            var type = PolySpatialInputType.Pointer;
            PolySpatialCore.HostMulticastHandler?.HostCommand(PolySpatialHostCommand.InputEvent, &type, &port, events.AsSpan());
            events.Dispose();
        }
    }
}
#endif
