using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class BooleanAdapter : ANodeAdapter<BooleanNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is BooleanNode bnode)
            {
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Boolean", outputType: MtlxDataTypes.Float);

                var value = new float[] { bnode.m_Value ? 1 : 0 };

                nodeData.AddPortValue("value", MtlxDataTypes.Float, value);
            }
        }
    }
}
