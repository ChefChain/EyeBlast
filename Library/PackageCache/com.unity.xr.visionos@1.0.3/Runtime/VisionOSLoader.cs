using System;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

#if INCLUDE_UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
#endif

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Manages the lifecycle of VisionOS subsystems.
    /// </summary>
    public class VisionOSLoader : XRLoaderHelper
    {
        const string k_DisplaySubsystemId = "VisionOS-Display";
        const string k_InputSubsystemId = "VisionOS-Input";
        const string k_MeshSubsystemId = "VisionOS-Meshing";

        static readonly List<XRDisplaySubsystemDescriptor> k_DisplaySubsystemDescriptors = new();
        static readonly List<XRInputSubsystemDescriptor> k_InputSubsystemDescriptors = new();
        static readonly List<XRMeshSubsystemDescriptor> k_MeshSubsystemDescriptors = new();
        static readonly List<XRSessionSubsystemDescriptor> k_SessionSubsystemDescriptors = new();
        static readonly List<XRPlaneSubsystemDescriptor> k_PlaneSubsystemDescriptors = new();
        static readonly List<XRImageTrackingSubsystemDescriptor> k_ImageTrackingSubsystemDescriptors = new();
        static readonly List<XRAnchorSubsystemDescriptor> k_AnchorSubsystemDescriptors = new();

#if INCLUDE_UNITY_XR_HANDS
        static List<XRHandSubsystemDescriptor> s_HandSubsystemDescriptors = new();
#endif

        /// <summary>
        /// The [XRSessionSubsystem](xref:UnityEngine.XR.ARSubsystems.XRSessionSubsystem) whose lifecycle is managed by this loader.
        /// </summary>
        public XRSessionSubsystem sessionSubsystem => GetLoadedSubsystem<XRSessionSubsystem>();

        /// <summary>
        /// The [XRPlaneSubsystem](xref:UnityEngine.XR.ARSubsystems.XRPlaneSubsystem) whose lifecycle is managed by this loader.
        /// </summary>
        public XRPlaneSubsystem planeSubsystem => GetLoadedSubsystem<XRPlaneSubsystem>();

        /// <summary>
        /// The [XRAnchorSubsystem](xref:UnityEngine.XR.ARSubsystems.XRAnchorSubsystem) whose lifecycle is managed by this loader.
        /// </summary>
        public XRAnchorSubsystem anchorSubsystem => GetLoadedSubsystem<XRAnchorSubsystem>();

        /// <summary>
        /// The [XRInputSubsystem](xref:UnityEngine.XR.XRInputSubsystem) whose lifecycle is managed by this loader.
        /// </summary>
        public XRInputSubsystem inputSubsystem => GetLoadedSubsystem<XRInputSubsystem>();

        /// <summary>
        /// The [XRImageTrackingSubsystem](xref:UnityEngine.XR.ARSubsystems.XRImageTrackingSubsystem) whose lifecycle is managed by this loader.
        /// </summary>
        public XRImageTrackingSubsystem imageTrackingSubsystem => GetLoadedSubsystem<XRImageTrackingSubsystem>();

        /// <summary>
        /// The [XRMeshSubsystem](xref:UnityEngine.XR.XRMeshSubsystem) whose lifecycle is
        /// managed by this loader.
        /// </summary>
        public XRMeshSubsystem meshSubsystem => GetLoadedSubsystem<XRMeshSubsystem>();

#if INCLUDE_UNITY_XR_HANDS
        /// <summary>
        /// The [XRHandSubsystem](xref:UnityEngine.XR.XRHandSubsystem) whose lifecycle is
        /// managed by this loader.
        /// </summary>
        public XRHandSubsystem handSubsystem => GetLoadedSubsystem<XRHandSubsystem>();

        XRHandProviderUtility.SubsystemUpdater m_Updater;
#endif

        /// <summary>
        /// Initializes the loader.
        /// </summary>
        /// <returns>`True` if the session subsystem was successfully created, otherwise `false`.</returns>
        public override bool Initialize()
        {
            // TODO: #ifdef for runtime-only after removing macos stub

            // TODO: Remove targetFrameRate setting or find a better place to put it (or make it optional?)
            Application.targetFrameRate = 90;
            CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(k_DisplaySubsystemDescriptors, k_DisplaySubsystemId);
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(k_InputSubsystemDescriptors, k_InputSubsystemId);
            CreateSubsystem<XRMeshSubsystemDescriptor, XRMeshSubsystem>(k_MeshSubsystemDescriptors, k_MeshSubsystemId);
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(k_SessionSubsystemDescriptors, VisionOSSessionSubsystem.sessionSubsystemId);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(k_PlaneSubsystemDescriptors, VisionOSPlaneSubsystem.planeSubsystemId);
            CreateSubsystem<XRImageTrackingSubsystemDescriptor, XRImageTrackingSubsystem>(k_ImageTrackingSubsystemDescriptors, VisionOSImageTrackingSubsystem.imageTrackingSubsystemId);
            CreateSubsystem<XRAnchorSubsystemDescriptor, XRAnchorSubsystem>(k_AnchorSubsystemDescriptors, VisionOSAnchorSubsystem.anchorSubsystemId);

#if INCLUDE_UNITY_XR_HANDS
            CreateSubsystem<XRHandSubsystemDescriptor, XRHandSubsystem>(s_HandSubsystemDescriptors, VisionOSHandProvider.handSubsystemId);
            m_Updater = new XRHandProviderUtility.SubsystemUpdater(GetLoadedSubsystem<XRHandSubsystem>());
#endif

            var loadedSessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();
            if (loadedSessionSubsystem == null)
            {
                Debug.LogError("Failed to load visionOS session subsystem.");
            }

            return loadedSessionSubsystem != null;
        }

        /// <summary>
        /// This method does nothing for managed subsystems. Subsystems must be started individually.
        /// </summary>
        /// <returns>Returns `true` on iOS. Returns `false` otherwise.</returns>
        public override bool Start()
        {
            StartSubsystem<XRDisplaySubsystem>();
            StartSubsystem<XRInputSubsystem>();

            // TODO: #ifdef for runtime-only after removing macos stub
#if INCLUDE_UNITY_XR_HANDS
            StartSubsystem<XRHandSubsystem>();
            m_Updater?.Start();
#endif
            return true;
        }

        /// <summary>
        /// This method does nothing for managed subsystems. Subsystems must be stopped individually.
        /// </summary>
        /// <returns>Returns `true` on iOS. Returns `false` otherwise.</returns>
        public override bool Stop()
        {
            StopSubsystem<XRDisplaySubsystem>();
            StopSubsystem<XRInputSubsystem>();

            // TODO: #ifdef for runtime-only after removing macos stub
#if INCLUDE_UNITY_XR_HANDS
            StopSubsystem<XRHandSubsystem>();
            m_Updater?.Stop();
#endif

            return true;
        }

        /// <summary>
        /// Destroys each subsystem.
        /// </summary>
        /// <returns>Always returns `true`.</returns>
        public override bool Deinitialize()
        {
            DestroySubsystem<XRDisplaySubsystem>();
            DestroySubsystem<XRInputSubsystem>();
            DestroySubsystem<XRMeshSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();
            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XRImageTrackingSubsystem>();
            DestroySubsystem<XRAnchorSubsystem>();

#if INCLUDE_UNITY_XR_HANDS
            DestroySubsystem<XRHandSubsystem>();
#endif
            return true;
        }
    }
}
