
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class Vector3Adapter : ANodeAdapter<Vector3Node>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            portMap.Add("X", "in1");
            portMap.Add("Y", "in2");
            portMap.Add("Z", "in3");

            QuickNode.NaryOp(MtlxNodeTypes.Combine3, node, graph, externals, "Vector3", portMap);
        }
    }
}
