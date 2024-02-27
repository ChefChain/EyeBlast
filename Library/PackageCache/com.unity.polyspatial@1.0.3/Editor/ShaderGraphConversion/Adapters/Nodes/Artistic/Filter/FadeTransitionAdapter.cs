namespace UnityEditor.ShaderGraph.MaterialX
{
    class FadeTransitionAdapter : ANodeAdapter<FadeTransitionNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Fade-Transition-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "FadeTransition",
                "Fade = saturate(FadeValue * (FadeContrast + 1) + (NoiseValue - 1) * FadeContrast);");
        }
    }
}