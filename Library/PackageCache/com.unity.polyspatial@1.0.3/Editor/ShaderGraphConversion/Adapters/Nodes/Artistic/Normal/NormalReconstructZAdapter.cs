namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalReconstructZAdapter : ANodeAdapter<NormalReconstructZNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Normal-Reconstruct-Z-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "NormalReconstructZ", @"
float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
float3 normalVector = float3(In.x, In.y, reconstructZ);
Out = normalize(normalVector);");
        }
    }
}
