using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleTexture3DAdapter : ANodeAdapter<SampleTexture3DNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Texture-3D-Node.html
            var rgbaLine = ((SampleTexture3DNode)node).mipSamplingMode switch
            {
                Texture3DMipSamplingMode.Standard => "RGBA = SAMPLE_TEXTURE3D(Texture, Sampler, UV);",
                Texture3DMipSamplingMode.LOD => "RGBA = SAMPLE_TEXTURE3D_LOD(Texture, Sampler, UV, LOD);",
                var mipSamplingMode => throw new NotSupportedException($"Unknown sampling mode {mipSamplingMode}"),
            };
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SampleTexture3D",
                $"{rgbaLine} R = RGBA.r; G = RGBA.g; B = RGBA.b; A = RGBA.a;");
        }
    }
}