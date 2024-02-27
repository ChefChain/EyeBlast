namespace UnityEditor.ShaderGraph.MaterialX
{
    class SphereMaskAdapter : ANodeAdapter<SphereMaskNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Sphere-Mask-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SphereMask",
                "Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));");
        }
    }
}