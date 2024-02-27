using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// VisionOS implementation of the <c>XRImageTrackingSubsystem</c>.
    /// </summary>
    [Preserve]
    public sealed class VisionOSImageTrackingSubsystem : XRImageTrackingSubsystem
    {
        internal const string imageTrackingSubsystemId = "VisionOS-ImageTracking";
        const Feature k_ImageTrackingFeature = Feature.ImageTracking;

        class VisionOSImageTrackingProvider : Provider, IVisionOSProvider
        {
            const int k_InitialSize = 16;
            static VisionOSImageTrackingProvider s_Instance;

            static readonly unsafe NativeApi.ImageTracking.AR_Image_Tracking_Update_Handler k_ImageTrackingUpdateHandler = ImageTrackingUpdateHandler;

            IntPtr m_ImageTrackingConfiguration;

            readonly Dictionary<TrackableId, XRTrackedImage> m_TempAddedImages = new(k_InitialSize);
            readonly Dictionary<TrackableId, XRTrackedImage> m_TempUpdatedImages = new(k_InitialSize);
            readonly HashSet<TrackableId> m_TempRemovedImages = new(k_InitialSize);

            NativeArray<XRTrackedImage> m_AddedImages = new(k_InitialSize, Allocator.Persistent);
            NativeArray<XRTrackedImage> m_UpdatedImages = new(k_InitialSize, Allocator.Persistent);
            NativeArray<TrackableId> m_RemovedImages = new(k_InitialSize, Allocator.Persistent);

            VisionOSImageDatabase m_ImageDatabase;

            public AR_Authorization_Type RequiredAuthorizationType => NativeApi.ImageTracking.ar_image_tracking_provider_get_required_authorization_type();
            public bool IsSupported => NativeApi.ImageTracking.ar_image_tracking_provider_is_supported();
            public bool ShouldBeActive => running;
            public IntPtr CurrentProvider { get; private set; } = IntPtr.Zero;

            public VisionOSImageTrackingProvider()
            {
                s_Instance = this;
                VisionOSProviderRegistration.RegisterProvider(k_ImageTrackingFeature, this);
            }

            public override void Start() { }

            public override void Stop() { }

            public override void Destroy()
            {
                VisionOSProviderRegistration.UnregisterProvider(k_ImageTrackingFeature, this);
                m_AddedImages.Dispose();
                m_UpdatedImages.Dispose();
                m_RemovedImages.Dispose();
                m_TempAddedImages.Clear();
                m_TempUpdatedImages.Clear();
                m_TempRemovedImages.Clear();
            }

            public bool TryCreateNativeProvider(Feature features, out IntPtr provider)
            {
                if (!IsSupported)
                {
                    Debug.LogWarning("Image tracking provider is not supported");
                    provider = IntPtr.Zero;
                    return false;
                }

                // TODO: Warn about starting with no reference images?
                // Configuration may have already been created by imageLibrary setter, otherwise no library is selected, so use an empty configuration
                if (m_ImageTrackingConfiguration == IntPtr.Zero)
                    m_ImageTrackingConfiguration = NativeApi.ImageTracking.ar_image_tracking_configuration_create();

                provider = NativeApi.ImageTracking.ar_image_tracking_provider_create(m_ImageTrackingConfiguration);
                CurrentProvider = provider;
                if (provider == IntPtr.Zero)
                {
                    Debug.LogWarning("Failed to create image tracking provider.");
                    return false;
                }

                NativeApi.ImageTracking.UnityVisionOS_impl_ar_image_tracking_provider_set_update_handler(provider, k_ImageTrackingUpdateHandler);

                return true;
            }

            // ReSharper disable InconsistentNaming
            // TODO: Use IntPtr instead of void*?
            [MonoPInvokeCallback(typeof(NativeApi.ImageTracking.AR_Image_Tracking_Update_Handler))]
            static unsafe void ImageTrackingUpdateHandler(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count)
            {
                s_Instance.ProcessImageUpdates(added_anchors, added_anchor_count, updated_anchors, updated_anchor_count, removed_anchors, removed_anchor_count);
            }

            bool GetTrackedImage(IntPtr imageAnchor, out XRTrackedImage trackedImage)
            {
                var isTracked = NativeApi.Anchor.ar_trackable_anchor_is_tracked(imageAnchor);
                var pose = NativeApi.Utilities.GetWorldPose(imageAnchor);
                var trackableId = NativeApi.Utilities.GetTrackableId(imageAnchor);
                var trackingState = isTracked ? TrackingState.Tracking : TrackingState.None;
                var referenceImagePointer = NativeApi.ImageTracking.ar_image_anchor_get_reference_image(imageAnchor);
                var referenceImageName = Marshal.PtrToStringAnsi(NativeApi.ImageTracking.ar_reference_image_get_name(referenceImagePointer));
                if (!m_ImageDatabase.TryGetImageForName(referenceImageName, out var referenceImage))
                {
                    Debug.LogError($"Cannot find id for AR reference image {referenceImagePointer} with name {referenceImageName}");
                    trackedImage = default;
                    return false;
                }

                trackedImage = new XRTrackedImage(trackableId, referenceImage.guid, pose, referenceImage.size, trackingState, imageAnchor);
                return true;
            }

            IEnumerable<XRTrackedImage> ProcessImageArray(NativeArray<IntPtr> imageAnchors)
            {
                foreach (var imageAnchor in imageAnchors)
                {
                    if (!GetTrackedImage(imageAnchor, out var trackedImage))
                        continue;

                    var trackableId = trackedImage.trackableId;
                    if (trackableId == TrackableId.invalidId)
                    {
                        Debug.LogError($"Trying to add AR image anchor with invalid trackable id: {trackedImage.trackableId}");
                        continue;
                    }

                    yield return trackedImage;
                }
            }

            unsafe void ProcessImageUpdates(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count)
            {
                var imageAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(added_anchors, added_anchor_count, Allocator.None);
                foreach (var image in ProcessImageArray(imageAnchors))
                {
                    // NB: Using a dictionary does not preserve update order
                    m_TempAddedImages[image.trackableId] = image;
                }

                imageAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(updated_anchors, updated_anchor_count, Allocator.None);
                foreach (var image in ProcessImageArray(imageAnchors))
                {
                    // NB: Using a dictionary does not preserve update order
                    // If the plane is already in added planes, we haven't acquired it yet, so replace it instead of adding to updated planes
                    var trackableId = image.trackableId;
                    if (m_TempAddedImages.ContainsKey(trackableId))
                        m_TempAddedImages[trackableId] = image;
                    else
                        m_TempUpdatedImages[trackableId] = image;
                }

                imageAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(removed_anchors, removed_anchor_count, Allocator.None);
                foreach (var image in imageAnchors)
                {
                    var trackableId = NativeApi.Utilities.GetTrackableId(image);
                    var removed = m_TempAddedImages.Remove(trackableId);
                    removed |= m_TempUpdatedImages.Remove(trackableId);

                    // Only add to removed images if we have already acquired this image (i.e. it does not exist in the other temp dictionaries)
                    if (!removed)
                        m_TempRemovedImages.Add(trackableId);
                }
            }

            // ReSharper restore InconsistentNaming

            public override RuntimeReferenceImageLibrary CreateRuntimeLibrary(
                XRReferenceImageLibrary serializedLibrary)
            {
                return new VisionOSImageDatabase(serializedLibrary);
            }

            public override RuntimeReferenceImageLibrary imageLibrary
            {
                set
                {
                    if (!NativeApi.ImageTracking.ar_image_tracking_provider_is_supported())
                    {
                        Debug.LogWarning("Image tracking is not supported");
                        return;
                    }

                    if (value == null)
                    {
                        m_ImageDatabase = null;
                        Stop();
                    }
                    else if (value is VisionOSImageDatabase database)
                    {
                        m_ImageDatabase = database;

                        // TODO: Is it possible to remove images without creating a new configuration?
                        m_ImageTrackingConfiguration = NativeApi.ImageTracking.ar_image_tracking_configuration_create();
                        NativeApi.ImageTracking.ar_image_tracking_configuration_add_reference_images(m_ImageTrackingConfiguration, database.self);

                        // Restart session with new provider if one exists, otherwise assume subsystem is not started, and we will create one with the correct configuration at start
                        if (CurrentProvider != IntPtr.Zero)
                        {
                            var sessionSubsystem = VisionOSSessionSubsystem.VisionOSSessionProvider.Instance;
                            sessionSubsystem.RequestRestart();
                            CurrentProvider = NativeApi.ImageTracking.ar_image_tracking_provider_create(m_ImageTrackingConfiguration);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"{value.GetType().Name} is not a valid VisionOS image library.");
                    }
                }
            }

            public override unsafe TrackableChanges<XRTrackedImage> GetChanges(
                XRTrackedImage defaultTrackedImage,
                Allocator allocator)
            {
                try
                {
                    NativeApi.Utilities.DictionaryToNativeArray(m_TempAddedImages, ref m_AddedImages);
                    NativeApi.Utilities.DictionaryToNativeArray(m_TempUpdatedImages, ref m_UpdatedImages);
                    NativeApi.Utilities.HashSetToNativeArray(m_TempRemovedImages, ref m_RemovedImages);

                    var changes = new TrackableChanges<XRTrackedImage>(
                        m_AddedImages.GetUnsafePtr(), m_TempAddedImages.Count,
                        m_UpdatedImages.GetUnsafePtr(), m_TempUpdatedImages.Count,
                        m_RemovedImages.GetUnsafePtr(), m_TempRemovedImages.Count,
                        defaultTrackedImage, sizeof(BoundedPlane), allocator);

                    return changes;
                }
                finally
                {
                    m_TempAddedImages.Clear();
                    m_TempUpdatedImages.Clear();
                    m_TempRemovedImages.Clear();
                }
            }

            // TODO: Handle messaging for removal of requestedMaxNumberOfMovingImages
            public override int requestedMaxNumberOfMovingImages
            {
                get => 0;

                // ReSharper disable once ValueParameterNotUsed
                set
                {
                    // Suppress warning for default value
                    if (value == 0)
                        return;

                    Debug.LogWarning($"{nameof(requestedMaxNumberOfMovingImages)} is not supported.");
                }
            }

            public override int currentMaxNumberOfMovingImages => 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRImageTrackingSubsystemDescriptor.Create(new XRImageTrackingSubsystemDescriptor.Cinfo
            {
                id = imageTrackingSubsystemId,
                providerType = typeof(VisionOSImageTrackingProvider),
                subsystemTypeOverride = typeof(VisionOSImageTrackingSubsystem),
                supportsMovingImages = true,
                supportsMutableLibrary = true,
                requiresPhysicalImageDimensions = true,

                // TODO: Is image validation supported?
                supportsImageValidation = true
            });
        }
    }
}
