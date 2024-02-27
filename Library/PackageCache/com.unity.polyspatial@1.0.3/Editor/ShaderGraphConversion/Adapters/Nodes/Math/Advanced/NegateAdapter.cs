
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NegateAdapter : ANodeAdapter<NegateNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.UnaryOp(MtlxNodeTypes.Multiply, node, graph, externals, "Negate", "in2");
            nodeData.AddPortValue("in1", nodeData.datatype, new float[] { -1, -1, -1, -1 });
        }
    }
}
