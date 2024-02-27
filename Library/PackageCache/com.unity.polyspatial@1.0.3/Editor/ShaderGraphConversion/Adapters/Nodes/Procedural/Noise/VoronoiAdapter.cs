
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class VoronoiAdapter : AbstractUVNodeAdapter<VoronoiNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            var slot = NodeUtils.GetOutputByName(node, "Cells");
            return slot.isConnected ? "Cells output not supported for MaterialX conversion." : "";
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("AngleOffset", "jitter");
            var outputNode = QuickNode.NaryOp(MtlxNodeTypes.WorleyNoise2d, node, graph, externals, "Voronoi", portMap);

            // portMap.Add("CellDensity", "amplitude");
            var cellSlot = NodeUtils.GetSlotByName(node, "CellDensity");
            var uvSlot = (UVMaterialSlot)NodeUtils.GetSlotByName(node, "UV");

            var scaleNode = graph.AddNode(NodeUtils.GetNodeName(node, "CellDensity"), MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2);
            scaleNode.AddPort("in1", MtlxDataTypes.Vector2);
            scaleNode.AddPortValue("in2", MtlxDataTypes.Float, SlotUtils.GetDefaultValue(cellSlot));

            externals.AddExternalPortAndEdge(uvSlot, scaleNode.name, "in1");
            externals.AddExternalPortAndEdge(cellSlot, scaleNode.name, "in2");

            graph.AddPortAndEdge(scaleNode.name, outputNode.name, "texcoord", MtlxDataTypes.Vector2);

            if (!uvSlot.isConnected)
            {
                var uvNode = QuickNode.CreateUVNode(
                    graph, NodeUtils.GetNodeName(node, "GradientNoiseUV"), (int)uvSlot.channel);
                graph.AddEdge(uvNode.name, scaleNode.name, "in1");
            }
        }
    }
}
