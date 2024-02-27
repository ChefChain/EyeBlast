namespace UnityEditor.ShaderGraph.MaterialX
{
    class SaturationAdapter : ANodeAdapter<SaturationNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Saturation-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Saturation", @"
float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
Out = luma.xxx + Saturation.xxx * (In - luma.xxx);");
        }
    }
}
