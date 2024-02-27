using System.Text;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleTexture2DLODAdapter : AbstractSampleTexture2DAdapter<SampleTexture2DLODNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Texture-2D-LOD-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SampleTexture2DLOD",
                GetExpr("SAMPLE_TEXTURE2D_LOD(Texture, Sampler, UV, LOD)", ((SampleTexture2DLODNode)node).textureType));
        }
    }
}