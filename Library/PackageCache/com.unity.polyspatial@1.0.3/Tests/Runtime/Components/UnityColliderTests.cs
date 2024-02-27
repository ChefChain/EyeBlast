using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

using Assert = NUnit.Framework.Assert;

// These tests require the simulator, so must be INTERNAL
#if POLYSPATIAL_INTERNAL
namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityColliderTests
    {
        GameObject m_TestGameObject;
        Collider m_TestCollider;
        Collider m_TestCompoundCollider;
        LayerMask m_InitialLayerMask;
        bool m_InitialEnableSimulator;

        PolySpatialUnityTracker Tracker => PolySpatialCore.UnitySimulation?.Tracker;

        [SetUp]
        public void SetUpTest()
        {
            m_InitialLayerMask = PolySpatialSettings.instance.ColliderSyncLayerMask;
            PolySpatialSettings.instance.ColliderSyncLayerMask = 1;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestCollider != null)
                UnityObject.Destroy(m_TestCollider);
            if (m_TestCompoundCollider != null)
                UnityObject.Destroy(m_TestCompoundCollider);
            if (m_TestGameObject != null)
                UnityObject.Destroy(m_TestGameObject);
            m_TestGameObject = null;
            m_TestCollider = null;
            m_TestCompoundCollider = null;

            PolySpatialSettings.instance.ColliderSyncLayerMask = m_InitialLayerMask;
        }

        (GameObject, T) CreateTestObjects<T>() where T : Collider
        {
            m_TestGameObject = new GameObject("Collider Test Object");
            m_TestCollider = m_TestGameObject.AddComponent<T>();
            return (m_TestGameObject, (T)m_TestCollider);
        }

        [UnityTest]
        public IEnumerator Test_BoxCollider_Create_Destroy_Tracking()
        {
            yield return Test_UnityCollider_Create_Destroy_Tracking<BoxCollider>();
        }

        [UnityTest]
        public IEnumerator Test_SphereCollider_Create_Destroy_Tracking()
        {
            yield return Test_UnityCollider_Create_Destroy_Tracking<SphereCollider>();
        }

        [UnityTest]
        public IEnumerator Test_CapsuleCollider_Create_Destroy_Tracking()
        {
            yield return Test_UnityCollider_Create_Destroy_Tracking<CapsuleCollider>();
        }

        [UnityTest]
        public IEnumerator Test_BoxCollider_Set_Updates_ColliderData()
        {
            void ChangeColliderCallback(BoxCollider collider)
            {
                collider.size = Vector3.one * 10;
                collider.center = Vector3.one * 10;
            };
            yield return Test_UnityCollider_Set_Updates_ColliderData<BoxCollider>(ChangeColliderCallback);
        }

        [UnityTest]
        public IEnumerator Test_SphereCollider_Set_Updates_ColliderData()
        {
            void ChangeColliderCallback(SphereCollider collider)
            {
                collider.radius = 10;
                collider.center = Vector3.one * 10;
            };
            yield return Test_UnityCollider_Set_Updates_ColliderData<SphereCollider>(ChangeColliderCallback);
        }

        [UnityTest]
        public IEnumerator Test_CapsuleCollider_Set_Updates_ColliderData()
        {
            void ChangeColliderCallback(CapsuleCollider collider)
            {
                collider.radius = 10;
                collider.height = 10;
                collider.center = Vector3.one * 10;
            };
            yield return Test_UnityCollider_Set_Updates_ColliderData<CapsuleCollider>(ChangeColliderCallback);
        }

        [UnityTest]
        public IEnumerator Test_CompoundCollider_Create_Destroy_Tracking()
        {
            CreateTestObjects<BoxCollider>();
            m_TestCompoundCollider = m_TestGameObject.AddComponent<SphereCollider>();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            var cData = PolySpatialComponentUtils.GetTrackingData(m_TestCompoundCollider);
            Assert.IsTrue(cData.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, cData.GetLifecycleStage());

            var mriid = m_TestCollider.GetInstanceID();
            var cMriid = m_TestCompoundCollider.GetInstanceID();

            // destroy one collider
            UnityObject.Destroy(m_TestCollider);

            yield return null;

#if UNITY_EDITOR
            // check correct collider is destroyed
            data = PolySpatialComponentUtils.GetColliderTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
            cData = PolySpatialComponentUtils.GetColliderTrackingData(cMriid);
            Assert.IsTrue(cData.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, cData.GetLifecycleStage());

            // destroy other collider
            UnityObject.Destroy(m_TestCompoundCollider);

            yield return null;

#if UNITY_EDITOR
            // check other collider is destroyed
            cData = PolySpatialComponentUtils.GetColliderTrackingData(cMriid);
            Assert.IsTrue(cData.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, cData.GetLifecycleStage());
#endif
        }

        IEnumerator Test_UnityCollider_Set_Updates_ColliderData<T>(Action<T> changeColliderCallback) where T : Collider
        {
            CreateTestObjects<T>();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            Bounds oldBounds = new Bounds();
            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestCollider.gameObject));
            if (backingGO != null)
                oldBounds = backingGO.GetComponent<T>().bounds;

            changeColliderCallback(m_TestCollider as T);

            // let the changes happen
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            if (backingGO != null)
                Assert.AreNotEqual(oldBounds, backingGO.GetComponent<T>().bounds, "Expected the collider size to be different.");
        }

        IEnumerator Test_UnityCollider_Create_Destroy_Tracking<T>() where T : Collider
        {
            CreateTestObjects<T>();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var mriid = m_TestCollider.GetInstanceID();

            // destroy the component
            UnityObject.Destroy(m_TestCollider);

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetColliderTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_Collider_Disable_Tracking()
        {
            CreateTestObjects<BoxCollider>();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestCollider.gameObject));
            if (backingGO != null)
                Assert.IsTrue(backingGO.GetComponent<Collider>().enabled);

            m_TestCollider.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestCollider);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            if (backingGO != null)
            {
                // Collider should not be null as it was only disabled!
                Assert.IsTrue(backingGO.GetComponent<Collider>() != null);
            }
        }

        [UnityTest]
        public IEnumerator Test_MeshCollider_Convex()
        {
            var (_, collider) = CreateTestObjects<MeshCollider>();
            collider.sharedMesh = new Mesh();

            // Non-convex, not a trigger: should be fine.
            yield return null;

            // Convex, is trigger: should be fine.
            collider.convex = true;
            collider.isTrigger = true;
            yield return null;

            // Non-convex, is trigger: not supported.
            collider.convex = false;
            LogAssert.Expect(LogType.Error, "Triggers on concave MeshColliders are not supported");
            yield return null;
        }
    }
}
#endif
