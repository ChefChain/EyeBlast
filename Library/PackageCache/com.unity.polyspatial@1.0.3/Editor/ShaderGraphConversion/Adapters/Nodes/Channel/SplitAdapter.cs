
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SplitAdapter : ANodeAdapter<SplitNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeName = NodeUtils.GetNodeName(node, "Split");
            var inputSlot = NodeUtils.GetSlotByName(node, "In");

            var inputType = SlotUtils.GetDataTypeName(inputSlot);
            int len = MtlxDataTypes.GetLength(inputType);

            var inputNode = graph.AddNode(nodeName, MtlxNodeTypes.Dot, inputType);
            inputNode.AddPortValue("in", inputType, SlotUtils.GetDefaultValue(inputSlot));
            externals.AddExternalPortAndEdge(inputSlot, inputNode.name, "in");

            string portNames = "RGBA";

            var outputs = new List<MaterialSlot>();
            node.GetOutputSlots(outputs);
            for(int i = 0; i < len; ++i)
            {
                if (len == 1)
                {
                    externals.AddExternalPort(outputs[i].slotReference, nodeName);
                }
                else
                {
                    var outNodeData = graph.AddNode($"{nodeName}{portNames[i]}", MtlxNodeTypes.Extract, MtlxDataTypes.Float);
                    graph.AddPortAndEdge(nodeName, outNodeData.name, "in", inputType);
                    outNodeData.AddPortValue("index", MtlxDataTypes.Integer, new float[] { i });
                    externals.AddExternalPort(outputs[i].slotReference, outNodeData.name);
                }
            }
        }
    }
}
