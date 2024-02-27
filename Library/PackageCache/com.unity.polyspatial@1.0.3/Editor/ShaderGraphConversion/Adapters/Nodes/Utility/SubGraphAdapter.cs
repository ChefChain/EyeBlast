using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph.MaterialX
{

    /*
    This is the most complex adapter, it's worth outlining what is going on.

    1. Convert the subgraph to an MtlxGraphData.
    2. Create a unique ScopeName from the SubGraphNode: it will be prepended to all node names in the mtlxSubGraph.
    3. Remap the SubGraphNode's input/output ports to corresponding MtlxSubGraph nodes & ports (presuming that they will be renamed w/the ScopeName).
    4. Rename all of the MtlxSubGraph nodes/edges w/the ScopeName.
    5. Merge the graphs together.
    */
    internal class SubGraphAdapter : ANodeAdapter<SubGraphNode>
    {
        static readonly Regex s_NonIdentifierChars = new("[^a-zA-Z_0-9]");

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is SubGraphNode sgNode && sgNode.asset != null)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(sgNode.asset.assetGuid);
                var graphData = MtlxPostProcessor.LoadGraphData(assetPath);
                var filename = Path.GetFileNameWithoutExtension(assetPath);

                // 2. Setup the scope name.
                string subGraphScope = $"subgraph_{s_NonIdentifierChars.Replace(filename, "")}_{node.objectId}";

                ExternalEdgeMap subExternals = new();
                MtlxGraphData subGraph = new(filename, graphData.path);
                SubGraphContext subContext = new(sgNode, sgContext);

                // 1. build the endogenous subgraph and external edge map.
                foreach (var n in graphData.GetNodes<AbstractMaterialNode>())
                {
                    if (AdapterMap.IsNodeSupported(n))
                    {
                        AdapterMap.ProcessInstance(n, subGraph, subExternals, subContext);
                    }
                }
                subExternals.ResolveExternals(subGraph);

                // 3a. remap the sgNode's input connections/value to the corresponding property nodes.
                // (add the external ports assuming the names will be updated).
                foreach (var p in graphData.GetNodes<PropertyNode>())
                {
                    var pSlot = NodeUtils.GetPrimaryOutput(p);
                    var sgInputSlot = NodeUtils.GetInputByName(sgNode, pSlot.RawDisplayName());

                    if (sgInputSlot.isConnected)
                    {
                        var srcSlot = SlotUtils.GetSourceConnectionSlot(sgInputSlot);
                        externals.AddExternalEdge(srcSlot.slotReference, sgInputSlot.slotReference);
                        externals.AddExternalPort(sgInputSlot.slotReference, $"{subGraphScope}{p.property.referenceName}", "value");
                    }
                    else if (sgInputSlot is Texture2DInputMaterialSlot)
                    {
                        // Unconnected texture slots correspond to implicit properties.
                        if (!subGraph.HasConnection(p.property.referenceName, "value"))
                        {
                            var variableName = node.GetVariableNameForSlot(sgInputSlot.id);
                            QuickNode.EnsureImplicitProperty(variableName, MtlxDataTypes.Filename, subGraph);
                            subGraph.AddEdge(variableName, p.property.referenceName, "value");
                        }
                    }
                    else
                    {
                        // The default values from the propertyNodes are no good, we need to use the subgraph input port values.
                        subGraph.GetNode(p.property.referenceName).GetPort("value").SetValue(SlotUtils.GetDefaultValue(sgInputSlot));
                    }

                    if (sgInputSlot is Texture2DInputMaterialSlot)
                    {
                        // Special handling for texture size properties (which cannot be handled via the externals map,
                        // since we already have mappings for the texture value).  If we have an inner texture size
                        // node, ensure that we also have an outer size node and connect its output to the inner one.
                        var innerSizeNodeName = TextureSizeAdapter.GetTextureSizeNodeName(p.property.referenceName);
                        if (subGraph.HasNode(innerSizeNodeName))
                        {
                            var scopedInnerSizeNodeName = $"{subGraphScope}{innerSizeNodeName}";
                            if (!graph.HasConnection(scopedInnerSizeNodeName, "value"))
                            {
                                var outerSizeNodeName = TextureSizeAdapter.EnsureTextureSizeProperty(
                                    sgInputSlot, graph);
                                graph.AddEdge(outerSizeNodeName, scopedInnerSizeNodeName, "value");
                            }
                        }
                    }
                }

                // 3b. remap the sgNode's output port references to the corresponding endogenous nodes.
                foreach (var o in graphData.GetNodes<SubGraphOutputNode>())
                {
                    var inputs = new List<MaterialSlot>();
                    o.GetInputSlots(inputs);
                    foreach (var input in inputs)
                    {
                        if (!input.isConnected)
                            continue;

                        var srcSlot = SlotUtils.GetSourceConnectionSlot(input);
                        if (!AdapterMap.IsNodeSupported(srcSlot.owner))
                            continue;

                        // mtlx name for the endogenous upstream src node
                        if (!subExternals.slotToPort.TryGetValue(srcSlot.slotReference, out var portRef))
                        {
                            // If there's no slotToPort mapping, then something went wrong with node conversion
                            // (for example, failure to parse a custom function) and a warning will have been
                            // logged if appropriate.  Omitting the external port mapping is fine and will result
                            // in anything referencing that mapping using its default value (refer to the
                            // implementation of ExternalEdgeMap.ResolveExternals).
                            continue;
                        }

                        // output node in the main graph- TODO: RawDisplayName should be wrapped by something.
                        var sgOutputSlot = NodeUtils.GetOutputByName(sgNode, input.RawDisplayName());

                        externals.AddExternalPort(sgOutputSlot.slotReference, $"{subGraphScope}{portRef.node}");
                    }
                }
                // 4 and 5--
                // TODO: it's sloppy to have these operations on the interface for MtlxGraphData when they are uniquely relevant for SubGraphs.
                subGraph.ReScope(subGraphScope);
                graph.Merge(subGraph);
            }
        }
    }
}
