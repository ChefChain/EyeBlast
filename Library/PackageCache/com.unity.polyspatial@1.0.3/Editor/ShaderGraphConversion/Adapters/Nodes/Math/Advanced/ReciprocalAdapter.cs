namespace UnityEditor.ShaderGraph.MaterialX
{
    class ReciprocalAdapter : ANodeAdapter<ReciprocalNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Reciprocal", "Out = rcp(In);");
        }
    }
}