namespace UnityEditor.ShaderGraph.MaterialX
{
    class AndAdapter : ANodeAdapter<AndNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/And-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "And", "Out = A && B;");
        }
    }
}