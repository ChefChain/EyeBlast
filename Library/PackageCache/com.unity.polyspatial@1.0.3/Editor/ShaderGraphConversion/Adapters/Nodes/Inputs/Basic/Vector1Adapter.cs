using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Vector1Adapter : ANodeAdapter<Vector1Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("X", "value");

            QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Vector1", portMap);
        }
    }
}
