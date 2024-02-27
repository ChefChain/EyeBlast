using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Matrix4Adapter : ANodeAdapter<Matrix4Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is Matrix4Node mnode)
            {
                var value = PropertyAdapter.GetDefaultValue(mnode.AsShaderProperty());

                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Matrix4");
                nodeData.AddPortValue("value", MtlxDataTypes.Matrix44, value);
            }
        }
    }
}
