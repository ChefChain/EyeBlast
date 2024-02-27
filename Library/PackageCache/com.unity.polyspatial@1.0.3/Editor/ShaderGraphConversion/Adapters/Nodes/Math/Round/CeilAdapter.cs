
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CeilAdapter : ANodeAdapter<CeilingNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Ceil, node, graph, externals, "Ceil");
        }
    }
}
