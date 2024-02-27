using System.Collections;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using NUnit.Framework;
using Tests.Runtime.Functional;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.Functional.Components
{
    public class ComponentTestBase
    {
        internal PolySpatialUnityTracker Tracker => PolySpatialCore.UnitySimulation?.Tracker;
        internal PolySpatialCorePlatformTestWrapper m_TestPlatformWrapper;

        [SetUp]
        public void Setup()
        {
            InternalSetup();
        }

        internal virtual void InternalSetup()
        {
            var nn = PolySpatialCore.UnitySimulation as PolySpatialUnitySimulation;
            m_TestPlatformWrapper = new PolySpatialCorePlatformTestWrapper(nn.NextHandler);
            nn.NextHandler = m_TestPlatformWrapper;
        }

        [TearDown]
        public void TearDown()
        {
            InternalTearDown();
        }

        internal virtual void InternalTearDown()
        {
            var nn = PolySpatialCore.UnitySimulation as PolySpatialUnitySimulation;
            nn.NextHandler = m_TestPlatformWrapper.NextHandler;
            m_TestPlatformWrapper = null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            return InternalUnityTearDown();
        }

        // This will always be called after TearDown so don't do anything
        // in here that is also done in InternalTearDown unless you are
        // explicitly bypassing InternalTearDown.
        internal virtual IEnumerator InternalUnityTearDown()
        {
            yield return null;
        }

        internal void CleanupObject<T>(ref T obj) where T : UnityEngine.Object
        {
            if (obj)
            {
                obj.DestroyAppropriately();
                obj = null;
            }
        }
    }

    public abstract class PooledComponentTestBase<T> : ComponentTestBase where T: UnityEngine.Component
    {

        internal GameObject m_TestGameObject;
        internal T m_TestComponent;

        internal List<(GameObject, Component)> m_ObjectPoolForCleanup = new();

        internal virtual void DestroyLocalTestObjects<U>(GameObject go, U comp) where U : Component
        {
            var item = (go, sr: comp);
            if (m_ObjectPoolForCleanup.Contains(item))
            {
                m_ObjectPoolForCleanup.Remove(item);
            }

            go.DestroyAppropriately();
            comp.DestroyAppropriately();
        }

        internal virtual (GameObject, T) CreateLocalTestObjects(string name)
        {
            var go = new GameObject(name);
            var comp = go.AddComponent<T>();
            var ret = (go, comp);
            m_ObjectPoolForCleanup.Add(ret);
            return ret;
        }

        internal virtual (GameObject, U) CreateLocalTestObjects<U>(string name) where U : Component
        {
            var go = new GameObject(name);
            var comp = go.AddComponent<U>();
            var ret = (go, comp);
            m_ObjectPoolForCleanup.Add(ret);
            return ret;
        }

        internal virtual void CreateTestObjects(string name)
        {
            (m_TestGameObject, m_TestComponent) = CreateLocalTestObjects<T>(name);
        }

        internal virtual void DestroyAllTestObjects()
        {
            m_TestGameObject = null;
            m_TestComponent = null;
            foreach (var (go, comp) in m_ObjectPoolForCleanup)
            {
                go.DestroyAppropriately();
                comp.DestroyAppropriately();
            }

            m_ObjectPoolForCleanup.Clear();
        }

        internal override void InternalTearDown()
        {
            DestroyAllTestObjects();
            base.InternalTearDown();
        }


    }
}
