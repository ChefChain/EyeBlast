using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class TilingAndOffsetAdapter : AbstractUVNodeAdapter<TilingAndOffsetNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Out = UV * Tiling + Offset;
            var uvSlot = (UVMaterialSlot)NodeUtils.GetSlotByName(node, "UV");
            var tilingSlot = NodeUtils.GetSlotByName(node, "Tiling");
            var offsetSlot = NodeUtils.GetSlotByName(node, "Offset");
            var outputSlot = NodeUtils.GetPrimaryOutput(node);

            // tiling operation
            var mulNode = graph.AddNode(NodeUtils.GetNodeName(node, "TilingAndOffsetMul"), MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2);

            // uv input
            QuickNode.HandleUVSlot(uvSlot, NodeUtils.GetNodeName(node, "TilingAndOffsetUV"), mulNode.name, "in1", graph, externals);

            // tiling input
            mulNode.AddPortValue("in2", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(tilingSlot));
            externals.AddExternalPortAndEdge(tilingSlot, mulNode.name, "in2");

            // offset operation
            var addNode = graph.AddNode(NodeUtils.GetNodeName(node, "TilingAndOffsetAdd"), MtlxNodeTypes.Add, MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(mulNode.name, addNode.name, "in1", MtlxDataTypes.Vector2);

            // offset input
            addNode.AddPortValue("in2", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(offsetSlot));
            externals.AddExternalPortAndEdge(offsetSlot, addNode.name, "in2");

            externals.AddExternalPort(outputSlot.slotReference, addNode.name);
        }
    }
}
