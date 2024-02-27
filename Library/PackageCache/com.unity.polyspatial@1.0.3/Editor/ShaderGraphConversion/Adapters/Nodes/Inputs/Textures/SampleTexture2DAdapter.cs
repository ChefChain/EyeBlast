namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleTexture2DAdapter : AbstractSampleTexture2DAdapter<SampleTexture2DNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Texture-2D-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SampleTexture2D",
                GetExpr("SAMPLE_TEXTURE2D(Texture, Sampler, UV)", ((SampleTexture2DNode)node).textureType));
        }
    }
}
