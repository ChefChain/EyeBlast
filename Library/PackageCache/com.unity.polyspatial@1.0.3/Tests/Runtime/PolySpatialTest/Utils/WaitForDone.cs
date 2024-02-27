using System;
using UnityEngine;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    /// <summary>
    /// A custom Yield instruction with a built in timeout if predicate never evaluates to true
    /// </summary>
    public class WaitForDone : CustomYieldInstruction
    {
        private Func<bool> m_Predicate;
        private float m_Timeout;
        private bool IsDoneWaiting()
        {
            m_Timeout -= Time.deltaTime;
            return m_Timeout <= 0f || m_Predicate();
        }

        public override bool keepWaiting => !IsDoneWaiting();

        public WaitForDone(float timeout, Func<bool> predicate)
        {
            if (timeout < 0f) timeout = 0f;

            m_Predicate = predicate;
            m_Timeout = timeout;
        }
    }
}
