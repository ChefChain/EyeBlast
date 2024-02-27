
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ArcsineAdapter : ANodeAdapter<ArcsineNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Arcsine, node, graph, externals, "Arcsine");
        }
    }
}
