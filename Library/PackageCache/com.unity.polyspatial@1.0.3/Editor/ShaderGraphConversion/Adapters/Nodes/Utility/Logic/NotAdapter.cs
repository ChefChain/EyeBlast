namespace UnityEditor.ShaderGraph.MaterialX
{
    class NotAdapter : ANodeAdapter<NotNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Not-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Not", "Out = !In;");
        }
    }
}