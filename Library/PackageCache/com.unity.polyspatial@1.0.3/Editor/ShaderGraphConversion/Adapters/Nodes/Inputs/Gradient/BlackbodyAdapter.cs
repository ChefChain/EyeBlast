namespace UnityEditor.ShaderGraph.MaterialX
{
    class BlackbodyAdapter : ANodeAdapter<BlackbodyNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Blackbody-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Blackbody", @"
float powTemperature = pow(Temperature, -1.5);
float logTemperature = log(Temperature);
float3 color = float3(
    56100000.0 * powTemperature + 148.0,
    (Temperature > 6500.0) ? 35200000.0 * powTemperature + 184.0 : 100.04 * logTemperature - 623.6,
    194.18 * logTemperature - 1448.6);
float3 rescaledColor = clamp(color, 0.0, 255.0) / 255.0;
Out = (Temperature < 1000.0) ? rescaledColor * Temperature / 1000.0 : rescaledColor;");
        }
    }
}
