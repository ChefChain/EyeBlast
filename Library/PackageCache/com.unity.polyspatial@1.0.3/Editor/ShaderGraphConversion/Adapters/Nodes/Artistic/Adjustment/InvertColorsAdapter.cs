namespace UnityEditor.ShaderGraph.MaterialX
{
    class InvertColorsAdapter : ANodeAdapter<InvertColorsNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Invert-Colors-Node.html
            var dataType = NodeUtils.GetDataTypeName(node);
            var invertColorsNode = (InvertColorsNode)node;

            var values = new float[MtlxDataTypes.GetLength(dataType)];
            if (invertColorsNode.redChannel.isOn)
                values[0] = 1.0f;
            if (invertColorsNode.greenChannel.isOn && values.Length > 1)
                values[1] = 1.0f;
            if (invertColorsNode.blueChannel.isOn && values.Length > 2)
                values[2] = 1.0f;
            if (invertColorsNode.alphaChannel.isOn && values.Length > 3)
                values[3] = 1.0f;
                
            QuickNode.CompoundOp(node, graph, externals, sgContext, "InvertColors", new()
            {
                ["Out"] = new(MtlxNodeTypes.Absolute, dataType, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, dataType, new()
                    {
                        ["in1"] = new FloatInputDef(dataType, values),
                        ["in2"] = new ExternalInputDef("In"),
                    }),
                }),
            });
        }
    }
}