using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for image tracking.
    /// Signatures should match image_tracking.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class ImageTracking
        {
            /// <summary>
            /// Handler triggered when there are updates to image anchors.
            /// <param name="added_anchors">Collection of anchors that are added.</param>
            /// <param name="updated_anchors">Collection of anchors that are updated.</param>
            /// <param name="removed_anchors">Collection of anchors that are removed.</param>
            /// </summary>
            public unsafe delegate void AR_Image_Tracking_Update_Handler(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count);

            /// <summary>
            /// Get the underlying tracked reference image from an image anchor.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="image_anchor">The image anchor to get the reference image from.</param>
            /// <returns>An instance of `ar_reference_image_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_image_anchor_get_reference_image")]
            public static extern IntPtr ar_image_anchor_get_reference_image(IntPtr image_anchor);

            // TODO: Wrapper function for enumerating anchors

            /// <summary>
            /// Get the reference image name.
            /// </summary>
            /// <param name="reference_image">Reference Image.</param>
            /// <returns>The name of the image, might be NULL if it hasn't been set before.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_reference_image_get_name")]
            public static extern IntPtr ar_reference_image_get_name(IntPtr reference_image);

            /// <summary>
            /// Get the width in meters of the reference image.
            /// </summary>
            /// <param name="reference_image">Reference Image.</param>
            /// <returns>The physical width of the image in meters.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_reference_image_get_physical_width")]
            public static extern float ar_reference_image_get_physical_width(IntPtr reference_image);

            /// <summary>
            /// Get the height in meters of the reference image.
            /// </summary>
            /// <param name="reference_image">Reference Image.</param>
            /// <returns>The physical width of the image in meters.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_reference_image_get_physical_height")]
            public static extern float ar_reference_image_get_physical_height(IntPtr reference_image);

            /// <summary>
            /// Create a collection of reference images initialized with an empty set.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>An instance of `ar_reference_images_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_reference_images_create")]
            public static extern IntPtr ar_reference_images_create();

            /// <summary>
            /// Load reference images from a bundle into a new collection.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="group_name">Group to load images from.</param>
            /// <param name="bundle">If nil, this will load the main bundle</param>
            /// <returns>New collection of reference images.</returns>
            [DllImport(Constants.LibraryName,
                EntryPoint = "ar_reference_images_load_reference_images_in_group")]
            public static extern IntPtr ar_reference_images_load_reference_images_in_group(IntPtr group_name,
                IntPtr bundle);

            /// <summary>
            /// Get the count of reference images in the collection.
            /// </summary>
            /// <param name="reference_images">The collection of reference images.</param>
            /// <returns>The number of reference images in the collection.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_reference_images_get_count")]
            public static extern int ar_reference_images_get_count(IntPtr reference_images);

            // TODO: wrapper function for enumerating reference images

            /// <summary>
            /// Create image tracking configuration.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>An instance of `ar_image_tracking_configuration_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_image_tracking_configuration_create")]
            public static extern IntPtr ar_image_tracking_configuration_create();

            /// <summary>
            /// Add reference images to the set to be tracked. The image tracking configuration can run without any reference images, but will not detect anything.
            /// </summary>
            /// <param name="image_tracking_configuration">Image tracking configuration.</param>
            /// <param name="reference_images">Reference images to add.</param>
            [DllImport(Constants.LibraryName,
                EntryPoint = "ar_image_tracking_configuration_add_reference_images")]
            public static extern void ar_image_tracking_configuration_add_reference_images(
                IntPtr image_tracking_configuration, IntPtr reference_images);

            // TODO: Wrapper function for add images completion handler

            /// <summary>
            /// Create an image tracking provider.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <param name="image_tracking_configuration">Image Tracking configuration.</param>
            /// <returns>An instance of `ar_image_tracking_provider`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_image_tracking_provider_create")]
            public static extern IntPtr ar_image_tracking_provider_create(IntPtr image_tracking_configuration);

            /// <summary>
            /// Set the image tracking update handler.
            /// </summary>
            /// <param name="image_tracking_provider">The image provider.</param>
            /// <param name="image_tracking_update_handler">The image tracking update handler.</param>
            [DllImport(Constants.LibraryName,
                EntryPoint = "UnityVisionOS_impl_ar_image_tracking_provider_set_update_handler")]
            public static extern void UnityVisionOS_impl_ar_image_tracking_provider_set_update_handler(
                IntPtr image_tracking_provider,
                AR_Image_Tracking_Update_Handler image_tracking_update_handler);

            /// <summary>
            /// Determines whether this device supports the image tracking provider.
            /// </summary>
            /// <returns><see langword="true"/> if the image tracking provider is supported on this device. Otherwise, <see langword="false"/>.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_image_tracking_provider_is_supported")]
            public static extern bool ar_image_tracking_provider_is_supported();

            /// <summary>
            /// Get the authorization type required by the image tracking provider.
            /// </summary>
            /// <returns>Authorization type.</returns>
            [DllImport(Constants.LibraryName,
                EntryPoint = "ar_image_tracking_provider_get_required_authorization_type")]
            public static extern AR_Authorization_Type ar_image_tracking_provider_get_required_authorization_type();

            /// <summary>
            /// Get a reference image from a collection of reference images.
            /// </summary>
            /// <param name="reference_images">The collection of reference images.</param>
            /// <param name="index">The index of the image to get.</param>
            /// <returns></returns>
            [DllImport(Constants.LibraryName, EntryPoint = "UnityVisionOS_impl_get_reference_image_at_index")]
            public static extern IntPtr UnityVisionOS_impl_get_reference_image_at_index(IntPtr reference_images,
                int index);
        }
    }
}
