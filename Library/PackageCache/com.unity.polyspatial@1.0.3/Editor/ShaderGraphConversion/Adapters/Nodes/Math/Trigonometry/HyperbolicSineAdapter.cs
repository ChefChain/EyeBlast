namespace UnityEditor.ShaderGraph.MaterialX
{
    class HyperbolicSineAdapter : ANodeAdapter<HyperbolicSineNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "HyperbolicSine", "Out = sinh(In);");
        }
    }
}