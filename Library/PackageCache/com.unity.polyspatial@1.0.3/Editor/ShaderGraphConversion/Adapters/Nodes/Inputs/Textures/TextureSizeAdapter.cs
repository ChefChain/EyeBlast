using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TextureSizeAdapter : ANodeAdapter<Texture2DPropertiesNode>
    {
        internal static string EnsureTextureSizeProperty(MaterialSlot slot, MtlxGraphData graph)
        {
            string textureSizeNodeName;
            var isSystemInput = true;
            var srcSlot = SlotUtils.GetRedirectedSourceConnectionSlot(slot);
            if (srcSlot == null)
            {
                textureSizeNodeName = GetTextureSizeNodeName(slot.owner.GetVariableNameForSlot(slot.id));
            }
            else if (srcSlot.owner is PropertyNode propertyNode)
            {
                textureSizeNodeName = GetTextureSizeNodeName(propertyNode.property.referenceName);
                isSystemInput = false;
            }
            else
            {
                textureSizeNodeName = GetTextureSizeNodeName(Texture2DAssetAdapter.GetVariableNameForSlot(srcSlot));
            }
            if (!graph.HasNode(textureSizeNodeName))
            {
                var nodeData = graph.AddNode(
                    textureSizeNodeName, MtlxNodeTypes.Constant, MtlxDataTypes.Vector3, !isSystemInput, isSystemInput);
                nodeData.AddPortValue("value", MtlxDataTypes.Vector3, new float[3]);
            }
            return textureSizeNodeName;
        }

        internal static string GetTextureSizeNodeName(string texturePropertyName)
        {
            return $"TextureSize{texturePropertyName}";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Unconnected texture slots correspond to implicit properties.
            var slot = NodeUtils.GetPrimaryInput(node);
            if (!slot.isConnected)
                QuickNode.EnsureImplicitProperty(node.GetVariableNameForSlot(slot.id), MtlxDataTypes.Filename, graph);
    
            var sizeNodeName = EnsureTextureSizeProperty(slot, graph);

            QuickNode.CompoundOp(node, graph, externals, sgContext, "TextureSize", new()
            {
                ["Width"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new ImplicitInputDef(sizeNodeName, MtlxDataTypes.Vector3),
                    ["channels"] = new StringInputDef("x"),
                }),
                ["Height"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new ImplicitInputDef(sizeNodeName, MtlxDataTypes.Vector3),
                    ["channels"] = new StringInputDef("y"),
                }),
                ["Texel Width"] = new(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new InternalInputDef("Width"),
                }),
                ["Texel Height"] = new(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new InternalInputDef("Height"),
                }),
            });
        }
    }
}