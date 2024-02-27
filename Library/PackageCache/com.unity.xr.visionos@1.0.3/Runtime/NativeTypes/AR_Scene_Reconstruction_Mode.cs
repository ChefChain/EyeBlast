using System;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Scene reconstruction mode
    /// </summary>
    [Flags]
    enum AR_Scene_Reconstruction_Mode : long
    {
        /// <summary>
        /// Scene reconstruction default mode. Generates a mesh of the world.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Scene reconstruction classification mode. It generates a mesh of the world with an additional classification for each face.
        /// </summary>
        Classification = 1 << 0
    }
}
