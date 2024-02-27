namespace UnityEditor.ShaderGraph.MaterialX
{
    class SawtoothWaveAdapter : ANodeAdapter<SawtoothWaveNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Sawtooth-Wave-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "SawtoothWave", "Out = 2 * (In - floor(0.5 + In));");
        }
    }
}