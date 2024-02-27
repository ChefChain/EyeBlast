using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for hand tracking.
    /// Signatures should match hand_tracking.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class HandTracking
        {
            /// <summary>
            /// Create a hand anchor.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>Returns an allocated `ar_hand_anchor_t` object.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_anchor_create")]
            public static extern IntPtr ar_hand_anchor_create();

            /// <summary>
            /// Get the hand skeleton of hand anchor.
            /// </summary>
            /// <param name="hand_anchor">Hand anchor.</param>
            /// <returns>The skeleton.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_anchor_get_hand_skeleton")]
            public static extern IntPtr ar_hand_anchor_get_hand_skeleton(IntPtr hand_anchor);

            /// <summary>
            /// Create hand tracking configuration.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>An instance of `ar_hand_tracking_configuration_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_tracking_configuration_create")]
            public static extern IntPtr ar_hand_tracking_configuration_create();

            /// <summary>
            /// Create a hand tracking provider.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="hand_tracking_configuration">Hand tracking configuration.</param>
            /// <returns>An instance of `ar_hand_tracking_provider`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_tracking_provider_create")]
            public static extern IntPtr ar_hand_tracking_provider_create(IntPtr hand_tracking_configuration);

            /// <summary>
            /// Determines whether this device supports the hand tracking provider.
            /// </summary>
            /// <returns><see langword="true"/> if the hand tracking provider is supported on this device. Otherwise, <see langword="false"/>.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_tracking_provider_is_supported")]
            public static extern bool ar_hand_tracking_provider_is_supported();

            /// <summary>
            /// Fill the given ar_hand_anchor_t instances with latest hand anchor tracking data, if at least one of the hands has been tracked since the last call
            /// to this function. Subsequent calls to this function will not update the instances and return <see langword="false"/> until updated tracking data has arrived.
            /// </summary>
            /// <param name="hand_tracking_provider">Hand tracking provider.</param>
            /// <param name="hand_anchor_left">`ar_hand_anchor_t` instance for the left hand to be updated</param>
            /// <param name="hand_anchor_right">`ar_hand_anchor_t` instance for the right hand to be updated</param>
            /// <returns><see langword="true"/> on success and <see langword="false"/> if there is no update for either of the hands.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_hand_tracking_provider_get_latest_anchors")]
            public static extern bool ar_hand_tracking_provider_get_latest_anchors(IntPtr hand_tracking_provider,
                IntPtr hand_anchor_left, IntPtr hand_anchor_right);

            /// <summary>
            /// Get the authorization type required by the hand tracking provider.
            /// </summary>
            /// <returns>Authorization type.</returns>
            [DllImport(Constants.LibraryName,
                EntryPoint = "ar_hand_tracking_provider_get_required_authorization_type")]
            public static extern AR_Authorization_Type ar_hand_tracking_provider_get_required_authorization_type();
        }
    }
}
