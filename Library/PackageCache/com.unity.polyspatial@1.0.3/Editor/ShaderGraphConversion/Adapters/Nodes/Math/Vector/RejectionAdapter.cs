namespace UnityEditor.ShaderGraph.MaterialX
{
    class RejectionAdapter : ANodeAdapter<RejectionNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Rejection-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Rejection", "Out = A - (B * dot(A, B) / dot(B, B));");
        }
    }
}