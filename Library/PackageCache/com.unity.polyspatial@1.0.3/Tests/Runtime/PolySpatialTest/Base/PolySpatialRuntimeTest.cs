using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.PolySpatialTest.Base
{
    /// <summary>
    /// A singleton class to store PolySpatial related values for the duration of a test or test fixture, such
    /// as PolySpatialSetting flags. Used with custom <see cref="PolySpatialRuntimeSetup" /> attributes, PolySpatial property
    /// values are captured and restored before/after test setup/teardown.
    ///
    /// </summary>
    public class PolySpatialRuntimeTest
    {
        private static PolySpatialRuntimeTest s_Instance;
        public static PolySpatialRuntimeTest instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PolySpatialRuntimeTest();

                return s_Instance;
            }
        }

        internal bool enablePolySpatialRuntime;

        internal void EnableRuntime()
        {
            // capture current runtime value
            enablePolySpatialRuntime = PolySpatialRuntime.Enabled;
            PolySpatialRuntime.Enabled = true;
        }

        internal void RestoreRuntime()
        {
            // reset to captured value
            PolySpatialRuntime.Enabled = enablePolySpatialRuntime;
        }
    }

    public class PolySpatialRuntimeSetup: NUnitAttribute, IOuterUnityTestAction
    {
        public IEnumerator BeforeTest(ITest test)
        {
            PolySpatialRuntimeTest.instance.EnableRuntime();
            Debug.Log($"Enabling polyspatial runtime; old value: {PolySpatialRuntimeTest.instance.enablePolySpatialRuntime}");
            yield return null;
        }

        public IEnumerator AfterTest(ITest test)
        {
            Debug.Log($"Restoring polyspatial runtime; old value: {PolySpatialRuntimeTest.instance.enablePolySpatialRuntime}");
            PolySpatialRuntimeTest.instance.RestoreRuntime();
            yield return null;
        }
    }
}
