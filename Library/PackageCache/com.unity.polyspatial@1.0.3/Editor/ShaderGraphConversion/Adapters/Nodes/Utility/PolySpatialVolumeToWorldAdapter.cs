namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolySpatialVolumeToWorldAdapter : ANodeAdapter<PolySpatialVolumeToWorldNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "PolySpatialVolumeToWorld",
                ((PolySpatialVolumeToWorldNode)node).GetFunctionBody());
        }
    }
}