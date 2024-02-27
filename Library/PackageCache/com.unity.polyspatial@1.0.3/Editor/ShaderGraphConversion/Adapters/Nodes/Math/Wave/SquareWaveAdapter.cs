namespace UnityEditor.ShaderGraph.MaterialX
{
    class SquareWaveAdapter : ANodeAdapter<SquareWaveNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Square-Wave-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SquareWave", "Out = 1.0 - 2.0 * round(frac(In));");
        }
    }
}