namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalStrengthAdapter : ANodeAdapter<NormalStrengthNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Strength-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "NormalStrength",
                "Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));");
        }
    }
}