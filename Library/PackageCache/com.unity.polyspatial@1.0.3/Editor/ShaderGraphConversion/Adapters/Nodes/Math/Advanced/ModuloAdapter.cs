
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ModuloAdapter : ANodeAdapter<ModuloNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.BinaryOp(MtlxNodeTypes.Modulo, node, graph, externals, "Modulo"); //in2 is the input edge.
        }
    }
}
