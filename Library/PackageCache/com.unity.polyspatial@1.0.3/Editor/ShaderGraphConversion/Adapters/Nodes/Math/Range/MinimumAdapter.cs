
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MinimumAdapter : ANodeAdapter<MinimumNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.BinaryOp(MtlxNodeTypes.Minimum, node, graph, externals, "Minimum");
        }
    }
}
