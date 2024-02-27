namespace UnityEditor.ShaderGraph.MaterialX
{
    class SpherizeAdapter : AbstractUVNodeAdapter<SpherizeNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Spherize-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Spherize", @"
float2 delta = UV - Center;
float delta2 = dot(delta.xy, delta.xy);
float delta4 = delta2 * delta2;
float2 delta_offset = delta4 * Strength;
Out = UV + delta * delta_offset + Offset;");
        }
    }
}