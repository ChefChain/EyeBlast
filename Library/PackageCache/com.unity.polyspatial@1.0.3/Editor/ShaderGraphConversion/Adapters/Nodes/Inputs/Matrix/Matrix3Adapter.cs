using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Matrix3Adapter : ANodeAdapter<Matrix3Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is Matrix3Node mnode)
            {
                var value = PropertyAdapter.GetDefaultValue(mnode.AsShaderProperty());

                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Matrix3");
                nodeData.AddPortValue("value", MtlxDataTypes.Matrix33, value);
            }
        }
    }
}
