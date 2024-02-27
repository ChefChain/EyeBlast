using System;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Option set describing possible general alignments of a detected plane.
    /// </summary>
    [Flags]
    enum AR_Plane_Alignment : long
    {
        /// <summary>
        /// No plane alignment.
        /// </summary>
        None = 0,

        /// <summary>
        /// Planes orthogonal to the gravity vector.
        /// </summary>
        Horizontal = 1 << 0,

        /// <summary>
        /// Planes parallel to the gravity vector.
        /// </summary>
        Vertical = 1 << 1
    }
}
