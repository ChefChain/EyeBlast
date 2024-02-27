
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MultiplyAdapter : ANodeAdapter<MultiplyNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // TODO: I don't think mtlx multiply handles vector transformation or even matrix concatenation.
            var inputSlots = new List<MaterialSlot>();
            node.GetInputSlots<MaterialSlot>(inputSlots);

            var aType = SlotUtils.GetDataTypeName(inputSlots[0]);
            var bType = SlotUtils.GetDataTypeName(inputSlots[1]);

            if (aType == bType)
            {
                QuickNode.BinaryOp(MtlxNodeTypes.Multiply, node, graph, externals, "Multiply");
            }
            else
            {
                var portMap = new Dictionary<string, string>();
                var lhs = MtlxDataTypes.IsVector(aType);

                portMap.Add(lhs ? "A" : "B", "in");
                portMap.Add(lhs ? "B" : "A", "mat");

                QuickNode.NaryOp(MtlxNodeTypes.TransformMatrix, node, graph, externals, "Multiply", portMap);
            }
        }
    }
}
