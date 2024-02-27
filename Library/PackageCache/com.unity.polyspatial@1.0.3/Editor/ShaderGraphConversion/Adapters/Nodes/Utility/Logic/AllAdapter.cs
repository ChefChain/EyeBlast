namespace UnityEditor.ShaderGraph.MaterialX
{
    class AllAdapter : ANodeAdapter<AllNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/All-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "All", "Out = all(In);");
        }
    }
}