using UnityEditor;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    // Imports normal maps as linear RGB textures to avoid the postprocessing (converting XYZ1 to 1YYX),
    // which RealityKit does not support.
    class NormalMapPostprocessor : AssetPostprocessor
    {
        bool m_WasNormalMap;

        void OnPreprocessTexture()
        {
            var textureImporter = (TextureImporter)assetImporter;
            if (textureImporter.textureType == TextureImporterType.NormalMap)
            {
                m_WasNormalMap = true;
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.sRGBTexture = false;
            }
            else
            {
                m_WasNormalMap = false;
            }
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            if (m_WasNormalMap)
            {
                var textureImporter = (TextureImporter)assetImporter;
                textureImporter.textureType = TextureImporterType.NormalMap;

                // Don't reserialize the importer, since the change was only temporary.
                EditorUtility.ClearDirty(textureImporter);
            }
        }
    }
}