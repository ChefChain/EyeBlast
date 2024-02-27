namespace UnityEditor.ShaderGraph.MaterialX
{
    class Texture2DAssetAdapter : ANodeAdapter<Texture2DAssetNode>
    {
        public static string GetVariableNameForSlot(MaterialSlot slot)
        {
            // Texture2DAssetNode overrides GetVariableNameForSlot to wrap the variable name in
            // a function call: UnityBuildTexture2DStructNoScale(...)
            var variableName = slot.owner.GetVariableNameForSlot(slot.id);
            var startIndex = variableName.IndexOf('(');
            var endIndex = variableName.LastIndexOf(')');
            if (startIndex != -1 && endIndex != -1)
                variableName = variableName.Substring(startIndex + 1, endIndex - startIndex - 1);
            
            return variableName;
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var slot = NodeUtils.GetPrimaryOutput(node);
            QuickNode.AddImplicitPropertyFromNode(
                GetVariableNameForSlot(slot), MtlxDataTypes.Filename, node, graph, externals, slot.RawDisplayName());
        }
    }
}
