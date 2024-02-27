namespace UnityEditor.ShaderGraph.MaterialX
{
    class ProjectionAdapter : ANodeAdapter<ProjectionNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Projection-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Projection", "Out = B * dot(A, B) / dot(B, B);");
        }
    }
}