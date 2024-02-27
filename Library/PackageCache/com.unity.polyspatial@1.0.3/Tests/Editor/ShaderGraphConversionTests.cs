using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;

using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX.Tests
{
    [TestFixture]
    public class ShaderGraphConversionTests
    {
        private const string k_TestAssetsDirectory = "Packages/com.unity.polyspatial/Tests/Data/ShaderGraph/SourceAssets";
        private const string k_TestExpectedOutputDirectory = "Packages/com.unity.polyspatial/Tests/Data/ShaderGraph/ExpectedOutput";

        void ExpectGraphConverts(string assetPath)
        {
            var mtlxGraph = MtlxPostProcessor.ProcessFile(assetPath, "", out _);
            var relativePath = assetPath.Substring(k_TestAssetsDirectory.Length);
            var dstPathNoExt = $"{Path.GetDirectoryName(relativePath)}/{Path.GetFileNameWithoutExtension(assetPath)}";

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IGraphProcessor)))
            {
                var nodeProcessor = (IGraphProcessor)Activator.CreateInstance(type);
                if (!nodeProcessor.IsEnabled())
                    continue;

                var actualContents = nodeProcessor.ProcessGraph(mtlxGraph)
                    .Replace("\r\n", "\n")
                    .Replace("\\", "/");

                using var data = new NativeList<byte>(Allocator.Temp);

                var expectedFilepath = $"{k_TestExpectedOutputDirectory}{dstPathNoExt}.{nodeProcessor.FileExtension}";
                                
                if (!File.Exists(expectedFilepath))
                {
                    Debug.LogError($"Missing baseline: {expectedFilepath}");
                }
                else
                {
                    // Compare the generated data against the baseline intermediate asset
                    var intermediateBaseline = File.ReadAllText(expectedFilepath, Encoding.UTF8)
                        .Replace("\r\n", "\n")
                        .Replace("\\", "/");
                    Assert.AreEqual(actualContents, intermediateBaseline, $"Asset Path: {assetPath} Baseline Path: {expectedFilepath}");
                }

                // Compare the generated data against the asset stored in the database
                var readResult = PolySpatialAssetData.ReadDataForAsset(AssetDatabase.LoadAssetAtPath<Shader>(assetPath),
                    nodeProcessor.FileExtension, data);
                Assert.IsTrue(readResult);
                var assetContents = Encoding.UTF8.GetString(data.AsArray())
                    .Replace("\r\n", "\n")
                    .Replace("\\", "/");
                Assert.AreEqual(actualContents, assetContents, $"For asset with path: {assetPath}");
            }
        }

        /// <summary>
        /// This editor time test will inline compare the output of the shadergraph assets in k_TestAssetsDirectory
        /// with the expected output in k_TestExpectedDirectory. It will catch if the expected output has changed.
        ///
        /// This test is relatively fragile- as the assets and expected outputs need to be updated individually.
        /// To do so:
        /// * Enable all PolySpatial/ShaderGraph project settings.
        /// * Copy the TestAssets subdirectory into a project-- which should cause them to import and generate output files.
        /// * -- Should fix any import errors if there are any and reimport.
        /// * Copy from ProjectDir/Assets/StreamingAssets/ to the expected test directory.
        /// * Clean up any assets not needed in that project.
        /// </summary>
        [Test]
        public void TestAllAssets()
        {
            string[] testAssets = Directory.GetFiles(k_TestAssetsDirectory, "*.shadergraph", SearchOption.AllDirectories);

            foreach(var path in testAssets)
            {
                ExpectGraphConverts(path);
            }
        }
    }
}
