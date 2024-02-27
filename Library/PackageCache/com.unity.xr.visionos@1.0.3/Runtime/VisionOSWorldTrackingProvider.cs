using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    class VisionOSWorldTrackingProvider : IVisionOSProvider
    {
        internal static VisionOSWorldTrackingProvider Instance { get; private set; }
        internal event Action<IntPtr> OnCreated;

        public AR_Authorization_Type RequiredAuthorizationType => NativeApi.WorldTracking.ar_world_tracking_provider_get_required_authorization_type();
        public bool IsSupported => NativeApi.WorldTracking.ar_world_tracking_provider_is_supported();

        public bool ShouldBeActive => GetTrackingSubsystemStatus() == SubsystemStatus.Started;

        public IntPtr CurrentProvider { get; private set; } = IntPtr.Zero;

        public VisionOSWorldTrackingProvider()
        {
            Instance = this;
        }

        public bool TryCreateNativeProvider(Feature features, out IntPtr provider)
        {
            if (!IsSupported)
            {
                Debug.LogWarning("World tracking provider is not supported.");
                provider = IntPtr.Zero;
                return false;
            }

            provider = CreateWorldTrackingProvider();
            CurrentProvider = provider;
            OnCreated?.Invoke(provider);
            if (provider == IntPtr.Zero)
            {
                Debug.LogWarning("Failed to create world tracking provider.");
                return false;
            }

            return true;
        }

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_CreateWorldTrackingProvider")]
        static extern IntPtr CreateWorldTrackingProvider();

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_GetTrackingSubsystemStatus")]
        static extern SubsystemStatus GetTrackingSubsystemStatus();
    }
}
