namespace UnityEditor.ShaderGraph.MaterialX
{
    class IsInfiniteAdapter : ANodeAdapter<IsInfiniteNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "IsInfinite", "Out = isinf(In);");
        }
    }
}