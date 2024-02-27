using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Matrix2Adapter : ANodeAdapter<Matrix2Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is Matrix2Node mnode)
            {
                var value = PropertyAdapter.GetDefaultValue(mnode.AsShaderProperty());

                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Matrix2");
                nodeData.AddPortValue("value", MtlxDataTypes.Matrix22, value);
            }
        }
    }
}