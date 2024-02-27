namespace UnityEditor.ShaderGraph.MaterialX
{
    class MetalReflectanceAdapter : ANodeAdapter<MetalReflectanceNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is not MetalReflectanceNode metalReflectanceNode)
                return;
            
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Metal-Reflectance-Node.html
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "MetalReflectance");
            nodeData.AddPortValue("value", MtlxDataTypes.Vector3, metalReflectanceNode.material switch
            {
                MetalMaterialType.Iron => new[] { 0.560f, 0.570f, 0.580f },
                MetalMaterialType.Silver => new[] { 0.972f, 0.960f, 0.915f },
                MetalMaterialType.Aluminium => new[] { 0.913f, 0.921f, 0.925f },
                MetalMaterialType.Gold => new[] { 1.000f, 0.766f, 0.336f },
                MetalMaterialType.Copper => new[] { 0.955f, 0.637f, 0.538f },
                MetalMaterialType.Chromium => new[] { 0.550f, 0.556f, 0.554f },
                MetalMaterialType.Nickel => new[] { 0.660f, 0.609f, 0.526f },
                MetalMaterialType.Titanium => new[] { 0.542f, 0.497f, 0.449f },
                MetalMaterialType.Cobalt => new[] { 0.662f, 0.655f, 0.634f },
                MetalMaterialType.Platinum => new[] { 0.672f, 0.637f, 0.585f },
                _ => new[] { 1.0f, 0.0f, 1.0f },
            });
        }
    }
}