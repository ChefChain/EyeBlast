using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// VisionOS extension methods to the [XRMeshSubsystem](https://docs.unity3d.com/ScriptReference/XR.XRMeshSubsystem.html).
    /// </summary>
    public static class VisionOSMeshSubsystemExtensions
    {
        /// <summary>
        /// Get the face classifications for the given mesh ID.
        /// </summary>
        /// <param name="subsystem">The meshing subsystem.</param>
        /// <param name="meshId">The trackable ID representing the mesh.</param>
        /// <param name="allocator">The memory allocator type to use in allocating the native array memory.</param>
        /// <returns>
        /// An array of mesh classifications, one for each face in the mesh.
        /// </returns>
        public static unsafe NativeArray<ARMeshClassification> GetFaceClassifications(this XRMeshSubsystem subsystem, TrackableId meshId, Allocator allocator)
        {
            var meshAnchor = GetMeshAnchorForMeshId(meshId);
            var geometry = NativeApi.SceneReconstruction.ar_mesh_anchor_get_geometry(meshAnchor);
            var classifications = NativeApi.SceneReconstruction.ar_mesh_geometry_get_classification(geometry);
            var numClassifications = 0;
            if (classifications != IntPtr.Zero)
            {
                numClassifications = NativeApi.SceneReconstruction.ar_geometry_source_get_count(classifications);
            }

            var meshClassifications = new NativeArray<ARMeshClassification>(numClassifications, allocator);
            if (classifications != IntPtr.Zero)
            {
                var format = NativeApi.SceneReconstruction.ar_geometry_source_get_format(classifications);
                if (format != MTLVertexFormat.MTLVertexFormatUChar)
                    throw new InvalidOperationException($"Unexpected vertex format {format} getting mesh classifications. Only {nameof(MTLVertexFormat.MTLVertexFormatUChar)} is supported");

                var buffer = NativeApi.SceneReconstruction.UnityVisionOS_impl_ar_geometry_source_get_buffer(classifications);
                var tmp = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ARMeshClassification>((void*)buffer, numClassifications, Allocator.None);
                meshClassifications.CopyFrom(tmp);
            }

            return meshClassifications;
        }

        /// <summary>
        /// Whether mesh classification is enabled.
        /// </summary>
        /// <param name="subsystem">The meshing subsystem.</param>
        /// <returns>
        /// <c>true</c> if the mesh classification is enabled. Otherwise, <c>false</c>.
        /// </returns>
        public static bool GetClassificationEnabled(this XRMeshSubsystem subsystem)
        {
            return VisionOSSessionSubsystem.VisionOSSessionProvider.Instance.GetSceneReconstructionMode() == AR_Scene_Reconstruction_Mode.Classification;
        }

        /// <summary>
        /// Sets whether mesh classification should be enabled.
        /// </summary>
        /// <param name="subsystem">The meshing subsystem.</param>
        /// <param name="enabled">Whether the mesh classification should be enabled.</param>
        public static void SetClassificationEnabled(this XRMeshSubsystem subsystem, bool enabled)
        {
            VisionOSSessionSubsystem.VisionOSSessionProvider.Instance.SetSceneReconstructionMode(
                enabled ? AR_Scene_Reconstruction_Mode.Classification : AR_Scene_Reconstruction_Mode.Default);
        }

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_GetMeshAnchorForMeshId")]
        static extern IntPtr GetMeshAnchorForMeshId(TrackableId trackableId);
    }
}
