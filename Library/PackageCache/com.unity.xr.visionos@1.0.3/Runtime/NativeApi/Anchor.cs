using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for anchor.
    /// Signatures should match anchor.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class Anchor
        {
            // TODO: Bring over missing summary comments
            // TODO: Clean up naming
            [DllImport(Constants.LibraryName, EntryPoint = "ar_anchor_get_origin_from_anchor_transform")]
            public static extern IntPtr ar_anchor_get_origin_from_anchor_transform(IntPtr anchor);

            [DllImport(Constants.LibraryName,
                EntryPoint = "UnityVisionOS_impl_ar_anchor_get_origin_from_anchor_transform_to_float_array")]
            public static extern IntPtr UnityVisionOS_impl_ar_anchor_get_origin_from_anchor_transform_to_float_array(
                IntPtr anchor);

            [DllImport(Constants.LibraryName, EntryPoint = "ar_anchor_get_identifier")]
            public static extern void ar_anchor_get_identifier(IntPtr anchor, IntPtr out_identifier);

            [DllImport(Constants.LibraryName, EntryPoint = "ar_trackable_anchor_is_tracked")]
            public static extern bool ar_trackable_anchor_is_tracked(IntPtr anchor);
        }
    }
}
