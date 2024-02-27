namespace UnityEditor.ShaderGraph.MaterialX
{
    class NoiseSineWaveAdapter : ANodeAdapter<NoiseSineWaveNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Noise-Sine-Wave-Node.html
            var type = MtlxDataTypes.GetHlslForType(NodeUtils.GetDataTypeName(node));
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "NoiseSineWave", $@"
{type} sinIn = sin(In);
{type} sinInOffset = sin(In + 1.0);
{type} randomno = frac(sin((sinIn - sinInOffset) * {12.9898f + 78.233f}) * 43758.5453);
{type} noise = lerp(MinMax.x, MinMax.y, randomno);
Out = sinIn + noise;");
        }
    }
}