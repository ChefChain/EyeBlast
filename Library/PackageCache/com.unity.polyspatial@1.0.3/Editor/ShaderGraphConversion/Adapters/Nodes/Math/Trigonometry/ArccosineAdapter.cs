
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ArccosineAdapter : ANodeAdapter<ArccosineNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Arccosine, node, graph, externals, "Arccosine");
        }
    }
}
