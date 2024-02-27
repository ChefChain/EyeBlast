using System;
using System.Collections;
using NUnit.Framework.Interfaces;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.TestRunner;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if !UNITY_EDITOR
[assembly: TestRunCallback(typeof(Tests.Runtime.PolySpatialTest.Base.PlayerLogTestReporter))]
#endif

namespace Tests.Runtime.PolySpatialTest.Base
{
    /// <summary>
    /// An abstract PolySpatial test class with common attributes and methods. This is an implicit TestFixture class
    /// since it contains a UnityTearDown method.
    /// </summary>
    public abstract class PolySpatialTest
    {
        protected GameObject m_TestGameObject;
        protected Light m_TestLight;
        protected MeshRenderer m_TestPolySpatialMeshRenderer;
        protected MeshFilter m_TestPolySpatialMeshFilter;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_TestGameObject)
            {
                // skip a frame in case there is any pending action on the GameObject
                yield return null;
                Object.Destroy(m_TestGameObject);
            }

            yield return new WaitForDone(2f, () => m_TestGameObject == null);

            if (m_TestPolySpatialMeshRenderer)
            {
                Object.Destroy(m_TestPolySpatialMeshRenderer);
                yield return new WaitForDone(2f, () => m_TestPolySpatialMeshRenderer == null);
            }

            if (m_TestLight)
            {
                Object.Destroy(m_TestLight);
                yield return new WaitForDone(2f, () => m_TestLight == null);
            }

            yield return null;
        }

        /// <summary>
        /// This method disables test failures caused by logging error|exception messages from the executing code.
        /// PolySpatial may log these types of messages without throwing an exception, which will cause tests to fail.
        /// Note that this function needs to be called from test methods individually; it cannot be set globally
        /// from pre/post-test methods, like SetUp or TearDown; it only applies to that method's scope.
        /// See Class documentation for more information:
        /// https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/api/UnityEngine.TestTools.LogAssert.html
        /// TODO: remove this workaround if JIRA addressed: https://jira.unity3d.com/browse/LXR-250
        /// </summary>
        protected void DisableFailOnErrorMessages()
        {
            LogAssert.ignoreFailingMessages = true;
        }
    }

    // This class gets set up in the player only via the assembly: attribute mechanism at the
    // top of this file, but this is not protected with ifdefs to make sure we don't introduce
    // compilation issues.
    public class PlayerLogTestReporter : ITestRunCallback
    {
        DateTime m_StartTime;
        StackTraceLogType m_oldLogEnabled;

        public void RunStarted(ITest testsToRun)
        {
            m_oldLogEnabled = Application.GetStackTraceLogType(LogType.Log);
        }

        public void RunFinished(ITestResult testResults)
        {
        }

        public void TestStarted(ITest test)
        {
            m_StartTime = DateTime.Now;

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"##>>>> Test started: {test.Name}");
            if (m_oldLogEnabled != StackTraceLogType.None)
                Application.SetStackTraceLogType(LogType.Log, m_oldLogEnabled);
        }

        public void TestFinished(ITestResult result)
        {
            var duration = DateTime.Now - m_StartTime;

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"##<<<< Test finished: {result.Test.Name} {result.ResultState.Label} ({duration.TotalMilliseconds}ms)");
            if (m_oldLogEnabled != StackTraceLogType.None)
                Application.SetStackTraceLogType(LogType.Log, m_oldLogEnabled);
        }
    }
}
