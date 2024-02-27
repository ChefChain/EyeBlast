using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class IntegerAdapter : ANodeAdapter<IntegerNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is IntegerNode inode)
            {
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Integer", outputType: MtlxDataTypes.Float);

                var value = new float[] { inode.value };

                nodeData.AddPortValue("value", MtlxDataTypes.Float, value);
            }
        }
    }
}
