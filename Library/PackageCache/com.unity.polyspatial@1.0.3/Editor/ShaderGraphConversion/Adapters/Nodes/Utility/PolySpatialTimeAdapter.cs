using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolySpatialTimeAdapter : ANodeAdapter<PolySpatialTimeNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "PolySpatialTime", new()
            {
                ["Out"] = new(MtlxNodeTypes.Time, MtlxDataTypes.Float, new()),
            });
        }
    }
}