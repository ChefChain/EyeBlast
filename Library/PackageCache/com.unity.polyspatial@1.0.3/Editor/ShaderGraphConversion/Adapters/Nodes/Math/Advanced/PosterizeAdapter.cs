namespace UnityEditor.ShaderGraph.MaterialX
{
    class PosterizeAdapter : ANodeAdapter<PosterizeNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Posterize-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Posterize", "Out = floor(In * Steps) / Steps;");
        }
    }
}