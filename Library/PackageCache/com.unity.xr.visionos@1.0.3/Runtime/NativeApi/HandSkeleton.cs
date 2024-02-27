using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for hand skeleton.
    /// signatures should match hand_skeleton.h
    /// </summary>
    static partial class NativeApi
    {
        internal static class HandSkeleton
        {
            // TODO: Bring over missing summary comments
            // TODO: Clean up naming

            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_skeleton_get_joint_named")]
            public static extern IntPtr ar_hand_skeleton_get_joint_named(IntPtr hand_skeleton, AR_Skeleton_Joint_Name joint_name);
        }
    }
}
