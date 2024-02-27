namespace UnityEditor.ShaderGraph.MaterialX
{
    class InverseLerpAdapter : ANodeAdapter<InverseLerpNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Inverse-Lerp-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "InverseLerp", "Out = (T - A)/(B - A);");
        }
    }
}