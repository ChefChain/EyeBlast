using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    enum BakedLightingMode
    {
        None,
        Lightmap,
        LightProbes,
    }

    enum ReflectionProbeMode
    {
        None,
        Simple,
        Blended,
    }

    [Title("Utility", "PolySpatial Lighting")]
    class PolySpatialLightingNode : CodeFunctionNode
    {
        public PolySpatialLightingNode()
        {
            name = "PolySpatial Lighting";
        }

        [SerializeField]
        private BakedLightingMode m_BakedLightingMode = BakedLightingMode.None;

        [EnumControl("Baked Lighting")]
        public BakedLightingMode bakedLightingMode
        {
            get { return m_BakedLightingMode; }
            set
            {
                if (m_BakedLightingMode == value)
                    return;

                m_BakedLightingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private ReflectionProbeMode m_ReflectionProbeMode = ReflectionProbeMode.None;

        [EnumControl("Reflection Probes")]
        public ReflectionProbeMode reflectionProbeMode
        {
            get { return m_ReflectionProbeMode; }
            set
            {
                if (m_ReflectionProbeMode == value)
                    return;

                m_ReflectionProbeMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private bool m_DynamicLighting = true;

        [ToggleControl("Dynamic Lighting")]
        public ToggleData dynamicLighting
        {
            get { return new ToggleData(m_DynamicLighting); }
            set
            {
                if (m_DynamicLighting == value.isOn)
                    return;

                m_DynamicLighting = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview => false;
        
        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            void AddProperty(AbstractShaderProperty property, string referenceName, bool exposeAsHidden)
            {
                property.overrideReferenceName = referenceName;

                // We expose certain properties as hidden so that they will show up in the Shader.GetProperty APIs
                // (with the HideInInspector flag), allowing us to tell at runtime which lighting features are used.
                if (exposeAsHidden)
                    property.hidden = true;
                else
                    property.generatePropertyBlock = false;

                properties.AddShaderProperty(property);
            }

            void AddTexture2DProperty(string referenceName, bool exposeAsHidden = false)
            {
                var property = new Texture2DShaderProperty()
                {
                    defaultType = Texture2DShaderProperty.DefaultType.Black,
                };
                AddProperty(property, referenceName, exposeAsHidden);
            }

            void AddCubemapProperty(string referenceName, bool exposeAsHidden = false)
            {
                AddProperty(new CubemapShaderProperty(), referenceName, exposeAsHidden);
            }

            void AddFloatProperty(string referenceName, bool exposeAsHidden = false)
            {
                AddProperty(new Vector1ShaderProperty(), referenceName, exposeAsHidden);
            }

            void AddVector4Property(string referenceName, bool exposeAsHidden = false)
            {
                AddProperty(new Vector4ShaderProperty(), referenceName, exposeAsHidden);
            }

            void AddMatrix4Property(string referenceName, bool exposeAsHidden = false)
            {
                AddProperty(new Matrix4ShaderProperty(), referenceName, exposeAsHidden);
            }

            AddMatrix4Property(PolySpatialShaderProperties.VolumeToWorld);

            switch (m_BakedLightingMode)
            {
                case BakedLightingMode.Lightmap:
                    AddTexture2DProperty(PolySpatialShaderProperties.Lightmap);
                    AddTexture2DProperty(PolySpatialShaderProperties.LightmapInd);
                    AddVector4Property(PolySpatialShaderProperties.LightmapST);
                    break;

                case BakedLightingMode.LightProbes:
                    AddVector4Property(PolySpatialShaderProperties.SHAr, true);
                    AddVector4Property(PolySpatialShaderProperties.SHAg, true);
                    AddVector4Property(PolySpatialShaderProperties.SHAb, true);
                    AddVector4Property(PolySpatialShaderProperties.SHBr, true);
                    AddVector4Property(PolySpatialShaderProperties.SHBg, true);
                    AddVector4Property(PolySpatialShaderProperties.SHBb, true);
                    AddVector4Property(PolySpatialShaderProperties.SHC, true);
                    break;
            }

            if (m_BakedLightingMode != BakedLightingMode.None || m_ReflectionProbeMode != ReflectionProbeMode.None)
            {
                switch (m_ReflectionProbeMode)
                {
                    case ReflectionProbeMode.None:
                        AddVector4Property(PolySpatialShaderGlobals.GlossyEnvironmentColor);
                        break;
                    
                    case ReflectionProbeMode.Simple:
                        AddCubemapProperty(PolySpatialShaderProperties.ReflectionProbeTexturePrefix + "0", true);
                        break;
                    
                    case ReflectionProbeMode.Blended:
                        for (var i = 0; i < PolySpatialShaderProperties.ReflectionProbeCount; ++i)
                        {
                            AddCubemapProperty(PolySpatialShaderProperties.ReflectionProbeTexturePrefix + i, true);
                            AddFloatProperty(PolySpatialShaderProperties.ReflectionProbeWeightPrefix + i, true);
                        }
                        break;
                }
            }

            if (m_DynamicLighting)
            {
                for (var i = 0; i < PolySpatialShaderGlobals.LightCount; ++i)
                {
                    AddVector4Property(PolySpatialShaderGlobals.LightColorPrefix + i);
                    AddVector4Property(PolySpatialShaderGlobals.LightPositionPrefix + i);
                    AddVector4Property(PolySpatialShaderGlobals.SpotDirectionPrefix + i);
                    AddVector4Property(PolySpatialShaderGlobals.LightAttenPrefix + i);
                }
            }
        }

        internal string GetFunctionBody()
        {
            StringWriter writer = new();

            // Compute the world space normal from the tangent space input normal and tangent frame vectors.
            writer.WriteLine($@"
float3 normalVS = mul(Normal, float3x3(TangentFrameX, TangentFrameY, TangentFrameZ));
float3 normalWS = normalize(mul({PolySpatialShaderProperties.VolumeToWorld}, float4(normalVS, 0.0)).xyz);");
            
            // https://github.cds.internal.unity3d.com/unity/unity/blob/918aac026438f350a9716ff831b1e309f2483743/Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl#L82
            Color kDialectricSpec = new(0.04f, 0.04f, 0.04f, 1.0f - 0.04f);
            writer.WriteLine($"float oneMinusReflectivity = {kDialectricSpec.a} * (1.0 - Metallic);");
            writer.WriteLine("float reflectivity = 1.0 - oneMinusReflectivity;");
            writer.WriteLine("float3 brdfDiffuse = BaseColor * oneMinusReflectivity;");
            writer.WriteLine($@"
float3 brdfSpecular = lerp(
    float3({kDialectricSpec.r}, {kDialectricSpec.g}, {kDialectricSpec.b}), BaseColor, Metallic);");
            writer.WriteLine("float perceptualRoughness = 1.0 - Smoothness;");
            // https://github.cds.internal.unity3d.com/unity/unity/blob/9eb48b21dbaea276a898a8ab6a2274a8e1e6f73c/Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl#L46
            const float kHalfMin = 6.103515625e-5f;
            const float kHalfMinSqrt = 0.0078125f;
            writer.WriteLine($"float roughness = max(perceptualRoughness * perceptualRoughness, {kHalfMinSqrt});");
            writer.WriteLine($"float roughness2 = max(roughness * roughness, {kHalfMin});");
            writer.WriteLine($"float roughness2MinusOne = roughness2 - 1.0;");
            writer.WriteLine("float normalizationTerm = roughness * 4.0 + 2.0;");
            writer.WriteLine("float grazingTerm = saturate(Smoothness + reflectivity);");
            writer.WriteLine($@"
float3 viewDirectionWS = normalize(mul({PolySpatialShaderProperties.VolumeToWorld},
    float4(ViewDirectionVS, 0.0)).xyz);");

            StringBuilder finalSumExpr = new("Emission");

            switch (m_BakedLightingMode)
            {
                case BakedLightingMode.Lightmap:
                    // https://github.cds.internal.unity3d.com/unity/unity/blob/1ade3ed2dfc1932d8c8427253060d0edaa1663d3/Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl#L251
                    writer.WriteLine($@"
float2 uv = LightmapUV * {PolySpatialShaderProperties.LightmapST}.xy + {PolySpatialShaderProperties.LightmapST}.zw;");
                    writer.WriteLine($@"
float4 direction = SAMPLE_TEXTURE2D(
    {PolySpatialShaderProperties.LightmapInd}, sampler{PolySpatialShaderProperties.LightmapInd}, uv);");
                    writer.WriteLine($@"
float4 encodedIlluminance = SAMPLE_TEXTURE2D(
    {PolySpatialShaderProperties.Lightmap}, sampler{PolySpatialShaderProperties.Lightmap}, uv);");
                    // Note: this only supports the dLDR encoding (which appears to be the default).
                    const float kLightmapHdrMultiplier = 4.59f;
                    writer.WriteLine($"float3 illuminance = encodedIlluminance.rgb * {kLightmapHdrMultiplier};");
                    writer.WriteLine("float halfLambert = dot(normalWS, direction.xyz - 0.5) + 0.5;");
                    writer.WriteLine("float3 bakedGI = illuminance * halfLambert / max(1e-4, direction.w);");
                    break;
                
                case BakedLightingMode.LightProbes:
                    // https://github.cds.internal.unity3d.com/unity/unity/blob/f5741f9e623093a5514dc13a534a4044e0d7e0ec/Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl#L116
                    writer.WriteLine("float4 vA = float4(normalWS, 1.0);");
                    writer.WriteLine("float4 vB = normalWS.xyzz * normalWS.yzzx;");
                    writer.WriteLine("float vC = normalWS.x * normalWS.x - normalWS.y * normalWS.y;");
                    writer.WriteLine($@"
float3 x1 = float3(
    dot({PolySpatialShaderProperties.SHAr}, vA),
    dot({PolySpatialShaderProperties.SHAg}, vA),
    dot({PolySpatialShaderProperties.SHAb}, vA));");
                    writer.WriteLine($@"
float3 x2 = float3(
    dot({PolySpatialShaderProperties.SHBr}, vB),
    dot({PolySpatialShaderProperties.SHBg}, vB),
    dot({PolySpatialShaderProperties.SHBb}, vB));");
                    writer.WriteLine($"float3 x3 = {PolySpatialShaderProperties.SHC}.rgb * vC;");
                    writer.WriteLine("float3 bakedGI = max(float3(0, 0, 0), x1 + x2 + x3);");
                    break;
            }
            if (m_BakedLightingMode != BakedLightingMode.None || m_ReflectionProbeMode != ReflectionProbeMode.None)
            {
                // https://github.cds.internal.unity3d.com/unity/unity/blob/e3e09beefeb3cbd8abd8e267dd80b22e5890968c/Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl#L416
                writer.WriteLine("float3 reflectVector = reflect(-viewDirectionWS, normalWS);");
                writer.WriteLine("float NoV = saturate(dot(normalWS, viewDirectionWS));");
                writer.WriteLine("float fresnelTerm = pow(1.0 - NoV, 4);");
                writer.WriteLine(@"
float3 environmentBrdfSpecular = lerp(brdfSpecular, grazingTerm, fresnelTerm) / (roughness2 + 1.0);");
                if (m_ReflectionProbeMode == ReflectionProbeMode.None)
                {
                    writer.WriteLine(
                        $"float3 environmentColor = {PolySpatialShaderGlobals.GlossyEnvironmentColor}.rgb;");
                }
                else
                {
                    // https://github.cds.internal.unity3d.com/unity/unity/blob/5d88a17c0c2ef08e18187e62c5d6b08fa4463ab1/Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl#L27
                    writer.WriteLine("float mip = perceptualRoughness * (10.2 - 4.2 * perceptualRoughness);");
                    
                    string GetReflectionProbeContribution(int index)
                    {
                        // Note: this only supports the dLDR encoding (which appears to be the default).
                        const float kReflectionProbeHdrMultiplier = 4.59f;
                        var textureProperty = PolySpatialShaderProperties.ReflectionProbeTexturePrefix + index;
                        return $@"
SAMPLE_TEXTURECUBE_LOD({textureProperty}, sampler{textureProperty}, reflectVector, mip).rgb *
    {kReflectionProbeHdrMultiplier}";
                    }

                    writer.Write("float3 environmentColor = ");
                    if (m_ReflectionProbeMode == ReflectionProbeMode.Simple)
                    {
                        writer.Write(GetReflectionProbeContribution(0));
                    }
                    else
                    {
                        for (var i = 0; i < PolySpatialShaderProperties.ReflectionProbeCount; ++i)
                        {
                            if (i > 0)
                                writer.Write(" + ");
                            
                            writer.Write($@"
{GetReflectionProbeContribution(i)} * {PolySpatialShaderProperties.ReflectionProbeWeightPrefix}{i}");
                        }
                    }
                    writer.WriteLine(";");
                }
                writer.Write("float3 bakedContribution = ");
                if (m_BakedLightingMode != BakedLightingMode.None)
                    writer.Write("bakedGI * brdfDiffuse + ");
                writer.WriteLine("environmentColor * environmentBrdfSpecular;");
                finalSumExpr.Append(" + bakedContribution * AmbientOcclusion");
            }
            
            if (m_DynamicLighting)
            {
                writer.WriteLine($@"
float3 positionWS = mul({PolySpatialShaderProperties.VolumeToWorld}, float4(PositionVS, 1)).xyz;");

                for (var i = 0; i < PolySpatialShaderGlobals.LightCount; ++i)
                {
                    // https://github.cds.internal.unity3d.com/unity/unity/blob/918aac026438f350a9716ff831b1e309f2483743/Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl#L154
                    var lightColor = PolySpatialShaderGlobals.LightColorPrefix + i;
                    var lightPosition = PolySpatialShaderGlobals.LightPositionPrefix + i;
                    var lightAtten = PolySpatialShaderGlobals.LightAttenPrefix + i;
                    var spotDirection = PolySpatialShaderGlobals.SpotDirectionPrefix + i;

                    writer.WriteLine($@"
float3 lightVector{i} = {lightPosition}.xyz - positionWS * {lightPosition}.w;");
                    writer.WriteLine($"float distanceSqr{i} = max(dot(lightVector{i}, lightVector{i}), {kHalfMin});");
                    writer.WriteLine($"float3 lightDirection{i} = lightVector{i} * rsqrt(distanceSqr{i});");
                    writer.WriteLine($"float factor{i} = distanceSqr{i} * {lightAtten}.x;");
                    writer.WriteLine($"float smoothFactor{i} = saturate(1.0 - factor{i} * factor{i});");
                    writer.WriteLine($@"
float distanceAtten{i} = rcp(distanceSqr{i}) * smoothFactor{i} * smoothFactor{i};");
                    writer.WriteLine($"float SdotL{i} = dot({spotDirection}.xyz, lightDirection{i});");
                    writer.WriteLine($"float angleAtten{i} = saturate(SdotL{i} * {lightAtten}.z + {lightAtten}.w);");
                    writer.WriteLine($"float atten{i} = distanceAtten{i} * angleAtten{i} * angleAtten{i};");
                    writer.WriteLine($"float NdotL{i} = saturate(dot(normalWS, lightDirection{i}));");
                    writer.WriteLine($"float3 radiance{i} = {lightColor}.rgb * (atten{i} * NdotL{i});");

                    // https://github.cds.internal.unity3d.com/unity/unity/blob/918aac026438f350a9716ff831b1e309f2483743/Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl#L179
                    writer.WriteLine($"float3 halfDir{i} = normalize(lightDirection{i} + viewDirectionWS);");
                    writer.WriteLine($"float NoH{i} = saturate(dot(normalWS, halfDir{i}));");
                    writer.WriteLine($"float LoH{i} = saturate(dot(lightDirection{i}, halfDir{i}));");
                    writer.WriteLine($"float d{i} = NoH{i} * NoH{i} * roughness2MinusOne + 1.00001f;");
                    writer.WriteLine($"float LoH2{i} = LoH{i} * LoH{i};");
                    writer.WriteLine($"float specularTerm{i} = roughness2 / ((d{i} * d{i}) * max(0.1, LoH2{i}) * normalizationTerm);");

                    finalSumExpr.Append($" + (brdfDiffuse + brdfSpecular * specularTerm{i}) * radiance{i}");
                }
            }

            writer.WriteLine($"Out = {finalSumExpr};");

            return writer.ToString();
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("PolySpatial_Lighting", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        string PolySpatial_Lighting(
            [Slot(0, Binding.MeshUV1)] Vector2 LightmapUV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 1.0f)] ColorRGB BaseColor,
            [Slot(2, Binding.TangentSpaceNormal)] Vector3 Normal,
            [Slot(3, Binding.None)] Vector1 Metallic,
            [Slot(4, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector1 Smoothness,
            [Slot(5, Binding.None)] ColorRGB Emission,
            [Slot(6, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector1 AmbientOcclusion,
            [Slot(7, Binding.WorldSpaceTangent, true)] Vector3 TangentFrameX,
            [Slot(8, Binding.WorldSpaceBitangent, true)] Vector3 TangentFrameY,
            [Slot(9, Binding.WorldSpaceNormal, true)] Vector3 TangentFrameZ,
            [Slot(10, Binding.WorldSpacePosition, true)] Vector3 PositionVS,
            [Slot(11, Binding.WorldSpaceViewDirection, true)] Vector3 ViewDirectionVS,
            [Slot(12, Binding.None, ShaderStageCapability.Fragment)] out Vector3 Out)
        {
            Out = Vector3.one;

            return
$@"
{{
    {GetFunctionBody()}
}}";
        }        
    }
}