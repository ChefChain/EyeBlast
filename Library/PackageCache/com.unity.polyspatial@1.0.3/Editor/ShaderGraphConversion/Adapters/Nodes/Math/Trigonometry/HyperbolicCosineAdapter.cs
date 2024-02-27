namespace UnityEditor.ShaderGraph.MaterialX
{
    class HyperbolicCosineAdapter : ANodeAdapter<HyperbolicCosineNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "HyperbolicCosine", "Out = cosh(In);");
        }
    }
}