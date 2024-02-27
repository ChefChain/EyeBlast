
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SineAdapter : ANodeAdapter<SineNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Sine, node, graph, externals, "Sine");
        }
    }
}
