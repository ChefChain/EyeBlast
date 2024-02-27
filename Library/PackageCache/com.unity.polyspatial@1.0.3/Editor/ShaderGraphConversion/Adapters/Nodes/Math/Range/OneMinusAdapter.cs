
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class OneMinusAdapter : ANodeAdapter<OneMinusNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.UnaryOp(MtlxNodeTypes.Subtract, node, graph, externals, "OneMinus", "in2");
            nodeData.AddPortValue("in1", nodeData.datatype, new float[] { 1, 1, 1, 1 });
        }
    }
}
