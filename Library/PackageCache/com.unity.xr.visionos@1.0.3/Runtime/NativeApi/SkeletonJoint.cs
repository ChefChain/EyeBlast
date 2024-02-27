using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for skeleton.
    /// signatures should match skeleton_joint.h
    /// </summary>
    static partial class NativeApi
    {
        internal static class SkeletonJoint
        {
            // TODO: Bring over missing summary comments
            // TODO: Clean up naming

            [DllImport(Constants.LibraryName, EntryPoint = "ar_skeleton_joint_get_anchor_from_joint_transform")]
            public static extern IntPtr ar_skeleton_joint_get_anchor_from_joint_transform(IntPtr joint);

            [DllImport(Constants.LibraryName, EntryPoint = "ar_skeleton_joint_is_tracked")]
            public static extern bool ar_skeleton_joint_is_tracked(IntPtr joint);

        }
    }
}
