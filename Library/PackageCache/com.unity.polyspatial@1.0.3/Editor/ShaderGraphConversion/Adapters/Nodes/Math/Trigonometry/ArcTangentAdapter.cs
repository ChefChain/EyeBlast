
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ArctangentAdapter : ANodeAdapter<ArctangentNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("In", "iny");
            
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Arctangent2, node, graph, externals, "Arctangent", portMap);
            nodeData.AddPortValue("inx", NodeUtils.GetDataTypeName(node), new[] {1f, 1f, 1f, 1f});
        }
    }
}