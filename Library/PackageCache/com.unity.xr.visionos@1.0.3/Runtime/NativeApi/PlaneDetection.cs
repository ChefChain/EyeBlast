using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for plane detection.
    /// Signatures should match plane_detection.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class PlaneDetection
        {
            /// <summary>
            /// Handler triggered when there are updates to plane anchors.
            /// <param name="added_anchors">Collection of anchors that are added.</param>
            /// <param name="updated_anchors">Collection of anchors that are updated.</param>
            /// <param name="removed_anchors">Collection of anchors that are removed.</param>
            /// </summary>
            public unsafe delegate void AR_Plane_Detection_Update_Handler(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count);

            /// <summary>
            /// Create plane detection configuration.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>An instance of `ar_plane_detection_configuration_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_detection_configuration_create")]
            public static extern IntPtr ar_plane_detection_configuration_create();


            /// <summary>
            /// Set the desired alignment of planes to detect.
            /// </summary>
            /// <param name="plane_detection_configuration">The plane detection configuration.</param>
            /// <param name="alignment">The plane alignment.</param>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_detection_configuration_set_alignment")]
            public static extern void ar_plane_detection_configuration_set_alignment(
                IntPtr plane_detection_configuration, AR_Plane_Alignment alignment);

            /// <summary>
            /// Get the alignment of the plane anchor.
            /// </summary>
            /// <param name="plane_anchor">The plane anchor.</param>
            /// <returns>The plane alignment.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_anchor_get_alignment")]
            public static extern AR_Plane_Alignment ar_plane_anchor_get_alignment(IntPtr plane_anchor);

            /// <summary>
            /// Get the geometry of the plane anchor.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="plane_anchor">The plane anchor.</param>
            /// <returns>An instance of `ar_plane_geometry_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_anchor_get_geometry")]
            public static extern IntPtr ar_plane_anchor_get_geometry(IntPtr plane_anchor);

            /// <summary>
            /// Get the classification of the plane anchor.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="plane_anchor">The plane anchor.</param>
            /// <returns>The plane classification.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_anchor_get_plane_classification")]
            public static extern AR_Plane_Classification ar_plane_anchor_get_plane_classification(IntPtr plane_anchor);

            /// <summary>
            /// The mesh vertices of the plane geometry.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="plane_geometry">The plane geometry.</param>
            /// <returns>An instance of `ar_geometry_source_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_geometry_get_mesh_vertices")]
            public static extern IntPtr ar_plane_geometry_get_mesh_vertices(IntPtr plane_geometry);

            /// <summary>
            /// The extent of the plane geometry.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="plane_geometry">The plane geometry.</param>
            /// <returns>An instance of `ar_plane_extent_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_geometry_get_plane_extent")]
            public static extern IntPtr ar_plane_geometry_get_plane_extent(IntPtr plane_geometry);

            /// <summary>
            /// The width of the plane extent.
            /// </summary>
            /// <param name="plane_extent">The plane extent.</param>
            /// <returns>The width of the plane extent.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_extent_get_width")]
            public static extern float ar_plane_extent_get_width(IntPtr plane_extent);

            /// <summary>
            /// The height of the plane extent.
            /// </summary>
            /// <param name="plane_extent">The plane extent.</param>
            /// <returns>The height of the plane extent.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_extent_get_height")]
            public static extern float ar_plane_extent_get_height(IntPtr plane_extent);

            /// <summary>
            /// Create a plane detection provider.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="plane_detection_configuration">A plane detection configuration.</param>
            /// <returns>An instance of `ar_plane_detection_provider`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_detection_provider_create")]
            public static extern IntPtr ar_plane_detection_provider_create(IntPtr plane_detection_configuration);

            /// <summary>
            /// Set the plane detection update handler.
            /// </summary>
            /// <param name="plane_detection_provider">The plane detection provider.</param>
            /// <param name="plane_detection_update_handler">The plane detection update handler.</param>
            [DllImport(Constants.LibraryName,
                EntryPoint = "UnityVisionOS_impl_ar_plane_detection_provider_set_update_handler")]
            public static extern void UnityVisionOS_impl_ar_plane_detection_provider_set_update_handler(
                IntPtr plane_detection_provider,
                AR_Plane_Detection_Update_Handler plane_detection_update_handler);

            /// <summary>
            /// Get the authorization type required by the plane detection provider.
            /// </summary>
            /// <returns>Authorization type.</returns>
            [DllImport(Constants.LibraryName,
                EntryPoint = "ar_plane_detection_provider_get_required_authorization_type")]
            public static extern AR_Authorization_Type ar_plane_detection_provider_get_required_authorization_type();

            /// <summary>
            /// Determines whether this device supports the plane detection provider.
            /// </summary>
            /// <returns><see langword="true"/> if the plane detection provider is supported on this device. Otherwise, <see langword="false"/>.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_plane_detection_provider_is_supported")]
            public static extern bool ar_plane_detection_provider_is_supported();
        }
    }
}
