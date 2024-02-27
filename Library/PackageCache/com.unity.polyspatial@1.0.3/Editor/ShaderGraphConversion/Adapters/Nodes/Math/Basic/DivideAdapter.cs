
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class DivideAdapter : ANodeAdapter<DivideNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // TODO: I don't think mtlx multiply handles vector transformation or even matrix concatenation.
            QuickNode.BinaryOp(MtlxNodeTypes.Divide, node, graph, externals, "Divide");
        }
    }
}
