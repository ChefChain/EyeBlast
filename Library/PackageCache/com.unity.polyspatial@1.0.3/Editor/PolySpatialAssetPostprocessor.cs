using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.PolySpatial.Internals
{
    internal class PolySpatialAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // If our script assembly has been removed, we're restarting and will postprocess later.
            if (!File.Exists("Library/ScriptAssemblies/Unity.PolySpatial.Editor.dll"))
                return;

            foreach (var assetPath in importedAssets)
            {
                if (PolySpatialAssetImporter.IsInterestedInAsset(assetPath))
                {
                    try
                    {
                        AssetDatabaseExperimental.ForceProduceArtifact(
                            new ArtifactKey(
                                new GUID(AssetDatabase.AssetPathToGUID(assetPath)),
                                typeof(PolySpatialAssetImporter)
                            ));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Encountered error processing {assetPath}: {e}");
                    }
                }
            }
        }
    }
}