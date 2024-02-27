using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MtlxPostProcessor : IPolySpatialAssetImporter
    {
        // Note: This is typically triggered from another thread, and the C# debugger does not reliably detect falls
        // into this function. Use debug printing instead

        public void OnImportAsset(AssetImportContext context)
        {
            var mtlxGraph = ProcessFile(context.assetPath, "Library/ShaderGraphs", out var destinationPath);

            // process each graph for each type of IGraphProcessor discovered, TODO: fix this callsite.
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IGraphProcessor)))
            {
                var nodeProcessor = (IGraphProcessor)Activator.CreateInstance(type);
                if (!nodeProcessor.IsEnabled())
                    continue;

                var fileContents = nodeProcessor.ProcessGraph(mtlxGraph);

                File.WriteAllText(
                    context.GetOutputArtifactFilePath($"{nodeProcessor.FileExtension}.polyspatial"),
                    fileContents);

                // Optionally dump the generated data to an intermediate file, which can aid in debugging shadergraph
                // issues or serve as starting point for a hand-crafted materialX file
                if (nodeProcessor.GenerateIntermediateFile())
                {
                    FileInfo file = new($"{destinationPath}.{nodeProcessor.FileExtension}");
                    file.Directory.Create();
                    File.WriteAllText(file.FullName, fileContents);
                }
            }
        }

        internal static GraphData LoadGraphData(string path)
        {
            var fileContents = File.ReadAllText(path, Encoding.UTF8);
            var assetGuid = AssetDatabase.AssetPathToGUID(path);
            var isSubGraph = AssetDatabase.LoadAssetAtPath<ShaderSubGraphMetadata>(path) != null;

            var graph = new GraphData
            {
                assetGuid = assetGuid,
                isSubGraph = isSubGraph,
                messageManager = null
            };
            MultiJson.Deserialize(graph, fileContents);
            graph.OnEnable();
            graph.ValidateGraph();
            return graph;
        }
        static bool IsAssetShaderGraph(string path)
        {
            var ext = Path.GetExtension(path).TrimStart('.');
            return ext == ShaderGraphImporter.Extension && !Directory.Exists(path);
        }

        internal static MtlxGraphData ProcessFile(string assetPath, string dstRoot, out string filePathNoExtension)
        {
            var graphData = LoadGraphData(assetPath);
            var filename = Path.GetFileNameWithoutExtension(assetPath);
            var mtlxGraph = ProcessGraph(graphData, filename);

            filePathNoExtension = $"{dstRoot}/{Path.GetDirectoryName(assetPath)}/{filename}";
            return mtlxGraph;
        }

        internal static MtlxGraphData ProcessGraph(GraphData graphData, string filename, bool skipContext = false)
        {
            ExternalEdgeMap externals = new();
            MtlxGraphData graph = new(filename, graphData.path);

            foreach (var node in graphData.GetNodes<AbstractMaterialNode>())
            {
                if (AdapterMap.IsNodeSupported(node))
                {
                    AdapterMap.ProcessInstance(node, graph, externals);
                }
            }
            if (!skipContext)
                AdapterMap.ProcessContext(graphData, graph, externals);

            externals.ResolveExternals(graph);
            return graph;
        }

        internal static void LogWarningForGraph(GraphData graphData, string message)
        {
            if (!ShouldSuppressWarningsForGraph(graphData))
                Debug.LogWarning(message);
        }

        static bool ShouldSuppressWarningsForGraph(GraphData graphData)
        {
            // Only suppress warnings for assets in immutable packages and their associated imports.
            var assetPath = AssetDatabase.GUIDToAssetPath(graphData.assetGuid);
            var isUnityImport = assetPath.StartsWith("Assets/Samples/Universal RP/") ||
                assetPath.StartsWith("Assets/TextMesh Pro/");
            if (!(Path.GetFullPath(assetPath).Contains("PackageCache") || isUnityImport))
                return false;
                
            var showWarningsForShaderGraphsInPackages = true;
            var polySpatialSettings = PolySpatialSettings.GetInstanceIfExists();
            if (polySpatialSettings != null)
                showWarningsForShaderGraphsInPackages = polySpatialSettings.ShowWarningsForShaderGraphsInPackages;

            // Suppress all warnings if specifically disabled.
            if (!showWarningsForShaderGraphsInPackages)
                return true;

            // Otherwise, suppress Unity package warnings unless it's an internal build.
#if POLYSPATIAL_INTERNAL
            return false;
#else
            // Suppress warnings for non-PolySpatial Unity shader graphs (such as those in the URP package).
            return assetPath.StartsWith("Packages/com.unity.") &&
                !assetPath.StartsWith("Packages/com.unity.polyspatial") || isUnityImport;
#endif
        }

        class MtlxNodeValidation : INodeValidationExtension
        {
            public INodeValidationExtension.Status GetValidationStatus(AbstractMaterialNode node, out string msg)
            {
                // Clear any previous error messages, as we will generate them again if still relevant.
                node.owner.messageManager.ClearNodesFromProvider(this, Enumerable.Repeat(node, 1));

                var status = INodeValidationExtension.Status.None;

                if (node is BlockNode bnode && !MaterialX.AdapterMap.IsBlockSupported(bnode))
                {
                    msg = $"Block Node '{bnode.descriptor.name}' are not supported for MaterialX conversion.";
                    status = INodeValidationExtension.Status.Warning;
                }
                else if (node is not BlockNode && !MaterialX.AdapterMap.IsNodeSupported(node))
                {
                    msg = $"Nodes of type '{node.GetType()}' are not supported for MaterialX conversion.";
                    status = INodeValidationExtension.Status.Warning;
                }
#if DISABLE_MATERIALX_EXTENSIONS
                else if (node is BlockNode vnode
                    && vnode.descriptor.shaderStage == ShaderStage.Vertex)
                {
                    msg = $"Vertex stage outputs are not supported with DISABLE_MATERIALX_EXTENSIONS defined";
                    status = INodeValidationExtension.Status.Warning;
                }
#endif
                else
                {
                    msg = AdapterMap.GetSupportNotification(node);
                    status = (msg == "") ? INodeValidationExtension.Status.None : INodeValidationExtension.Status.Warning;
                }

                // ShaderGraphImporter only reports one warning; we want to show all unique warnings.
                if (status == INodeValidationExtension.Status.Warning)
                {
                    // Ignore/clear any warnings upstream from the "Off" input of a MaterialX keyword node.
                    bool AffectsMaterialXOutput(AbstractMaterialNode node)
                    {
                        List<MaterialSlot> outputs = new();
                        node.GetOutputSlots(outputs);
                        foreach (var outputSlot in outputs)
                        {
                            foreach (var edge in outputSlot.owner.owner.GetEdges(outputSlot.slotReference))
                            {
                                var inputSlot = edge.inputSlot.slot;
                                if (!(inputSlot.owner is KeywordNode keywordNode &&
                                    keywordNode.keyword.referenceName == KeywordAdapter.k_MaterialXKeywordReferenceName &&
                                    inputSlot.RawDisplayName() == "Off") &&
                                    (inputSlot.owner is BlockNode || inputSlot.owner is SubGraphOutputNode ||
                                        AffectsMaterialXOutput(inputSlot.owner)))
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    if (ShouldSuppressWarningsForGraph(node.owner) || !AffectsMaterialXOutput(node))
                    {
                        msg = "";
                        return INodeValidationExtension.Status.None;
                    }

                    var previousWarnings = node.owner.messageManager.ErrorStrings(
                        _ => true, Rendering.ShaderCompilerMessageSeverity.Warning);

                    // The first warning will be reported by the validator.
                    if (previousWarnings.Any() && !previousWarnings.Contains(msg))
                        Debug.LogWarning(msg);
                }

                return status;
            }

            public string GetValidatorKey() => "MaterialXValidator";
        }

        public bool IsInterestedInAsset(string path) => IsAssetShaderGraph(path);
    }
}
