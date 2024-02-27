using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using Object = UnityEngine.Object;

namespace Unity.PolySpatial.Internals
{
    internal class EditorPolySpatialAssetProvider : IPolySpatialAssetProvider
    {
        public static string[] GetPathsForAsset(string guid)
        {
            var artifactKey = new ArtifactKey(new GUID(guid), typeof(PolySpatialAssetImporter));
            var artifactID = AssetDatabaseExperimental.LookupArtifact(artifactKey);
            if (!artifactID.isValid)
                return Array.Empty<string>();
            AssetDatabaseExperimental.GetArtifactPaths(artifactID, out var paths);
            return paths.Where(p => p.EndsWith(PolySpatialAssetData.PolySpatialAssetExtension)).ToArray();
        }

        public string[] GetPathsForAsset(Object asset)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
                return Array.Empty<string>();
            return GetPathsForAsset(guid);
        }
    }
}
