
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class AddAdapter : ANodeAdapter<AddNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.BinaryOp(MtlxNodeTypes.Add, node, graph, externals, "Add");
        }
    }
}
