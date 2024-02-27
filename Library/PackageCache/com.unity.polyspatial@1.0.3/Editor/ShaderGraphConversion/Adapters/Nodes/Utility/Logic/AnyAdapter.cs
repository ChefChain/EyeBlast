namespace UnityEditor.ShaderGraph.MaterialX
{
    class AnyAdapter : ANodeAdapter<AnyNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Any-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Any", "Out = any(In);");
        }
    }
}