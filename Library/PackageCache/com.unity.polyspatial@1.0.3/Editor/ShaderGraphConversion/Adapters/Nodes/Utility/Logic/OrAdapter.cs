namespace UnityEditor.ShaderGraph.MaterialX
{
    class OrAdapter : ANodeAdapter<OrNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Or-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Or", "Out = A || B;");
        }
    }
}