using System;
using System.IO;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// Class used to move Font assets from the project to the built player.
    /// This way we can make use of the font assets at runtime without
    /// worrying about how to read or find the actual font file data.
    /// </summary>
    class PolySpatialFontImporter : IPolySpatialAssetImporter
    {
        public bool IsInterestedInAsset(string path)
        {
            return path.EndsWith("ttf", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith("otf", StringComparison.OrdinalIgnoreCase);
        }

        public void OnImportAsset(AssetImportContext ctx)
        {
            string name = "";
            Font font = null;
            FileInfo fi = null;

            font = AssetDatabase.LoadAssetAtPath<Font>(ctx.assetPath);
            if (font != null)
            {
                name = font.name;
                fi = new FileInfo(ctx.assetPath);
            }

            // If this is not a font then we don't care about it.
            if (font == null || fi == null)
                return;

            var destPath = ctx.GetOutputArtifactFilePath($"{name}{PolySpatialAssetData.PolySpatialAssetExtension}");
            try
            {
                // Transfer the font to the PSL Asset Data storage location for
                // us to use at runtime.
                using (var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var sr = new BinaryReader(fs))
                    {
                        var bytes = sr.ReadBytes((int)fs.Length);
                        File.WriteAllBytes(destPath, bytes);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
    }
}
