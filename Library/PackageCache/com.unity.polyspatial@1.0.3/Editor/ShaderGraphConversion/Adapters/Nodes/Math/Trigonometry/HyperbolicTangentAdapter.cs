namespace UnityEditor.ShaderGraph.MaterialX
{
    class HyperbolicTangentAdapter : ANodeAdapter<HyperbolicTangentNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "HyperbolicTangent", "Out = tanh(In);");
        }
    }
}