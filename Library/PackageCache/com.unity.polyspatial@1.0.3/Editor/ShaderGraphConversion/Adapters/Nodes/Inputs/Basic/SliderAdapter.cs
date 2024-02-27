using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class SliderAdapter : ANodeAdapter<SliderNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is SliderNode snode)
            {
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Slider", outputType: MtlxDataTypes.Float);
                var value = new float[] { snode.value.x };
                nodeData.AddPortValue("value", MtlxDataTypes.Float, value);
            }
        }
    }
}
