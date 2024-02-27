namespace UnityEditor.ShaderGraph.MaterialX
{
    class FlipAdapter : ANodeAdapter<FlipNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Flip-Node.html
            var dataType = NodeUtils.GetDataTypeName(node);
            var flipNode = (FlipNode)node;
            
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ChannelMask", new()
            {
                ["Out"] = new(MtlxNodeTypes.Multiply, dataType, new()
                {
                    ["in1"] = new ExternalInputDef("In"),
                    ["in2"] = new FloatInputDef(dataType, new[]
                    {
                        flipNode.redChannel.isOn ? -1.0f : 1.0f,
                        flipNode.greenChannel.isOn ? -1.0f : 1.0f,
                        flipNode.blueChannel.isOn ? -1.0f : 1.0f,
                        flipNode.alphaChannel.isOn ? -1.0f : 1.0f,
                    }),
                }),
            });
        }
    }
}