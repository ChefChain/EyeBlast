namespace UnityEditor.ShaderGraph.MaterialX
{
    class SamplerStateAdapter : ANodeAdapter<SamplerStateNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            if (node is not SamplerStateNode samplerStateNode)
                return "";
            
            return samplerStateNode.wrap switch
            {
                TextureSamplerState.WrapMode.MirrorOnce => "MirrorOnce wrap mode is not supported.",
                _ => "",
            };
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Sampler states don't correspond to nodes in MaterialX; instead, they're properties of the image node.
        }
    }
}