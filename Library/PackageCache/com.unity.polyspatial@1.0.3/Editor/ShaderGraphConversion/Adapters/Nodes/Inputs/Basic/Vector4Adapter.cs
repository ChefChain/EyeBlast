
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Vector4Adapter : ANodeAdapter<Vector4Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("X", "in1");
            portMap.Add("Y", "in2");
            portMap.Add("Z", "in3");
            portMap.Add("W", "in4");

            QuickNode.NaryOp(MtlxNodeTypes.Combine4, node, graph, externals, "Vector4", portMap);
        }
    }
}
