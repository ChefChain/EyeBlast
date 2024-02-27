using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for scene reconstruction.
    /// Signatures and types should match scene_reconstruction.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class SceneReconstruction
        {
            /// <summary>
            /// Get a Metal buffer containing per-vector data for the source, and return its contents as a void*
            /// </summary>
            /// <param name="geometry_source">The geometry source.</param>
            /// <returns>The contents of the Metal buffer.</returns>
            [DllImport(NativeApi.Constants.LibraryName,
                EntryPoint = "UnityVisionOS_impl_ar_geometry_source_get_buffer")]
            public static extern IntPtr UnityVisionOS_impl_ar_geometry_source_get_buffer(IntPtr geometry_source);

            /// <summary>
            /// Get the number of vectors in the source.
            /// </summary>
            /// <param name="geometry_source">The geometry source.</param>
            /// <returns>The number of vectors in the source.</returns>
            [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "ar_geometry_source_get_count")]
            public static extern int ar_geometry_source_get_count(IntPtr geometry_source);

            /// <summary>
            /// Get the type of per-vector data in the buffer.
            /// </summary>
            /// <param name="geometry_source">The geometry source.</param>
            /// <returns>The type of per-vector data in the buffer.</returns>
            [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "ar_geometry_source_get_format")]
            public static extern MTLVertexFormat ar_geometry_source_get_format(IntPtr geometry_source);

            /// <summary>
            /// Get the classification for each face of the mesh.
            /// </summary>
            /// <param name="mesh_geometry">The mesh geometry.</param>
            /// <returns>An instance of `ar_geometry_source_t`.</returns>
            [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "ar_mesh_geometry_get_classification")]
            public static extern IntPtr ar_mesh_geometry_get_classification(IntPtr mesh_geometry);

            /// <summary>
            /// Get the geometry of the mesh in the anchor's coordinate system.
            /// </summary>
            /// <param name="mesh_anchor">The mesh anchor.</param>
            /// <returns>An instance of `ar_mesh_geometry_t`.</returns>
            [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "ar_mesh_anchor_get_geometry")]
            public static extern IntPtr ar_mesh_anchor_get_geometry(IntPtr mesh_anchor);

            //TODO: Mesh anchor enumeration

            /// <summary>
            /// Determines whether this device supports the scene reconstruction provider.
            /// </summary>
            /// <returns><see langword="true"/> if the scene reconstruction provider is supported on this device. Otherwise, <see langword="false"/>.</returns>
            [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "ar_hand_tracking_provider_is_supported")]
            public static extern bool ar_scene_reconstruction_provider_is_supported();

            /// <summary>
            /// Get the authorization type required by the scene reconstruction provider.
            /// </summary>
            /// <returns>Authorization type.</returns>
            [DllImport(NativeApi.Constants.LibraryName,
                EntryPoint = "ar_scene_reconstruction_provider_get_required_authorization_type")]
            public static extern AR_Authorization_Type
                ar_scene_reconstruction_provider_get_required_authorization_type();
        }
    }
}
