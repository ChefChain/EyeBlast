
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalFromHeightAdapter : ANodeAdapter<NormalFromHeightNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("In", "in");
            portMap.Add("Strength", "scale");
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.HeightToNormal, node, graph, externals, "NormalFromHeight", portMap);

            // Okay- our node transforms the output to a particular space.
            if (node is NormalFromHeightNode hnode && hnode.outputSpace == OutputSpace.Tangent)
            {
                // Transform into tangent space
                var nodeName = NodeUtils.GetNodeName(node, $"NormalFromHeightTangent");
                var transformNode = graph.AddNode(nodeName, MtlxNodeTypes.TransformNormal, MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(nodeData.name, transformNode.name, "in", MtlxDataTypes.Vector3);
                transformNode.AddPortString("fromspace", MtlxDataTypes.String, "world");
                transformNode.AddPortString("tospace", MtlxDataTypes.String, "tangent");

                // clobber the output slot bindings w/our tangent space output
                var outputSlot = NodeUtils.GetPrimaryOutput(node);
                externals.slotToPort[outputSlot.slotReference] = new ExternalEdgeMap.PortReference { node = nodeName, port = "" };
            }
        }
    }
}
