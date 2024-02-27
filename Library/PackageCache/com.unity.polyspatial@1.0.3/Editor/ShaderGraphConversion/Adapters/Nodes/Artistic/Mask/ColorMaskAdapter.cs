namespace UnityEditor.ShaderGraph.MaterialX
{
    class ColorMaskAdapter : ANodeAdapter<ColorMaskNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Color-Mask-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "ColorMask", @"
float Distance = distance(MaskColor, In);
Out = saturate(1 - (Distance - Range) / max(Fuzziness, 1e-5));");
        }
    }
}