using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// The AssetManifestBuildPostprocessor generates an AssetManifest.json file, which contains information about how
    /// PolySpatialAssetIDS correspond to built assets and resources. It does so by registering a custom BuildPlayerHandler
    /// which traverses the scenes to be built, finds all asset references and adds them to the manifest, then hands off
    /// the rest of build execution to DefaultBuildMethods.BuildPlayer()
    /// </summary>
    internal class AssetManifestBuildProcessor : BuildPlayerProcessor
    {
        /// <inheritdoc/>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            BuildAssetManifest(buildPlayerContext);
        }

        private static void BuildAssetManifest(BuildPlayerContext buildPlayerContext)
        {
        }
    }
}
