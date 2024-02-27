

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SaturateAdapter : ANodeAdapter<SaturateNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // clamp is a ternary, but it's defaults are 0-1 range, which matches saturate(...) impl in hlsl.
            // so we can just use the UnaryOp.
            QuickNode.UnaryOp(MtlxNodeTypes.Clamp, node, graph, externals, "Saturate");
        }
    }
}
