namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolySpatialLightingAdapter : ANodeAdapter<PolySpatialLightingNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "PolySpatialLighting",
                ((PolySpatialLightingNode)node).GetFunctionBody());
        }
    }
}