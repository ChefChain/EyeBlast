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
    /// The VisionOS implementation of the <c>XRAnchorSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class VisionOSAnchorSubsystem : XRAnchorSubsystem
    {
        internal const string anchorSubsystemId = "VisionOS-Anchor";

        // Note: This type does not implement IVisionOSProvider because World Tracking Data Provider is required for input,
        // and managed by VisionOSWorldTrackingProvider
        internal class VisionOSAnchorProvider : Provider
        {
            const int k_InitialSize = 16;

            static VisionOSAnchorProvider s_Instance;
            static readonly unsafe NativeApi.WorldTracking.AR_World_Tracking_Update_Handler k_WorldTrackingUpdateHandler = WorldTrackingUpdateHandler;
            static readonly NativeApi.WorldTracking.AR_World_Tracking_Add_Anchor_Completion_Handler k_AddAnchorCompletionHandler = AddAnchorCompletionHandler;
            static readonly NativeApi.WorldTracking.AR_World_Tracking_Remove_Anchor_Completion_Handler k_RemoveAnchorCompletionHandler = RemoveAnchorCompletionHandler;

            IntPtr m_WorldTrackingProvider = IntPtr.Zero;

            readonly Dictionary<TrackableId, XRAnchor> m_TempAddedAnchors = new(k_InitialSize);
            readonly Dictionary<TrackableId, XRAnchor> m_TempUpdatedAnchors = new(k_InitialSize);
            readonly HashSet<TrackableId> m_TempRemovedAnchors = new(k_InitialSize);

            NativeArray<XRAnchor> m_AddedAnchors = new(k_InitialSize, Allocator.Persistent);
            NativeArray<XRAnchor> m_UpdatedAnchors = new(k_InitialSize, Allocator.Persistent);
            NativeArray<TrackableId> m_RemovedAnchors = new(k_InitialSize, Allocator.Persistent);

            public VisionOSAnchorProvider()
            {
                s_Instance = this;
            }

            public override void Destroy()
            {
                m_AddedAnchors.Dispose();
                m_UpdatedAnchors.Dispose();
                m_RemovedAnchors.Dispose();
                m_TempAddedAnchors.Clear();
                m_TempUpdatedAnchors.Clear();
                m_TempRemovedAnchors.Clear();
            }

            public override void Start()
            {
                var worldTrackingProvider = VisionOSWorldTrackingProvider.Instance;
                worldTrackingProvider.OnCreated += OnWorldTrackingProviderCreated;
                m_WorldTrackingProvider = worldTrackingProvider.CurrentProvider;
            }

            public override void Stop()
            {
                VisionOSWorldTrackingProvider.Instance.OnCreated -= OnWorldTrackingProviderCreated;
            }

            // TODO: Should AnchorSubsystem just create its own data provider?
            public void OnWorldTrackingProviderCreated(IntPtr provider)
            {
                m_WorldTrackingProvider = provider;
                if (m_WorldTrackingProvider != IntPtr.Zero)
                    NativeApi.WorldTracking.UnityVisionOS_impl_ar_world_tracking_provider_set_anchor_update_handler(m_WorldTrackingProvider, k_WorldTrackingUpdateHandler);
            }

            // ReSharper disable InconsistentNaming
            // TODO: Use IntPtr instead of void*?
            [MonoPInvokeCallback(typeof(NativeApi.WorldTracking.AR_World_Tracking_Update_Handler))]
            static unsafe void WorldTrackingUpdateHandler(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count)
            {
                // TODO: Unsubscribe from updates? Re-create provider?
                if (!s_Instance.running)
                    return;

                s_Instance.ProcessAnchorUpdates(added_anchors, added_anchor_count, updated_anchors, updated_anchor_count, removed_anchors, removed_anchor_count);
            }

            static XRAnchor GetWorldAnchor(IntPtr worldAnchor)
            {
                var isTracked = NativeApi.Anchor.ar_trackable_anchor_is_tracked(worldAnchor);

                // TODO: For some reason this method was just returning the same pointer you gave it, so it needed to be wrapped in ObjC
                var transformFloatArray = NativeApi.Anchor.UnityVisionOS_impl_ar_anchor_get_origin_from_anchor_transform_to_float_array(worldAnchor);
                var worldMatrix = Marshal.PtrToStructure<FloatArrayToMatrix4x4>(transformFloatArray);
                var pose = new Pose(worldMatrix.GetPosition(), worldMatrix.GetRotation());

                var trackableId = NativeApi.Utilities.GetTrackableId(worldAnchor);
                var trackingState = isTracked ? TrackingState.Tracking : TrackingState.None;

                return new XRAnchor(trackableId, pose, trackingState, worldAnchor);
            }

            static XRAnchor GetWorldAnchorWithPose(IntPtr worldAnchor, Pose pose)
            {
                var trackableId = NativeApi.Utilities.GetTrackableId(worldAnchor);

                // TODO: Should we default to Tracking, Limited, or None?
                return new XRAnchor(trackableId, pose, TrackingState.None, worldAnchor);
            }

            static IEnumerable<XRAnchor> ProcessAnchorArray(NativeArray<IntPtr> worldAnchors)
            {
                foreach (var worldAnchor in worldAnchors)
                {
                    // Trying to get anchor info outside of update callback was failing, so create the xr anchor here,
                    // even though it might be removed or updated before we actually acquire it
                    var xrAnchor = GetWorldAnchor(worldAnchor);
                    var trackableId = xrAnchor.trackableId;
                    if (trackableId == TrackableId.invalidId)
                    {
                        Debug.LogError($"Trying to add AR anchor with invalid trackable id: {xrAnchor.trackableId}");
                        continue;
                    }

                    yield return xrAnchor;
                }
            }

            unsafe void ProcessAnchorUpdates(void* added_anchors, int added_anchor_count,
                void* updated_anchors, int updated_anchor_count, void* removed_anchors, int removed_anchor_count)
            {
                var worldAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(added_anchors, added_anchor_count, Allocator.None);
                foreach (var anchor in ProcessAnchorArray(worldAnchors))
                {
                    // NB: Using a dictionary does not preserve update order
                    var trackableId = anchor.trackableId;
                    m_TempAddedAnchors[trackableId] = anchor;
                }

                worldAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(updated_anchors, updated_anchor_count, Allocator.None);
                foreach (var anchor in ProcessAnchorArray(worldAnchors))
                {
                    // NB: Using a dictionary does not preserve update order
                    // If the anchor is already in added anchors, we haven't acquired it yet, so replace it instead of adding to updated anchors
                    var trackableId = anchor.trackableId;
                    if (m_TempAddedAnchors.ContainsKey(trackableId))
                        m_TempAddedAnchors[trackableId] = anchor;
                    else
                        m_TempUpdatedAnchors[trackableId] = anchor;
                }

                worldAnchors = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<IntPtr>(removed_anchors, removed_anchor_count, Allocator.None);
                foreach (var anchor in worldAnchors)
                {
                    var trackableId = NativeApi.Utilities.GetTrackableId(anchor);
                    var removed = m_TempAddedAnchors.Remove(trackableId);
                    removed |= m_TempUpdatedAnchors.Remove(trackableId);

                    // Only add to removed anchors if we have already acquired this anchor (i.e. it does not exist in the other temp dictionaries)
                    if (!removed)
                        m_TempRemovedAnchors.Add(trackableId);
                }
            }

            public override unsafe TrackableChanges<XRAnchor> GetChanges(
                XRAnchor defaultAnchor,
                Allocator allocator)
            {
                try
                {
                    NativeApi.Utilities.DictionaryToNativeArray(m_TempAddedAnchors, ref m_AddedAnchors);
                    NativeApi.Utilities.DictionaryToNativeArray(m_TempUpdatedAnchors, ref m_UpdatedAnchors);
                    NativeApi.Utilities.HashSetToNativeArray(m_TempRemovedAnchors, ref m_RemovedAnchors);

                    var changes = new TrackableChanges<XRAnchor>(
                        m_AddedAnchors.GetUnsafePtr(), m_TempAddedAnchors.Count,
                        m_UpdatedAnchors.GetUnsafePtr(), m_TempUpdatedAnchors.Count,
                        m_RemovedAnchors.GetUnsafePtr(), m_TempRemovedAnchors.Count,
                        defaultAnchor, sizeof(XRAnchor), allocator);

                    return changes;
                }
                finally
                {
                    m_TempAddedAnchors.Clear();
                    m_TempUpdatedAnchors.Clear();
                    m_TempRemovedAnchors.Clear();
                }
            }

            public override bool TryAddAnchor(Pose pose, out XRAnchor anchor)
            {
                if (m_WorldTrackingProvider == IntPtr.Zero)
                {
                    Debug.LogError("Cannot add AR anchor; World tracking provider is null.");
                    anchor = XRAnchor.defaultValue;
                    return false;
                }

                var transform = NativeApi.Utilities.GetMatrixFloats(pose);
                var anchorPtr = NativeApi.WorldTracking.UnityVisionOS_impl_ar_world_anchor_create_with_transform_float_array(transform);
                NativeApi.WorldTracking.UnityVisionOS_impl_ar_world_tracking_provider_add_anchor(m_WorldTrackingProvider, anchorPtr, k_AddAnchorCompletionHandler);

                anchor = GetWorldAnchorWithPose(anchorPtr, pose);

                // Completion handler is async, no way to return false here
                // TODO: New API for TryAddAnchorAsync?
                return true;
            }

            [MonoPInvokeCallback(typeof(NativeApi.WorldTracking.AR_World_Tracking_Add_Anchor_Completion_Handler))]
            static void AddAnchorCompletionHandler(IntPtr anchor, bool successful, IntPtr error)
            {
                if (!successful)
                {
                    var errorCode = (AR_World_Tracking_Error_Code)0;
                    if (error != IntPtr.Zero)
                        errorCode = (AR_World_Tracking_Error_Code)NativeApi.Error.ar_error_get_error_code(error);

                    Debug.LogError($"Failed to add AR anchor. Error code: {errorCode}");
                }
            }

            public override bool TryRemoveAnchor(TrackableId anchorId)
            {
                if (m_WorldTrackingProvider == IntPtr.Zero)
                {
                    Debug.LogError("Cannot remove AR anchor; World tracking provider is null.");
                    return false;
                }

                var uuid = NativeApi.Utilities.TrackableIdToPtr(anchorId);
                NativeApi.WorldTracking.UnityVisionOS_impl_ar_world_tracking_provider_remove_anchor_with_identifier(m_WorldTrackingProvider, uuid, k_RemoveAnchorCompletionHandler);

                // Completion handler is async, no way to return false here
                // TODO: New API for TryRemoveAnchorAsync?
                return true;
            }

            [MonoPInvokeCallback(typeof(NativeApi.WorldTracking.AR_World_Tracking_Remove_Anchor_Completion_Handler))]
            static void RemoveAnchorCompletionHandler(IntPtr anchor, bool successful, IntPtr error)
            {
                if (!successful)
                {
                    var errorCode = (AR_World_Tracking_Error_Code)0;
                    if (error != IntPtr.Zero)
                        errorCode = (AR_World_Tracking_Error_Code)NativeApi.Error.ar_error_get_error_code(error);

                    Debug.LogError($"Failed to remove AR anchor. Error code: {errorCode}");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRAnchorSubsystemDescriptor.Cinfo
            {
                id = anchorSubsystemId,
                providerType = typeof(VisionOSAnchorProvider),
                subsystemTypeOverride = typeof(VisionOSAnchorSubsystem),

                // TODO: Support attachment
                supportsTrackableAttachments = false
            };

            XRAnchorSubsystemDescriptor.Create(cinfo);
        }
    }
}
