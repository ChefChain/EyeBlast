
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SmoothStepAdapter : ANodeAdapter<SmoothstepNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("Edge1", "low");
            portMap.Add("Edge2", "high");
            portMap.Add("In", "in");

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.SmoothStep, node, graph, externals, "SmoothStep", portMap);
        }
    }
}
