namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleRawCubemapAdapter : ANodeAdapter<SampleRawCubemapNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Cubemap-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SampleRawCubemap",
                "Out = SAMPLE_TEXTURECUBE_LOD(Cube, Sampler, Dir, LOD);");
        }
    }
}