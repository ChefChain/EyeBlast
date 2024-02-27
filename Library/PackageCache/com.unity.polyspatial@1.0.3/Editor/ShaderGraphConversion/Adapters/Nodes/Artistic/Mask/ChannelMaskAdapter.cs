using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ChannelMaskAdapter : ANodeAdapter<ChannelMaskNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Channel-Mask-Node.html
            var dataType = NodeUtils.GetDataTypeName(node);
            var channelMask = ((ChannelMaskNode)node).channelMask;
            
            NodeDef outputDef;
            if (dataType == MtlxDataTypes.Float)
            {
                outputDef = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = ((channelMask & 1) == 0) ?
                        new FloatInputDef(MtlxDataTypes.Float, 0.0f) :
                        new ExternalInputDef("In"),
                });
            }
            else
            {
                var length = MtlxDataTypes.GetLength(dataType);

                Dictionary<string, InputDef> inputDefs = new();
                for (var i = 0; i < length; ++i)
                {
                    inputDefs[$"in{i + 1}"] = ((channelMask & 1 << i) == 0) ?
                        new FloatInputDef(MtlxDataTypes.Float, 0.0f) :
                        new InlineInputDef(MtlxNodeTypes.Extract, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new ExternalInputDef("In"),
                            ["index"] = new FloatInputDef(MtlxDataTypes.Integer, i),
                        });
                }
                outputDef = new($"combine{length}", dataType, inputDefs);
            }

            QuickNode.CompoundOp(node, graph, externals, sgContext, "ChannelMask", new()
            {
                ["Out"] = outputDef,
            });
        }
    }
}