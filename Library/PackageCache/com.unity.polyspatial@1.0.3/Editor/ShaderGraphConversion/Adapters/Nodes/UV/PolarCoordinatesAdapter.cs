namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolarCoordinatesAdapter : AbstractUVNodeAdapter<PolarCoordinatesNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Polar-Coordinates-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "PolarCoordinates", @"
float2 delta = UV - Center;
float radius = length(delta) * 2 * RadialScale;
float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
Out = float2(radius, angle);");
        }
    }
}