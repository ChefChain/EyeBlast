namespace UnityEditor.ShaderGraph.MaterialX
{
    class EllipseAdapter : AbstractUVNodeAdapter<EllipseNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Ellipse-Node.html
            // (but MaterialX doesn't support fwidth, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Ellipse", @"
float d = length((UV * 2 - 1) / float2(Width, Height));
Out = splitlr(1, 0, 1, float2(d, 0));");
        }
    }
}