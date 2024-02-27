using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SplitLRAdapter : ANodeAdapter<SplitLRNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            Dictionary<string, string> portMap = new()
            {
                ["Value L"] = "valuel",
                ["Value R"] = "valuer",
                ["Center"] = "center",
                ["UV"] = "texcoord",
            };
            QuickNode.NaryOp(MtlxNodeTypes.SplitLR, node, graph, externals, "SplitLR", portMap);
        }
    }
}