
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MaximumAdapter : ANodeAdapter<MaximumNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.BinaryOp(MtlxNodeTypes.Maximum, node, graph, externals, "Maximum");
        }
    }
}
