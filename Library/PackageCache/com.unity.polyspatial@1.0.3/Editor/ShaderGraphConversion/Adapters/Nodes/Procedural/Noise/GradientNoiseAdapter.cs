namespace UnityEditor.ShaderGraph.MaterialX
{
    class GradientNoiseAdapter : AbstractUVNodeAdapter<GradientNoiseNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "GradientNoise", new()
            {
                ["Out"] = new(MtlxNodeTypes.PerlinNoise2d, MtlxDataTypes.Float, new()
                {
                    ["amplitude"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["pivot"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["texcoord"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new ExternalInputDef("UV"),
                        ["in2"] = new ExternalInputDef("Scale"),
                    }),
                })
            });
        }
    }
}
