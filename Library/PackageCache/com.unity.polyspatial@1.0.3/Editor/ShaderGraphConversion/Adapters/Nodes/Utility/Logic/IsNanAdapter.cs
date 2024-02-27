namespace UnityEditor.ShaderGraph.MaterialX
{
    class IsNanAdapter : ANodeAdapter<IsNanNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "IsNan", "Out = isnan(In);");
        }
    }
}
