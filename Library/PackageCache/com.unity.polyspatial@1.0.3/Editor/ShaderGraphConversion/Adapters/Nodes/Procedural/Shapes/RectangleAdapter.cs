namespace UnityEditor.ShaderGraph.MaterialX
{
    class RectangleAdapter : AbstractUVNodeAdapter<RectangleNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Rectangle-Node.html
            // (but MaterialX doesn't support fwidth, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Rectangle", @"
float2 d = abs(UV * 2 - 1) - float2(Width, Height);
Out = splitlr(1, 0, 0, float2(max(d.x, d.y), 0));");
        }
    }
}