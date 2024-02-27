namespace UnityEditor.ShaderGraph.MaterialX
{
    class DistanceAdapter : ANodeAdapter<DistanceNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Distance-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Distance", "Out = distance(A, B);");
        }
    }
}