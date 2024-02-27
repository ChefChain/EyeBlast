using System;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Types of authorization for ARKit data.
    /// </summary>
    [Flags]
    enum AR_Authorization_Type : uint
    {
        /// <summary>
        /// No authorization required.
        /// </summary>
        None = 0,

        /// <summary>
        /// Authorization type used when requesting hand tracking.
        /// </summary>
        Hand_Tracking = 1 << 0,

        /// <summary>
        /// Authorization type used when requesting:
        /// - Plane detection
        /// - Scene reconstruction
        /// </summary>
        World_Sensing = 1 << 1
    }
}
