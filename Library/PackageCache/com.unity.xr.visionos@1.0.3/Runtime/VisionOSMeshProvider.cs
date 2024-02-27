using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    class VisionOSMeshProvider : IVisionOSProvider
    {
        public AR_Authorization_Type RequiredAuthorizationType => NativeApi.SceneReconstruction.ar_scene_reconstruction_provider_get_required_authorization_type();
        public bool IsSupported => NativeApi.SceneReconstruction.ar_scene_reconstruction_provider_is_supported();
        public bool ShouldBeActive => GetMeshSubsystemStatus() == SubsystemStatus.Started;
        public IntPtr CurrentProvider { get; private set; } = IntPtr.Zero;

        public bool TryCreateNativeProvider(Feature features, out IntPtr provider)
        {
            if (!IsSupported)
            {
                Debug.LogWarning("Scene reconstruction provider is not supported");
                provider = IntPtr.Zero;
                return false;
            }

            var mode = AR_Scene_Reconstruction_Mode.Default;
            if ((features & Feature.MeshClassification) != 0)
                mode = AR_Scene_Reconstruction_Mode.Classification;

            provider = CreateSceneReconstructionProvider(mode);
            CurrentProvider = provider;
            if (provider == IntPtr.Zero)
            {
                Debug.LogWarning("Failed to create scene reconstruction provider.");
                return false;
            }

            return true;
        }

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_CreateSceneReconstructionProvider")]
        static extern IntPtr CreateSceneReconstructionProvider(AR_Scene_Reconstruction_Mode mode);

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_GetMeshSubsystemStatus")]
        static extern SubsystemStatus GetMeshSubsystemStatus();
    }
}
