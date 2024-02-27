namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalFromTextureAdapter : ANodeAdapter<NormalFromTextureNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Normal-From-Texture-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "NormalFromTexture", @"
float uvOffset = pow(Offset, 3) * 0.1;
float2 offsetU = float2(UV.x + uvOffset, UV.y);
float2 offsetV = float2(UV.x, UV.y + uvOffset);
float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU);
float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV);
float3 va = float3(1, 0, (uSample - normalSample) * Strength);
float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
Out = normalize(cross(va, vb));");
        }
    }
}