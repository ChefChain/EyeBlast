using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tests.Editor;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

[assembly: TestPlayerBuildModifier(typeof(AddTestScenesToBuild))]
namespace Tests.Editor
{
    /// <summary>
    /// Test Scenes may not be included in build settings by default; this class adds all test Scenes
    /// under the default Test Scene directory at runtime for standalone player tests.
    /// Note: it only adds those tests Scenes which have not already been added to the build settings;
    /// if a test Scene is already included, it will not be re-added.
    /// </summary>
    public class AddTestScenesToBuild : ITestPlayerBuildModifier
    {
        private const string k_TestSceneDirectory = "Assets/Tests/Scenes";
        // TODO: replace with fixed path to Sample Scenes directory once fully supported
        private readonly string[] k_SampleScenes =
        {
            "Assets/Scenes/CubeSpawner.unity",
            "Assets/Scenes/VerticalSliceDemo.unity"
        };

        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            var buildSceneList = new HashSet<string>(playerOptions.scenes);
            buildSceneList.UnionWith(k_SampleScenes);
            buildSceneList.UnionWith(AssetDatabase.FindAssets(
                    "t:SceneAsset",
                    new[] {k_TestSceneDirectory})
                .Select(AssetDatabase.GUIDToAssetPath));

            playerOptions.scenes = buildSceneList.ToArray();

            return playerOptions;
        }
    }
}
