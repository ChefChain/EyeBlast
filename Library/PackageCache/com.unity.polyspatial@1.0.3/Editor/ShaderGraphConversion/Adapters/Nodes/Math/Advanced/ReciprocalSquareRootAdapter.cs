namespace UnityEditor.ShaderGraph.MaterialX
{
    class ReciprocalSquareRootAdapter : ANodeAdapter<ReciprocalSquareRootNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ReciprocalSquareRoot", "Out = rsqrt(In);");
        }
    }
}