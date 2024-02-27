
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class VertexColorAdapter : ANodeAdapter<VertexColorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.GeomColor, node, graph, externals, "VertexColor");
            nodeData.datatype = MtlxDataTypes.Color4;
        }
    }
}
