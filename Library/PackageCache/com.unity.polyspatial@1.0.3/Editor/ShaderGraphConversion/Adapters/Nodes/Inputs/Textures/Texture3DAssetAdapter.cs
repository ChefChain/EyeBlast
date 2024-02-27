namespace UnityEditor.ShaderGraph.MaterialX
{
    class Texture3DAssetAdapter : ANodeAdapter<Texture3DAssetNode>
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