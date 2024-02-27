using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace Unity.PolySpatial.Internals
{
    // Scripted importer used to generate any PolySpatialAssetData to be supplied in addition to assets.
    // This importer is not keyed to a specific extension - because we want to produce additional artifacts
    // for assets imported by other importers, but the AssetDatabase does not allow multiple importes to
    // be mapped to an extension. Instead, PolySpatialAssetImporter is invoked by
    // PolySpatialAssetPostprocessor.OnPostprocessAllAssets, which will call
    // AssetDatabaseExperimental.ForceProduceArtifact to run this importer.
    //
    // PolySpatialAssetImporter itself will not do any work, but rather find all classes implementing
    // IPolySpatialAssetImporter, and ask them if they care about the asset - and if yes, let them handle it.
    //
    // The reason for this (instead of having multiple ScriptedImporters) is that this way, we have only
    // one ArtifactKey (combination of Asset GUID and importer type) to query when looking up 
    // PolySpatialAssetData in EditorPolySpatialAssetProvider.
    [ScriptedImporter(1, "no_extension_is_needed", 0)]
    class PolySpatialAssetImporter : ScriptedImporter
    {
        private static IPolySpatialAssetImporter[] s_PolySpatialAssetImporters;

        static IPolySpatialAssetImporter[] polyspatialAssetImporters => s_PolySpatialAssetImporters ??= 
                TypeCache.GetTypesDerivedFrom<IPolySpatialAssetImporter>()
                .Select(t => (IPolySpatialAssetImporter)Activator.CreateInstance(t))
                .ToArray();

        public static bool IsInterestedInAsset(string path) => polyspatialAssetImporters.Any(i => i.IsInterestedInAsset(path));

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Note: This is typically triggered from another thread, and the C# debugger does not reliably detect falls
            // into this function. Use debug printing instead
            foreach (var importer in polyspatialAssetImporters.Where(i => i.IsInterestedInAsset(ctx.assetPath)))
            {
                importer.OnImportAsset(ctx);
            }
        }
    }
}
