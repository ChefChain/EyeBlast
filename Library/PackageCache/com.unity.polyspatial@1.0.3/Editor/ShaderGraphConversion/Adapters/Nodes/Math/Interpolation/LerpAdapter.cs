
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class LerpAdapter : ANodeAdapter<LerpNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("A", "bg");
            portMap.Add("B", "fg");
            portMap.Add("T", "mix");

            var portType = new Dictionary<string, string>();
            portType.Add("T", "float");

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Mix, node, graph, externals, "Lerp", portMap, portType);
        }
    }
}
