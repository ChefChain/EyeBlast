using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    [Title("Utility", "PolySpatial Environment Radiance")]
    class PolySpatialEnvironmentRadianceNode : CodeFunctionNode
    {
        public override bool hasPreview => false;
        
        public PolySpatialEnvironmentRadianceNode()
        {
            name = "PolySpatial Environment Radiance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("PolySpatial_EnvironmentRadiance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string PolySpatial_EnvironmentRadiance(
            [Slot(0, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] ColorRGB BaseColor,
            [Slot(1, Binding.None)] Vector1 Roughness,
            [Slot(2, Binding.None)] Vector1 Specular,
            [Slot(3, Binding.None)] Vector1 Metallic,
            [Slot(4, Binding.WorldSpaceNormal)] Vector3 Normal,
            [Slot(5, Binding.ObjectSpaceViewDirection, true)] Vector3 ViewDirOS,
            [Slot(6, Binding.None)] out ColorRGB DiffuseRadiance,
            [Slot(7, Binding.None)] out ColorRGB SpecularRadiance)
        {
            return
@"
{
    // https://github.cds.internal.unity3d.com/unity/unity/blob/918aac026438f350a9716ff831b1e309f2483743/Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl#L82
    float4 kDialectricSpec = {0.04, 0.04, 0.04, 1.0 - 0.04};
    float oneMinusReflectivity = kDialectricSpec.a * (1.0 - Metallic);
    float reflectivity = 1.0 - oneMinusReflectivity;
    float3 brdfDiffuse = BaseColor * oneMinusReflectivity;
    float3 brdfSpecular = lerp(kDialectricSpec.rgb, BaseColor, Metallic) + BaseColor * Specular;
    
    // https://github.cds.internal.unity3d.com/unity/unity/blob/9eb48b21dbaea276a898a8ab6a2274a8e1e6f73c/Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl#L46
    float kHalfMin = 6.103515625e-5;
    float kHalfMinSqrt = 0.0078125;
    float perceptualRoughness = Roughness;
    float roughness = max(perceptualRoughness * perceptualRoughness, kHalfMinSqrt);
    float roughness2 = max(roughness * roughness, kHalfMin);
    float grazingTerm = saturate((1.0 - Roughness) + reflectivity);

    // https://github.cds.internal.unity3d.com/unity/unity/blob/e3e09beefeb3cbd8abd8e267dd80b22e5890968c/Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl#L416
    float3 normalOS = normalize(mul(float4(Normal, 0.0f), unity_ObjectToWorld).xyz);
    float NoV = saturate(dot(normalOS, ViewDirOS));
    float fresnelTerm = pow(1.0 - NoV, 4);
    float3 environmentBrdfSpecular = lerp(brdfSpecular, grazingTerm, fresnelTerm) / (roughness2 + 1.0);

    // As an approximation, treat the reflection probe as an irradiance map
    // and sample it along the normal at its blurriest.
    DiffuseRadiance = brdfDiffuse * SHADERGRAPH_REFLECTION_PROBE(normalOS, normalOS, 6.0);

    // https://github.cds.internal.unity3d.com/unity/unity/blob/5d88a17c0c2ef08e18187e62c5d6b08fa4463ab1/Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl#L27
    float mip = perceptualRoughness * (10.2 - 4.2 * perceptualRoughness);
    SpecularRadiance = environmentBrdfSpecular * SHADERGRAPH_REFLECTION_PROBE(ViewDirOS, normalOS, mip);
}";
        }
    }
}