
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class Arctangent2Adapter : ANodeAdapter<Arctangent2Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("A", "iny");
            portMap.Add("B", "inx");

            QuickNode.NaryOp(MtlxNodeTypes.Arctangent2, node, graph, externals, "Arctangent2", portMap);
        }
    }
}
