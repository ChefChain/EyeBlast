
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class FloorAdapter : ANodeAdapter<FloorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.UnaryOp(MtlxNodeTypes.Floor, node, graph, externals, "Floor");
        }
    }
}
