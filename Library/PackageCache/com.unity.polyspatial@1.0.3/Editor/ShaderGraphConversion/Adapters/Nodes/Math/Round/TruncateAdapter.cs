namespace UnityEditor.ShaderGraph.MaterialX
{
    class TruncateAdapter : ANodeAdapter<TruncateNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Truncate", "Out = trunc(In);");
        }
    }
}