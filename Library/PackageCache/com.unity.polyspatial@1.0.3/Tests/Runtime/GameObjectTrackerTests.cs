using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;


using Assert = NUnit.Framework.Assert;
using Random = UnityEngine.Random;

namespace Tests.Runtime.Functional
{
    [TestFixture]
    public class GameObjectTrackerTests
    {
        GameObject m_Grandparent;
        GameObject m_Parent;
        GameObject m_Child1;
        GameObject m_Child2;

        bool m_RestoreEnablePolySpatialRuntime;
        PolySpatialUnitySimulation m_RestoreOldUnitySim;


        [SetUp]
        public void SetUp()
        {
            m_RestoreEnablePolySpatialRuntime = PolySpatialRuntime.Enabled;
            PolySpatialRuntime.Enabled = true;

            if (!(PolySpatialCore.LocalBackend is PolySpatialUnityBackend))
            {
                // Because of tracker setup, these tests can't really run without the unity backend being set from
                // the beginning.
                Assert.Ignore("Platform is not UnityPolySpatialNativePlatform, skipping.");
                //PolySpatialCore.Instance.SetPlatformLayer(new UnityPolySpatialNativePlatform());
            }

            UnityEngine.Random.InitState(42);
        }

        [TearDown]
        public void TearDown()
        {
            m_Grandparent = null;
            m_Parent = null;
            m_Child1 = null;
            m_Child2 = null;

            PolySpatialRuntime.Enabled = m_RestoreEnablePolySpatialRuntime;
        }

        [Test]
        public void Test_GameObjectTracker_Asserts_UsingPolySpatialCullingMask()
        {
#if UNITY_EDITOR
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => PolySpatialCore.UnitySimulation?.Tracker.TrackObjectChanges(LayerMask.GetMask(PolySpatialUnityBackend.PolySpatialLayerName)));
#endif
        }

        [UnityTest]
        public IEnumerator Test_GameObjectTracker_Create_Destroy_Tracking()
        {
            PolySpatialStateValidator stateValidator = new();

            stateValidator.GetState(
                out var simState,
                out var rendererState,
                out var unregisteredSimAssets,
                out var unregisteredRendererAssets);

            var diffState = stateValidator.GenerateStateDiff(simState, rendererState);
            Assert.IsEmpty(diffState.ToString(), $"Found state diff: {diffState}");
            var stateEntries = simState.Count;

            // Add a single object
            {
                // Adding a GO *should* produce a diff if change processing has not yet run
                m_Grandparent = new GameObject("Grandparent");
                stateEntries++;

                stateValidator.GetState(
                    out simState,
                    out rendererState,
                    out unregisteredSimAssets,
                    out unregisteredRendererAssets);

                diffState = stateValidator.GenerateStateDiff(simState, rendererState);
                Debug.Assert(unregisteredSimAssets.Count == 0 && unregisteredRendererAssets.Count == 0);

                Assert.IsNotEmpty(diffState.ToString(), $"Diff not found after adding Grandparent GameObject: {diffState}");

                // Wait for a frame to allow change processing to occur, then diff the world
                yield return null;

                HasMatchingState(stateValidator, stateEntries);
            }

            // Adding a child
            {
                m_Parent = new GameObject("Parent");
                m_Parent.transform.SetParent(m_Grandparent.transform);
                stateEntries++;

                yield return null;

                HasMatchingState(stateValidator, stateEntries);
            }

            // Adding two grandchildren
            {
                m_Child1 = new GameObject("Child1");
                m_Child1.transform.SetParent(m_Parent.transform);
                stateEntries++;

                m_Child2 = new GameObject("Child2");
                m_Child2.transform.SetParent(m_Parent.transform);
                stateEntries++;

                yield return null;

                HasMatchingState(stateValidator, stateEntries);
            }

            // Destroy a leaf object
            {
                GameObject.Destroy(m_Child1);
                m_Child1 = null;
                stateEntries--;

                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }

            // Destroy an intermediate object destroys its descendants, too
            {
                GameObject.Destroy(m_Parent);
                m_Parent = null;
                stateEntries -= 2;

                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }

            // Clean up the world
            {
                GameObject.Destroy(m_Grandparent);
                m_Grandparent = null;
                stateEntries -= 1;

                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }
        }

        [UnityTest]
        public IEnumerator Test_GameObjectTracker_Modify_Transform_Tracking()
        {
            PolySpatialStateValidator stateValidator = new();

            stateValidator.GetState(
                out var simState,
                out var rendererState,
                out var unregisteredSimAssets,
                out var unregisteredRendererAssets);

            var diffState = stateValidator.GenerateStateDiff(simState, rendererState);

            Debug.Assert(unregisteredSimAssets.Count == 0 && unregisteredRendererAssets.Count == 0);
            Assert.IsEmpty(diffState.ToString(), $"Found state diff: {diffState}");
            int stateEntries = simState.Count;

            m_Grandparent = new GameObject("Grandparent");

            m_Parent = new GameObject("Parent");
            m_Parent.transform.SetParent(m_Grandparent.transform);

            m_Child1 = new GameObject("Child1");
            m_Child1.transform.SetParent(m_Parent.transform);

            m_Child2 = new GameObject("Child2");
            m_Child2.transform.SetParent(m_Parent.transform);

            stateEntries += 4;

            // Validate initial setup
            yield return null;
            HasMatchingState(stateValidator, stateEntries);

            Random.InitState(42);
            // Make a random series of transform modifications; any such modification should be reflected
            List<GameObject> allGOs = new List<GameObject> {m_Grandparent, m_Parent, m_Child1, m_Child2};
            for (int ithBatch = 0; ithBatch < 10; ++ithBatch)
            {
                foreach (var go in allGOs)
                {
                    // On average, modify the transform of half the objects every frame
                    if (RandomBool())
                    {
                        RandomizeTransform(go, out bool _);
                    }
                }

                // Validate initial setup
                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }

            // Clean up the world
            {
                GameObject.Destroy(m_Grandparent);
                m_Grandparent = null;
                stateEntries -= 4;

                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }
        }

        [UnityTest]
        public IEnumerator Test_GameObjectTracker_Modify_Hierarchy_Tracking()
        {
            PolySpatialStateValidator stateValidator = new();

            stateValidator.GetState(
                out var simState,
                out var rendererState,
                out var unregisteredSimAssets,
                out var unregisteredRendererAssets);

            var diffState = stateValidator.GenerateStateDiff(simState, rendererState);

            Debug.Assert(unregisteredSimAssets.Count == 0 && unregisteredRendererAssets.Count == 0);
            Assert.IsEmpty(diffState.ToString(), $"Found state diff: {diffState}");
            int stateEntries = simState.Count;

            m_Grandparent = new GameObject("Grandparent");

            m_Parent = new GameObject("Parent");
            m_Parent.transform.SetParent(m_Grandparent.transform);

            m_Child1 = new GameObject("Child1");
            m_Child1.transform.SetParent(m_Parent.transform);

            m_Child2 = new GameObject("Child2");
            m_Child2.transform.SetParent(m_Parent.transform);

            stateEntries += 4;

            // Validate initial setup
            yield return null;
            HasMatchingState(stateValidator, stateEntries);

            // Make a random series of hierarchy modifications
            List<GameObject> allGOs = new List<GameObject> {m_Grandparent, m_Parent, m_Child1, m_Child2};
            for (int ithBatch = 0; ithBatch < 10; ++ithBatch)
            {
                int newParentIndex = Random.Range(0, allGOs.Count);
                int newChildIndex = Random.Range(0, allGOs.Count);

                if (RandomBool())
                {
                    // Make sure the new parent has no parent to avoid creating cycles
                    allGOs[newParentIndex].transform.SetParent(null);

                    // If newChildIndex == newParentIndex, the node is now an orphan. Otherwise,
                    // give it a random child (which may or may not have children of its own
                    if (newChildIndex != newParentIndex)
                    {
                        allGOs[newChildIndex].transform.SetParent(allGOs[newParentIndex].transform);
                    }
                }
                else
                {
                    while (allGOs[newChildIndex].transform.childCount > 0)
                    {
                        var child = allGOs[newChildIndex].transform.GetChild(0);
                        child.SetParent(null);
                    }

                    // If newChildIndex == newParentIndex, the node is now an orphan. Otherwise,
                    // give it a random parent
                    if (newChildIndex != newParentIndex)
                    {
                        allGOs[newChildIndex].transform.SetParent(allGOs[newParentIndex].transform);
                    }
                }

                // Validate initial setup
                yield return null;
                HasMatchingState(stateValidator, stateEntries);
            }

            // Clean up the world
            for (int i = 0; i < allGOs.Count; ++i)
            {
                GameObject.Destroy(allGOs[i]);
            }

            stateEntries -= 4;
            yield return null;
            HasMatchingState(stateValidator, stateEntries);
        }

        [UnityTest]
        public IEnumerator Test_GameObjectTracker_DisableTrackingMask()
        {
            PolySpatialStateValidator stateValidator = new();
            stateValidator.GetState(
                out var simState,
                out var rendererState,
                out var unregisteredSimAssets,
                out var unregisteredRendererAssets);
            var baselineStateEntries = simState.Count;

            GameObject trackedObject = new("Tracked");
            GameObject untrackedObject = new("Untracked");

            var restoreDisableTrackingMask = PolySpatialSettings.instance.DisableTrackingMask;
            try
            {
                PolySpatialSettings.instance.DisableTrackingMask = 1 << 2;

                trackedObject.layer = 1;
                untrackedObject.layer = 2;

                yield return null;

                // We should only see the tracked object; not the untracked one.
                HasMatchingState(stateValidator, baselineStateEntries + 1);
            }
            finally
            {
                PolySpatialSettings.instance.DisableTrackingMask = restoreDisableTrackingMask;

                GameObject.Destroy(trackedObject);
                GameObject.Destroy(untrackedObject);
            }
        }

        [UnityTest]
        public IEnumerator Test_GameObjectTracker_Create_Destroy_Tracking_ManualRegister()
        {
            Assert.IsTrue(PolySpatialCore.LocalBackend is PolySpatialUnityBackend);
            var unityBackend = PolySpatialCore.LocalBackend as PolySpatialUnityBackend;
            var sceneGraph = unityBackend.SceneGraph;
            var simTracker = PolySpatialCore.UnitySimulation.Tracker;

            // Creation
            {
                const string kGrandparentName = "Grandparent";
                const int kGrandparentLayer = 13;

                m_Grandparent = new GameObject(kGrandparentName);
                m_Grandparent.layer = kGrandparentLayer;

                var grandparentId = PolySpatialInstanceID.For(m_Grandparent);

                sceneGraph.RegisterCustomBackingGameObject(m_Grandparent, grandparentId);

                Assert.IsFalse(simTracker.IsTrackedGameObjectInstanceID(grandparentId.id));
                Assert.IsTrue(sceneGraph.FindBackingGameObjectForId(grandparentId) != null);

                yield return null;

                // This fails because the GameObjectTracker.ShouldTrackGameObject will always return false
                // when the GameObject is already registered to the UnitySceneGraph.
                // In Distributed Rendering, PolySpatialUnitySimulation and UnitySceneGraph never co-exist
                // in the same Unity instance.
                // Assert.IsTrue(simTracker.IsTrackedGameObjectInstanceID(grandparentId.id));

                var clientGameObject = sceneGraph.FindBackingGameObjectForId(grandparentId);
                Assert.IsTrue(clientGameObject != null);
                Assert.AreEqual(kGrandparentLayer, clientGameObject.layer, "UnitySceneGraph should preserve the GameObject layer");
                Assert.AreEqual(kGrandparentName, clientGameObject.name, "UnitySceneGraph should preserve the GameObject name");
                Assert.AreEqual(SceneManager.GetActiveScene(), m_Grandparent.scene, "UnitySceneGraph should not move manually registered GameObjects");
            }

            // Destruction
            {
                var grandparentId = PolySpatialInstanceID.For(m_Grandparent);

                GameObject.Destroy(m_Grandparent);

                yield return null;

                var clientGameObject = sceneGraph.FindBackingGameObjectForId(grandparentId);
                Assert.IsTrue(clientGameObject == null);
            }
        }

        bool RandomBool()
        {
            return (UnityEngine.Random.Range(0, 2) == 1);
        }

        // Randomly choose different ways of randomizing the GO's transform
        void RandomizeTransform(GameObject go, out bool changed)
        {
            changed = false;
            if (RandomBool())
            {
                if (RandomBool())
                {
                    go.transform.localPosition = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-100, 100);
                }
                else
                {
                    go.transform.position = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-100, 100);
                }
                changed = true;
            }

            if (RandomBool())
            {
                if (RandomBool())
                {
                    go.transform.localRotation = UnityEngine.Random.rotationUniform;
                }
                else
                {
                    go.transform.rotation = UnityEngine.Random.rotationUniform;
                }
                changed = true;
            }

            if (RandomBool())
            {
                go.transform.localScale = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-5, 5);
                changed = true;
            }
        }

        static void HasMatchingState(PolySpatialStateValidator stateValidator, int expectedStateEntries)
        {
            stateValidator.GetState(
                out var simState,
                out var rendererState,
                out var unregisteredSimAssets,
                out var unregisteredRendererAssets);

            var diffState = stateValidator.GenerateStateDiff(simState, rendererState);
            var diffText = diffState.ToString();
            Debug.Assert(unregisteredSimAssets.Count == 0 && unregisteredRendererAssets.Count == 0);

            Assert.IsEmpty(diffText, $"Found state diff: {diffText}");
            Assert.AreEqual(
                expectedStateEntries,
                simState.Count,
                $"Sim state entry count mismatch; expecting {expectedStateEntries} but go {simState.Count}");
            Assert.AreEqual(
                expectedStateEntries,
                rendererState.Count,
                $"Renderer state entry count mismatch; expecting {expectedStateEntries} but go {rendererState.Count}");
        }
    }
}
