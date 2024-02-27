using System;
using System.Collections;
using System.Collections.Concurrent;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.TestTools;

namespace Tests.Runtime.Functional.Components
{
    #if false
    public class UnityPolySpatialVolumeTests : PooledComponentTestBase<VolumeCamera>
    {
        bool m_HasDefaultCameraSet;
        PolySpatialUnitySimulation m_PreviousPlatform;

        TestHostNetworkPlatform m_HostNetworkPlatform = null;
        TestClientNetworkPlatform m_ClientNetworkPlatform = null;
        ConcurrentQueue<MessageBuffer> m_ClientToHostQueue = new();
        ConcurrentQueue<MessageBuffer> m_HostToClientQueue = new();

        TestPolySpatialCore m_TestCore;

        // Core replacement chicanery may frag host API usage since unitySim setup
        // assumes singleton instance/execution environment.
        bool m_DontRunTestsOnPlatform;

        void SetupHostNetworkPlatform()
        {
            m_HostNetworkPlatform = new TestHostNetworkPlatform(null);
            m_HostNetworkPlatform.Connection = new TestQueuedConnection();
            m_HostNetworkPlatform.Connection.IncomingQueue = m_ClientToHostQueue;
            m_HostNetworkPlatform.Connection.OutgoingQueue = m_HostToClientQueue;
            m_HostNetworkPlatform.Connection.Host = "Testing Host";
            m_HostNetworkPlatform.Connection.Port = 9876;
            m_HostNetworkPlatform.Connection.Identifier = new ConnectionID()
            {
                Address = "Host Connection",
            };

        }

        void SetupClientNetworkPlatform()
        {
            m_ClientNetworkPlatform = new TestClientNetworkPlatform(null);
            m_ClientNetworkPlatform.Connection = new TestQueuedConnection();
            m_ClientNetworkPlatform.Connection.IncomingQueue = m_HostToClientQueue;
            m_ClientNetworkPlatform.Connection.OutgoingQueue = m_ClientToHostQueue;
            m_ClientNetworkPlatform.Connection.Host = "Testing Client";
            m_ClientNetworkPlatform.Connection.Port = 9876;
            m_ClientNetworkPlatform.Connection.Identifier = new ConnectionID()
            {
                Address = "Client Connection",
            };
        }

        bool ShouldIgnoreTestsOnPlatform()
        {
            return PolySpatialCore.LocalBackend.GetType().Name.Contains("RealityKit");
        }

        internal override void InternalSetup()
        {
            m_DontRunTestsOnPlatform = ShouldIgnoreTestsOnPlatform();

            if (m_DontRunTestsOnPlatform)
                return;

            m_PreviousPlatform = PolySpatialCore.UnitySimulation;
            PolySpatialCore.Instance.SetPlatformLayer(null);

            SetupHostNetworkPlatform();
            m_TestCore = new();
            m_TestCore.SetPlatformLayer(m_HostNetworkPlatform);

            // Explicitly disable auto QVC creation so we have control of the
            // when and how.
            m_HasDefaultCameraSet = PolySpatialSettings.instance.EnableDefaultVolumeCamera;
            PolySpatialSettings.instance.EnableDefaultVolumeCamera = false;

            SetupClientNetworkPlatform();
            PolySpatialCore.Instance.SetPlatformLayer(m_ClientNetworkPlatform);

            base.InternalSetup();

            // At this point the client network instance has
            // been wrapped in the test wrapper so we need to
            // work with that instead.
            m_TestPlatformWrapper.OnBeginSession();
        }

        internal override void InternalTearDown()
        {
            if (m_DontRunTestsOnPlatform)
                return;

            base.InternalTearDown();

            m_TestCore.SetPlatformLayer(null);
            m_TestCore = null;
            m_HostNetworkPlatform = null;

            PolySpatialCore.Instance.SetPlatformLayer(m_PreviousPlatform);
            m_ClientNetworkPlatform = null;

            PolySpatialSettings.instance.EnableDefaultVolumeCamera = m_HasDefaultCameraSet;
            m_ClientToHostQueue.Clear();
        }

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialVolumeCamera_Create_Destroy_Tracking()
        {
            if (m_DontRunTestsOnPlatform)
                yield break;

            // Give platforms a time to do some work.
            yield return null;

            CreateTestObjects("Test Camera");

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            var iid = m_TestComponent.GetInstanceID();

            yield return null;

            DestroyAllTestObjects();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetPolySpatialVolumeCameraTrackingData(iid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialVolumeCamera_OnCreate_Creates_ClientVolumeCamera()
        {
            if (m_DontRunTestsOnPlatform)
                yield break;

            // Give platforms a time to do some work.
            yield return null;

            Assert.IsNull(m_ClientNetworkPlatform.Camera);

            CreateTestObjects("Test Camera");

            yield return null;

            Assert.IsNotNull(m_ClientNetworkPlatform.Camera);

            DestroyAllTestObjects();

            yield return null;

            Assert.IsNull(m_ClientNetworkPlatform.Camera);
        }

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialVolumeCamera_OnCreate_Creates_HostVolumeRenderer()
        {
            if (m_DontRunTestsOnPlatform)
                yield break;

            // Give platforms a time to do some work.
            yield return null;

            Assert.IsNull(m_HostNetworkPlatform.GetVolumeRendererForVolumeCamera(
                m_ClientNetworkPlatform.Connection,
                PolySpatialInstanceID.For(m_TestGameObject)));

            CreateTestObjects("Test Camera");

            yield return null;

            Assert.IsNotNull(m_HostNetworkPlatform.GetVolumeRendererForVolumeCamera(
                m_ClientNetworkPlatform.Connection,
                PolySpatialInstanceID.For(m_TestGameObject)));

            DestroyAllTestObjects();

            yield return null;

            Assert.IsNull(m_HostNetworkPlatform.GetVolumeRendererForVolumeCamera(
                m_ClientNetworkPlatform.Connection,
                PolySpatialInstanceID.For(m_TestGameObject)));
        }
    }
#endif
}

