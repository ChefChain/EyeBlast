
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class LengthAdapter : ANodeAdapter<LengthNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var inputType = SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node));
            string nodeType = MtlxDataTypes.GetLength(inputType) == 1 ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            QuickNode.UnaryOp(nodeType, node, graph, externals, "Length");
        }
    }
}
