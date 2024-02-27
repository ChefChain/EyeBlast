
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PowerAdapter : ANodeAdapter<PowerNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.BinaryOp(MtlxNodeTypes.Power, node, graph, externals, "Power");
        }
    }
}
