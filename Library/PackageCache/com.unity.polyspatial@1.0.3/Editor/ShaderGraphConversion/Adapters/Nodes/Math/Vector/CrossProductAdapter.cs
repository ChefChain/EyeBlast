
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CrossProductAdapter : ANodeAdapter<CrossProductNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.BinaryOp(MtlxNodeTypes.CrossProduct, node, graph, externals, "CrossProduct");
        }
    }
}
