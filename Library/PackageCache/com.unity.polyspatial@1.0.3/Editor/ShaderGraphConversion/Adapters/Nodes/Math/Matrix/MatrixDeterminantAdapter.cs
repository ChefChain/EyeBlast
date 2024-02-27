
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MatrixDeterminantAdapter : ANodeAdapter<MatrixDeterminantNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
            return !string.IsNullOrEmpty(SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node)));
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Determinant, node, graph, externals, "Determinant");
        }
    }
}
