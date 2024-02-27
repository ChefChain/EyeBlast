
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SquareRootAdapter : ANodeAdapter<SquareRootNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.SquareRoot, node, graph, externals, "SquareRoot");
        }
    }
}
