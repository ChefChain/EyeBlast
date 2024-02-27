namespace UnityEditor.ShaderGraph.MaterialX
{
    class CubemapAssetAdapter : ANodeAdapter<CubemapAssetNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var slot = NodeUtils.GetPrimaryOutput(node);
            QuickNode.AddImplicitPropertyFromNode(
                Texture2DAssetAdapter.GetVariableNameForSlot(slot), MtlxDataTypes.Filename,
                node, graph, externals, slot.RawDisplayName());
        }
    }
}