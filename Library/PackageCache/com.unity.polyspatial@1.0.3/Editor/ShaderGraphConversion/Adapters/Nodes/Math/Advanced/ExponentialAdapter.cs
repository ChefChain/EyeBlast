
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ExponentialAdapter : ANodeAdapter<ExponentialNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ExponentialNode enode)
            {
                switch(enode.exponentialBase)
                {
                    case ExponentialBase.BaseE:
                        QuickNode.UnaryOp(MtlxNodeTypes.Exponential, node, graph, externals, "ExpBaseE");
                        break;
                    case ExponentialBase.Base2:
                        var nodeData = QuickNode.UnaryOp(MtlxNodeTypes.Power, node, graph, externals, "ExpBase2", "in2");
                        nodeData.AddPortValue("in1", nodeData.datatype, new float[] { 2, 2, 2, 2 });
                        break;
                }
            }
        }
    }
}
