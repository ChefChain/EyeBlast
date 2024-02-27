namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleCubemapAdapter : ANodeAdapter<SampleCubemapNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Reflected-Cubemap-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SampleCubemap",
                "Out = SAMPLE_TEXTURECUBE_LOD(Cube, Sampler, reflect(-ViewDir, Normal), LOD);");
        }
    }
}