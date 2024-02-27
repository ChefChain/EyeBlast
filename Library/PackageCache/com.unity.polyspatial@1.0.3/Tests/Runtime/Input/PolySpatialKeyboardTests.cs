#if POLYSPATIAL_INPUT_TESTS
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using UnityEngine.InputSystem;

namespace Tests.Runtime.Functional.Input
{
    [TestFixture]
    public class PolySpatialKeyboardTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();
            InputSystem.RegisterLayout<PolySpatialKeyboard>();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            // Fully reset the simulation in each test in case left over state causes problems
            PolySpatialSimulationHostImpl.ResetInstance();
        }

        [Test]
        public void Test_SendCharacter()
        {
            string pressedKeys = "";
            void OnPress(char key)
            {
                pressedKeys = pressedKeys + key;
            }

            SendEvents('a');

            // has to be called after first event is sent so that device has been initialized
            var currentKeyboard = PolySpatialKeyboard.current;
            if (currentKeyboard == null)
            {
                Assert.Fail();
            }
            currentKeyboard.onTextInput += OnPress;
            InputSystem.Update();

            Assert.AreEqual("a", pressedKeys, "Expected pressed key to be a");
            pressedKeys = "";

            SendEvents('t');
            SendEvents('e');
            SendEvents('s');
            SendEvents('t');
            SendEvents(' ');
            SendEvents('1');
            InputSystem.Update();

            Assert.AreEqual("test 1", pressedKeys, "Expected pressed keys to be test 1");
            pressedKeys = "";
        }

        unsafe void SendEvents(char key)
        {
            NativeArray<char> events = new(1, Allocator.Temp);
             events[0] = key;
             var port = 0;
             var type = PolySpatialInputType.Character;
            PolySpatialCore.HostMulticastHandler?.HostCommand(PolySpatialHostCommand.InputEvent, &type, &port, events.AsSpan());
            events.Dispose();
        }
    }
}
#endif
