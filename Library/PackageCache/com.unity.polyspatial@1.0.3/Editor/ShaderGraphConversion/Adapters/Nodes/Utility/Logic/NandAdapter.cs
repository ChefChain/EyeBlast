namespace UnityEditor.ShaderGraph.MaterialX
{
    class NandAdapter : ANodeAdapter<NandNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Nand-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Nand", "Out = !A && !B;");
        }
    }
}