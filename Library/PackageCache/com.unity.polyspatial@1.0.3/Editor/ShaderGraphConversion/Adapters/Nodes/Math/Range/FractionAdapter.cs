
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class FractionAdapter : ANodeAdapter<FractionNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // fract => x - floor(x)
            var nodeName = NodeUtils.GetNodeName(node, "Fraction");
            var nodeType = NodeUtils.GetDataTypeName(node);
            var outputSlot = NodeUtils.GetPrimaryOutput(node);
            var inputSlot = NodeUtils.GetSlotByName(node, "In");

            var dot = graph.AddNode($"{nodeName}Dot", MtlxNodeTypes.Dot, nodeType);
            var floor = graph.AddNode($"{nodeName}Floor", MtlxNodeTypes.Floor, nodeType);
            var nodeData = graph.AddNode(nodeName, MtlxNodeTypes.Subtract, nodeType);

            dot.AddPortValue("in", nodeType, SlotUtils.GetDefaultValue(inputSlot));

            graph.AddPortAndEdge(dot.name, floor.name, "in", nodeType);
            graph.AddPortAndEdge(dot.name, nodeData.name, "in1", nodeType);
            graph.AddPortAndEdge(floor.name, nodeData.name, "in2", nodeType);

            externals.AddExternalPortAndEdge(inputSlot, dot.name, "in");
            externals.AddExternalPort(outputSlot.slotReference, nodeName);
        }
    }
}
