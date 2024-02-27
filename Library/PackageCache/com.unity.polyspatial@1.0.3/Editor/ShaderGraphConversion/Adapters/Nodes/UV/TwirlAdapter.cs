namespace UnityEditor.ShaderGraph.MaterialX
{
    class TwirlAdapter : AbstractUVNodeAdapter<TwirlNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Twirl-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Twirl", @"
float2 delta = UV - Center;
float angle = Strength * length(delta);
float x = cos(angle) * delta.x - sin(angle) * delta.y;
float y = sin(angle) * delta.x + cos(angle) * delta.y;
Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);");
        }
    }
}