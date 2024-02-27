#if HAS_TEST_PERF_FRAMEWORK
using System;
using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Tests.Performance
{
    [TestFixture]
    public class PerformanceTests
    {
        // Currently, RealityKit is very slow in these tests (in particular wrt time spent in
        // ProcessNewAndModified.MeshRenderer).  Until this gets optimized, we
        // run this test with fewer objects on RealityKit so it won't take ridiculously long.
        // TODO: LXR-1447 - remove conditional once performance issues resolved
        static bool IsSlowPlatform()
        {
            return PolySpatialCore.LocalBackend.GetType().Name.Contains("RealityKit");
        }

        [UnityTest]
        [Performance]
        public IEnumerator AddAndRemoveColliders()
        {
            yield return AddAndDestroyComponents<BoxCollider>("Collider");
        }

        [UnityTest]
        [Performance]
        public IEnumerator ModifyColliders()
        {
            yield return ModifyComponents<BoxCollider>(collider =>
            {
                collider.size = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                collider.center = Random.insideUnitSphere;
            }, "Collider");
        }

        [UnityTest]
        [Performance]
        public IEnumerator ModifyMeshRendererMaterials()
        {
            int numMaterials = 7;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material[numMaterials];
            for (int i = 0; i < numMaterials; i++)
                mat[i] = new Material(shader);
            yield return ModifyComponents<MeshRenderer>(mr =>
            {
                int numMRMaterials = Random.Range(1, numMaterials);
                var newMaterials = new Material[numMRMaterials];
                for (int i = 0; i < numMRMaterials; i++)
                    newMaterials[i] = mat[Random.Range(0, numMaterials)];
                mr.sharedMaterials = newMaterials;
            }, "MeshRenderer");
        }

        public IEnumerator ModifyComponents<T>(Action<T> modifyCallback, string trackingLabel) where T: Component
        {
            string[] markers =
            {
                $"Track.{trackingLabel}",
                $"ProcessDestroyed.{trackingLabel}",
                $"ProcessNewAndModified.{trackingLabel}",
            };

            int numObjs = IsSlowPlatform() ? 100 : 1000;
            int numFrames = 100;
            var components = new T[numObjs];

            for (int i = 0; i < numObjs; i++)
                components[i] = new GameObject().AddComponent<T>();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            using (Measure.ProfilerMarkers(markers))
            {
                for (int i = 0; i < numFrames; i++)
                {
                    foreach (var component in components)
                        modifyCallback(component);

                    yield return null;
                }
            }

            foreach (var component in components)
                UnityObject.Destroy(component.gameObject);
        }

        public IEnumerator AddAndDestroyComponents<T>(string trackingLabel) where T: Component
        {
            string[] markers =
            {
                $"Track.{trackingLabel}",
                $"ProcessDestroyed.{trackingLabel}",
                $"ProcessNewAndModified.{trackingLabel}",
            };

            int numObjs = IsSlowPlatform() ? 100 : 1000;
            int numFrames = 100;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            using (Measure.ProfilerMarkers(markers))
            {
                for (int i = 0; i < numFrames; i++)
                {
                    var components = new T[numObjs];

                    for (int j = 0; j < numObjs; j++)
                        components[j] = new GameObject().AddComponent<T>();

                    yield return null;

                    foreach (var component in components)
                        UnityObject.Destroy(component.gameObject);

                    yield return null;
                }
            }
        }
    }
}
#endif
