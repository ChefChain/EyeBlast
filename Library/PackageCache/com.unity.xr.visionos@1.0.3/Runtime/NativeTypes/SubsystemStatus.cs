using UnityEngine;
// ReSharper disable InconsistentNaming

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Status of a subsystem within its lifecycle.
    /// This should match SubsystemStatus in native code
    /// </summary>
    enum SubsystemStatus
    {
        Uninitialized = 0,
        Initialized = 1,
        Started = 2,
        Stopped = 3,
        Shutdown = 4
    }
}
