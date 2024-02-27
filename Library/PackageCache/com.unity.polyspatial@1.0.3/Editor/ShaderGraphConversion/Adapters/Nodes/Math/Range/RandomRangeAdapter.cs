
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RandomRangeAdapter : ANodeAdapter<RandomRangeNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("Min", "bg");
            portMap.Add("Max", "fg");
            var outputNode = QuickNode.NaryOp(MtlxNodeTypes.Mix, node, graph, externals, "RandomRange", portMap);

            // for some bizarre reason, noise2d in mtlx does not accept inlined values.
            var seedSlot = NodeUtils.GetSlotByName(node, "Seed");
            var seedNode = graph.AddNode(NodeUtils.GetNodeName(node, "RandomRangeSeed"), MtlxNodeTypes.Constant, MtlxDataTypes.Vector2);
            seedNode.AddPortValue("value", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(seedSlot));
            externals.AddExternalPortAndEdge(seedSlot, seedNode.name, "value");

            var noiseNode = graph.AddNode(NodeUtils.GetNodeName(node, "RandomRangeNoise"), MtlxNodeTypes.PerlinNoise2d, MtlxDataTypes.Float);
            graph.AddPortAndEdge(seedNode.name, noiseNode.name, "texcoord", MtlxDataTypes.Vector2);

            graph.AddPortAndEdge(noiseNode.name, outputNode.name, "mix", MtlxDataTypes.Float);
        }
    }
}
