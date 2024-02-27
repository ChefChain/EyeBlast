using System;
using UnityEngine;

#if UNITY_VISIONOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Runtime scripting API for VisionOS.
    /// </summary>
    public static class VisionOS
    {
#if UNITY_VISIONOS && !UNITY_EDITOR
        const string k_LibraryName = "__Internal";

        /// <summary>
        /// Set the range of values used for depth sorting.
        /// These values should match Camera.nearClipPlane and Camera.farClipPlane
        /// </summary>
        /// <param name="near">The value for the near clipping plane.</param>
        /// <param name="far">The value for the far clipping plane.</param>
        [DllImport(k_LibraryName, EntryPoint = "SetDepthRange")]
        public static extern void SetDepthRange(float near, float far);

        /// <summary>
        /// Return true if the app is running in the visionOS Simulator.
        /// Treat the Editor as running in simulator.
        /// </summary>
        [DllImport(k_LibraryName, EntryPoint = "UnityVisionOS_IsSimulator")]
        public static extern bool IsSimulator();

        /// <summary>
        /// Determine whether the immersive space for the app is ready.
        /// </summary>
        /// <returns><see langword="true"/> if the immersive space is ready. Otherwise, <see langword="false"/>.</returns>
        [DllImport(k_LibraryName, EntryPoint = "UnityVisionOS_IsImmersiveSpaceReady")]
        public static extern bool IsImmersiveSpaceReady();
#else
        /// <summary>
        /// Set the range of values used for depth sorting.
        /// These values should match Camera.nearClipPlane and Camera.farClipPlane
        /// </summary>
        /// <param name="near">The value for the near clipping plane.</param>
        /// <param name="far">The value for the far clipping plane.</param>
        public static void SetDepthRange(float near, float far) { }

        /// <summary>
        /// Determine whether the app is running in the visionOS simulator.
        /// Treat the Editor targeting visionOS as running in simulator.
        /// </summary>
        /// <returns><see langword="true"/> if the app is running in the visionOS Simulator.
        /// Otherwise, <see langword="false"/>.</returns>
        public static bool IsSimulator()
        {
#if UNITY_VISIONOS
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Determine whether the immersive space for the app is ready.
        /// Return true in the Editor for testing purposes.
        /// </summary>
        /// <returns><see langword="true"/> if the immersive space is ready (or in the Editor when targeting visionOS).
        /// Otherwise, <see langword="false"/>.</returns>
        public static bool IsImmersiveSpaceReady()
        {
#if UNITY_VISIONOS
            return true;
#else
            return false;
#endif
        }
#endif
    }
}
