
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class DotProductAdapter : ANodeAdapter<DotProductNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // dot product isn't supported for non-vectors?
            string nodeType = (MtlxDataTypes.GetLength(SlotUtils.GetDataTypeName(NodeUtils.GetSlotByName(node, "A"))) == 1) ? MtlxNodeTypes.Multiply : MtlxNodeTypes.DotProduct;

            var portMap = new Dictionary<string, string>();

            portMap.Add("A", "in1");
            portMap.Add("B", "in2");

            QuickNode.NaryOp(nodeType, node, graph, externals, "DotProduct", usgToMtlxPortMap: portMap, outputType: MtlxDataTypes.Float);
        }
    }
}
