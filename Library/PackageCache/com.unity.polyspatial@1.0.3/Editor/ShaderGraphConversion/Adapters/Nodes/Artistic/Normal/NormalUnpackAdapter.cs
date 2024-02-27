namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalUnpackAdapter : ANodeAdapter<NormalUnpackNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is not NormalUnpackNode normalUnpackNode)
                return;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Unpack-Node.html
            // (although it just uses predefined functions: UnpackNormalmapRGorAG or UnpackNormalmapRGB)
            switch (normalUnpackNode.normalMapSpace)
            {
                case NormalMapSpace.Tangent:
                    QuickNode.CompoundOp(
                        node, graph, externals, sgContext, "NormalUnpack", @"
float2 xy = float2(In.r * In.a, In.g) * 2.0 - 1.0;
Out = float3(xy, max(1.0e-16, sqrt(1.0 - saturate(dot(xy, xy)))));");
                    break;
                
                case NormalMapSpace.Object:
                    QuickNode.CompoundOp(
                        node, graph, externals, sgContext, "NormalUnpack", "Out = In.rgb * 2.0 - 1.0;");
                    break;
            }
        }
    }
}