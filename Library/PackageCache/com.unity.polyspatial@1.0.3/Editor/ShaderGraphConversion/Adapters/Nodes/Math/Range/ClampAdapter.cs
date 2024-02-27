
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ClampAdapter : ANodeAdapter<ClampNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("In", "in");
            portMap.Add("Min", "low");
            portMap.Add("Max", "high");


            QuickNode.NaryOp(MtlxNodeTypes.Clamp, node, graph, externals, "Clamp", portMap);
        }
    }
}
