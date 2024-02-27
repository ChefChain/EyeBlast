using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.XR.VisionOS;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Assertions;

namespace UnityEngine.XR.VisionOS
{
    sealed class VisionOSImageDatabase : MutableRuntimeReferenceImageLibrary
    {
        unsafe struct NativeView
        {
            [UsedImplicitly]
            public void* data;

            [UsedImplicitly]
            public int count;
        }

        static readonly StringBuilder k_StringBuilder = new();

        internal IntPtr self { get; }

        const string k_ResourceGroupName = "ARReferenceImages";

        // TODO: Pointers are not equivalent?
        //readonly Dictionary<IntPtr, XRReferenceImage> m_ReferenceImages = new();
        readonly Dictionary<string, XRReferenceImage> m_ReferenceImages = new();
        readonly Dictionary<string, SerializableGuid> m_NameToGuid = new();
        readonly Dictionary<string, SerializableGuid> m_NameToTextureGuid = new();

        static IntPtr CreateImageDatabase(XRReferenceImageLibrary library)
        {
            if (library == null)
            {
                return NativeApi.ImageTracking.ar_reference_images_create();
            }

            var bundle = GetCFBundleRef(library);
            if (bundle == IntPtr.Zero)
                throw new InvalidOperationException($"Could not create reference image library '{library.name}'. Unable to create resource bundle.");

            var groupName = GetARResourceGroupName(library);
            return NativeApi.ImageTracking.ar_reference_images_load_reference_images_in_group(Marshal.StringToHGlobalAnsi(groupName), bundle);
        }

        // ReSharper disable InconsistentNaming
        static unsafe IntPtr GetCFBundleRef(XRReferenceImageLibrary library)
        {
            var mainBundle = GetMainBundle();

            // If no data exists in the XRReferenceImageLibrary, then try to look it up in the main bundle
            if (!library.dataStore.TryGetValue(VisionOSPackageInfo.identifier, out var assetCatalogData))
            {
                // Do we need to call retain?
                //NSObject.Retain(mainBundle);
                return mainBundle;
            }

            fixed (void* data = assetCatalogData)
            {
                return CreateNSBundleFromCompiledAssetCatalog(library.guid.ToString(), new NativeView
                {
                    data = data,
                    count = assetCatalogData.Length
                });
            }
        }

        static string GetARResourceGroupName(XRReferenceImageLibrary library)
        {
            if (library.dataStore.ContainsKey(VisionOSPackageInfo.identifier))
                return k_ResourceGroupName;

            // Otherwise, construct the name based on the library's name + guid
            k_StringBuilder.Length = 0;
            k_StringBuilder.Append(library.name);
            k_StringBuilder.Append("_");
            k_StringBuilder.Append(library.guid.ToUUIDString());
            return k_StringBuilder.ToString();
        }

        public static string GetReferenceImageName(string name, Guid guid)
        {
            return $"{name}_{guid.ToUUIDString()}";
        }

        public VisionOSImageDatabase(XRReferenceImageLibrary serializedLibrary)
        {
            self = CreateImageDatabase(serializedLibrary);
            foreach (var image in serializedLibrary)
            {
                var name = GetReferenceImageName(image.name, image.guid);
                m_NameToGuid[name] = AsSerializedGuid(image.guid);
                m_NameToTextureGuid[name] = AsSerializedGuid(image.textureGuid);
            }
        }

        /// <summary>
        /// (Read Only) Whether image validation is supported. `True` on iOS 13 and later.
        /// </summary>

        // TODO: Does VisionOS support validation?
        protected override JobHandle ScheduleAddImageJobImpl(NativeSlice<byte> imageBytes, Vector2Int sizeInPixels, TextureFormat format, XRReferenceImage referenceImage, JobHandle inputDeps)
        {
            // TODO: add image
            throw new NotImplementedException();
        }

        static readonly TextureFormat[] k_SupportedFormats =
        {
            TextureFormat.Alpha8,
            TextureFormat.R8,
            TextureFormat.R16,
            TextureFormat.RFloat,
            TextureFormat.RGB24,
            TextureFormat.RGBA32,
            TextureFormat.ARGB32,
            TextureFormat.BGRA32
        };

        public override int supportedTextureFormatCount => k_SupportedFormats.Length;

        protected override TextureFormat GetSupportedTextureFormatAtImpl(int index) => k_SupportedFormats[index];

        protected override XRReferenceImage GetReferenceImageAt(int index)
        {
            Assert.AreNotEqual(self, IntPtr.Zero);
            return GetReferenceImage(index);
        }

        public override int count => NativeApi.ImageTracking.ar_reference_images_get_count(self);

        XRReferenceImage GetReferenceImage(int index)
        {
            var referenceImagePtr = NativeApi.ImageTracking.UnityVisionOS_impl_get_reference_image_at_index(self, index);
            if (referenceImagePtr == IntPtr.Zero)
            {
                Debug.LogError($"Couldn't find AR reference image at index {index}");
                return default;
            }

            var imageNamePtr = NativeApi.ImageTracking.ar_reference_image_get_name(referenceImagePtr);
            if (imageNamePtr == IntPtr.Zero)
            {
                Debug.LogError($"Couldn't get name for AR reference image at index {index}");
                return default;
            }

            var referenceImageName = Marshal.PtrToStringAnsi(imageNamePtr);
            if (string.IsNullOrEmpty(referenceImageName))
            {
                Debug.LogError($"Couldn't get name for AR reference image at index {index}");
                return default;
            }

            if (!m_ReferenceImages.TryGetValue(referenceImageName, out var xrReferenceImage))
            {
                if (!m_NameToGuid.TryGetValue(referenceImageName, out var imageGuid))
                    Debug.LogError($"Could not find guid for AR reference image with name: {referenceImageName}");

                m_NameToTextureGuid.TryGetValue(referenceImageName, out var textureGuid);
                var width = NativeApi.ImageTracking.ar_reference_image_get_physical_width(referenceImagePtr);
                var height = NativeApi.ImageTracking.ar_reference_image_get_physical_height(referenceImagePtr);

                // TODO: Texture guid?
                xrReferenceImage = new XRReferenceImage(imageGuid, textureGuid, new Vector2(width, height), referenceImageName, null);
                m_ReferenceImages.Add(referenceImageName, xrReferenceImage);
            }

            return xrReferenceImage;
        }

        // TODO: Pointers are not equivalent?
        // public bool TryGetImageForPointer(IntPtr referenceImagePointer, out XRReferenceImage image)
        // {
        //     return m_ReferenceImages.TryGetValue(referenceImagePointer, out image);
        // }

        public bool TryGetImageForName(string referenceImageName, out XRReferenceImage image)
        {
            return m_ReferenceImages.TryGetValue(referenceImageName, out image);
        }

        //TODO: Reference ARFoundation InternalUtils?
        /// <summary>Given a <see cref="System.Guid"/>, resolves it into the equivalent SerializableGuid.</summary>
        /// <param name="guid">The guid to resolve.</param>
        /// <returns>A <see cref="UnityEngine.XR.ARSubsystems.SerializableGuid"/> containing the same data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SerializableGuid AsSerializedGuid(Guid guid)
        {
            Decompose(guid, out var low, out var high);
            return new SerializableGuid(low, high);
        }

        // From core-utils
        /// <summary>
        /// Decomposes a 16-byte <c>Guid</c> into two 8-byte <c>ulong</c>s.
        /// Recompose with <see cref="GuidUtil.Compose(ulong, ulong)"/>.
        /// </summary>
        /// <param name="guid">The <c>Guid</c> being extended</param>
        /// <param name="low">The lower 8 bytes of the guid.</param>
        /// <param name="high">The upper 8 bytes of the guid.</param>
        static void Decompose(Guid guid, out ulong low, out ulong high)
        {
            var bytes = guid.ToByteArray();
            low = BitConverter.ToUInt64(bytes, 0);
            high = BitConverter.ToUInt64(bytes, 8);
        }

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "CFBundleGetMainBundle")]
        static extern IntPtr GetMainBundle();

        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_impl_CreateFromCompiledAssetCatalog")]
        static extern IntPtr CreateNSBundleFromCompiledAssetCatalog(string bundleIdentifier, NativeView car);

        // ReSharper restore InconsistentNaming
    }
}
