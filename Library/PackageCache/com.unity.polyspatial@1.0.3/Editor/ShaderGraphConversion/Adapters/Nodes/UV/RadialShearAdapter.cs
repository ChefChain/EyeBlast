namespace UnityEditor.ShaderGraph.MaterialX
{
    class RadialShearAdapter : AbstractUVNodeAdapter<RadialShearNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Radial-Shear-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "RadialShear", @"
float2 delta = UV - Center;
float delta2 = dot(delta.xy, delta.xy);
float2 delta_offset = delta2 * Strength;
Out = UV + float2(delta.y, -delta.x) * delta_offset + Offset;");
        }
    }
}