
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RoundAdapter : ANodeAdapter<RoundNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Note: Reference implementation doesn't implement 'round', but it is in the spec.
            QuickNode.UnaryOp(MtlxNodeTypes.Round, node, graph, externals, "Round");
        }
    }
}
