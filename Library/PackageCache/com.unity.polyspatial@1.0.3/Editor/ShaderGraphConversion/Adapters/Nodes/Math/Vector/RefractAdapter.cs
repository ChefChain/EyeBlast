namespace UnityEditor.ShaderGraph.MaterialX
{
    class RefractAdapter : ANodeAdapter<RefractNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Refract-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Refract", $@"
float internalIORSource = max(IORSource, 1.0);
float internalIORMedium = max(IORMedium, 1.0);
float eta = internalIORSource / internalIORMedium;
Refracted = refract(Incident, Normal, eta);

float cos0 = dot(Incident, Normal);
float k = 1.0 - eta*eta*(1.0 - cos0*cos0);

// https://github.cds.internal.unity3d.com/unity/unity/blob/1ade3ed2dfc1932d8c8427253060d0edaa1663d3/Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl#L137 
float SqrtIorToFresnel0 = (internalIORMedium - internalIORSource) / (internalIORMedium + internalIORSource);
float IorToFresnel0 = SqrtIorToFresnel0 * SqrtIorToFresnel0;

// https://github.cds.internal.unity3d.com/unity/unity/blob/1ade3ed2dfc1932d8c8427253060d0edaa1663d3/Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl#L59
float x = 1.0 + cos0;
float x2 = x * x;
float Transm_Schlick = (1.0 - x * x2 * x2) * (1.0 - IorToFresnel0);

// https://github.cds.internal.unity3d.com/unity/unity/blob/1ade3ed2dfc1932d8c8427253060d0edaa1663d3/Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl#L104
float ior = internalIORMedium / internalIORSource;
float g = sqrt(ior * ior + cos0 * cos0 - 1.0);
float t1 = (g + cos0) / (g - cos0);
float t2 = ((cos0 - g) * cos0 - 1.0) / (1.0 - (g + cos0) * cos0);
float FresnelDielectric = 1.0 - saturate(1.0 - 0.5 * t1 * t1 * (1.0 + t2 * t2));

Intensity = internalIORSource <= internalIORMedium ?
    saturate(Transm_Schlick) :
    (k >= 0.0 ? FresnelDielectric :
    {(((RefractNode)node).refractMode == RefractMode.Safe ? "0.0" : "1.0")});");
        }
    }
}