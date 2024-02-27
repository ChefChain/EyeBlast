using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Vector2Adapter : ANodeAdapter<Vector2Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("X", "in1");
            portMap.Add("Y", "in2");

            QuickNode.NaryOp(MtlxNodeTypes.Combine2, node, graph, externals, "Vector2", portMap);
        }
    }
}
