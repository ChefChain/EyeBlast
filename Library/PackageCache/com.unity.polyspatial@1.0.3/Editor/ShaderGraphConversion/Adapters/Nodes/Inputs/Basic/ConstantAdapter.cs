using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class ConstantAdapter : ANodeAdapter<ConstantNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ConstantNode cnode)
            {
                var value = cnode.constant switch
                {
                    ConstantType.PI => 3.1415926f,
                    ConstantType.TAU => 6.28318530f,
                    ConstantType.PHI => 1.618034f,
                    ConstantType.E => 2.718282f,
                    ConstantType.SQRT2 => 1.414214f,
                    _ => 1.0f
                };

                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, $"Constant{cnode.constant.ToString()}");
                nodeData.AddPortValue("value", MtlxDataTypes.Float, new float[] { value });
            }
        }
    }
}
