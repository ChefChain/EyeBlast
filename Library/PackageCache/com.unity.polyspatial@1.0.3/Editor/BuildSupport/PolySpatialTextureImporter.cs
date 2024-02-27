using System;
using System.IO;
using Unity.Collections;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.PolySpatial.Internals
{
    // This class can produce additional polyspatial data for textures as non-primary artifacts, which PolySpatial will be able to
    // access using PolySpatialAssetData. This lets us make sure that we include readable texture data in the correct compression
    // format for the target device(s).
    class PolySpatialTextureImporter : IPolySpatialAssetImporter
    {
        (TextureImporterFormat importerFormat, TextureFormat format) GetTextureFormatForCompression(Texture2D t, PolySpatialSettings.PolySpatialTextureCompressionFormat compressionFormat, bool hasAlpha)
        {
            bool supportsCompression = t.width % 4 == 0 && t.height % 4 == 0;
            if (!supportsCompression)
                return hasAlpha
                    ? (TextureImporterFormat.RGBA32, TextureFormat.RGBA32)
                    : (TextureImporterFormat.RGB24, TextureFormat.RGB24);
            switch (compressionFormat)
            {
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.Unknown:
                    return hasAlpha
                        ? (TextureImporterFormat.RGBA32, TextureFormat.RGBA32)
                        : (TextureImporterFormat.RGB24, TextureFormat.RGB24);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.ETC:
                    return hasAlpha
                        ? (TextureImporterFormat.RGBA32, TextureFormat.RGBA32)
                        : (TextureImporterFormat.ETC_RGB4, TextureFormat.ETC_RGB4);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.ETC2:
                    return hasAlpha
                        ? (TextureImporterFormat.ETC2_RGBA8, TextureFormat.ETC2_RGBA8)
                        : (TextureImporterFormat.ETC2_RGB4, TextureFormat.ETC2_RGB);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.DXTC:
                    return hasAlpha
                        ? (TextureImporterFormat.DXT5, TextureFormat.DXT5)
                        : (TextureImporterFormat.DXT1, TextureFormat.DXT1);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.BPTC:
                    return hasAlpha
                        ? (TextureImporterFormat.BC7, TextureFormat.BC7)
                        : (TextureImporterFormat.BC6H, TextureFormat.BC6H);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.ASTC:
                    return (TextureImporterFormat.ASTC_6x6, TextureFormat.ASTC_6x6);
                case PolySpatialSettings.PolySpatialTextureCompressionFormat.PVRTC:
                    return hasAlpha
                        ? (TextureImporterFormat.PVRTC_RGBA4, TextureFormat.PVRTC_RGBA4)
                        : (TextureImporterFormat.PVRTC_RGB4, TextureFormat.PVRTC_RGB4);
                default:
                    throw new NotImplementedException($"PolySpatial does not support {compressionFormat}");
            }
        }

        // Note: This is typically triggered from another thread, and the C# debugger does not reliably detect falls
        // into this function. Use debug printing instead

        public void OnImportAsset(AssetImportContext context)
        {
            var formats = PolySpatialSettings.AdditionalTextureFormats;
            if (formats == null)
                return;

            var importer = AssetImporter.GetAtPath(context.assetPath) as TextureImporter;
            if (importer == null)
                return;

            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(context.assetPath);
            
            // If this is not a Texture2D (e.g. an .exr file), we have nothing to do here.
            if (t == null)
                return;
            
            var textureCompression = importer.textureCompression;

            if (textureCompression != TextureImporterCompression.Uncompressed)
            {
                t.SetAllowReadingInEditor(true);
                using NativeArray<Color32> colorBuffer = new(t.GetPixels32(), Allocator.Temp);
                t.SetAllowReadingInEditor(false);
                foreach (var format in formats)
                {
                    var requestedFormat = GetTextureFormatForCompression(t, format, importer.DoesSourceTextureHaveAlpha());
                    if (t.format == requestedFormat.format)
                    {
                        // If the texture produced by the Unity Texture importer is already in the desired format, then
                        // no need to generate it again, just write that.
                        File.WriteAllBytes(
                            context.GetOutputArtifactFilePath($"{t.graphicsFormat}{PolySpatialAssetData.PolySpatialAssetExtension}"),
                            t.GetRawTextureData());
                        continue;
                    }

                    var settings = new TextureGenerationSettings(importer.textureType);
                    importer.ReadTextureSettings(settings.textureImporterSettings);
                    settings.platformSettings = importer.GetDefaultPlatformTextureSettings();
                    settings.platformSettings.overridden = true;
                    settings.platformSettings.format = requestedFormat.importerFormat;
#if UNITY_2023_2_OR_NEWER                    
                    settings.platformSettings.ignorePlatformSupport = true;
#endif                    

                    settings.sourceTextureInformation = new()
                    {
                        width = t.width,
                        height = t.height,
                        containsAlpha = importer.DoesSourceTextureHaveAlpha(),
                    };
                    var output = TextureGenerator.GenerateTexture(settings, colorBuffer);

                    foreach (var warning in output.importWarnings)
                        Debug.LogWarning(warning);
                    if (!string.IsNullOrEmpty(output.importInspectorWarnings))
                        Debug.LogWarning(output.importInspectorWarnings);

                    File.WriteAllBytes(
                        context.GetOutputArtifactFilePath(
                            $"{output.texture.graphicsFormat}{PolySpatialAssetData.PolySpatialAssetExtension}"),
                        output.texture.GetRawTextureData());
                }
            }
            else
            {
                if (formats.Length != 0)
                {
                    // If no compression is desired, but we do generally want polyspatial asset data for textures,
                    // write the uncompressed texture data to a polyspatial asset.
                    File.WriteAllBytes(
                        context.GetOutputArtifactFilePath(
                            $"{t.graphicsFormat}{PolySpatialAssetData.PolySpatialAssetExtension}"),
                        t.GetRawTextureData());
                }
            }
        }

        public bool IsInterestedInAsset(string path) =>
            PolySpatialSettings.AdditionalTextureFormats?.Length > 0 && AssetImporter.GetAtPath(path) is TextureImporter;
    }
}
