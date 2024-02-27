namespace UnityEditor.ShaderGraph.MaterialX
{
    class ReplaceColorAdapter : ANodeAdapter<ReplaceColorNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Replace-Color-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "ReplaceColor", @"
float Distance = distance(From, In);
Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 1e-5f)));");
        }
    }
}