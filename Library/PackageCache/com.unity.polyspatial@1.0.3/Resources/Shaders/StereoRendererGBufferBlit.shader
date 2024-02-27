// Used by the StereoRendererFeature to blit the world position
// to a lower resolution "GPass" for VisionOS to use. A square blur
// is applied to try and "bleed" the z values of objects past their
// edges to reduce artifacting at the egde when the pass is composited
// into VisionOS with a vertex-displaced grid.
Shader "Hidden/PolySpatial/StereoRendererGbufferBlit"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _GBufferDimensions;
            float4x4 _InvViewProj;

            inline bool IsObject(float sample)
            {
            #if UNITY_REVERSED_Z
                return sample > .01;
            #else
                return sample <.99;
            #endif
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Force override stereo eye index for DeclareOpaqueTexture.hlsl to use.
                // In singlepass we blit these from a Texture2DArray into a Texture2D via two separate
                // blit commands because RK DrawableQueue does not take Texture2DArrays.
                // Depth pass must first be resolved to not have msaa samples. Easiest to
                // override stereoEyeIndex and rely on DeclareDepthTexture.hlsl
                // as that's what the DepthTexture option and _CameraDepthTexture do.
                unity_StereoEyeIndex = _BlitTexArraySlice;

                // Sample each neighboring vertex depth and if there is depth
                // average it with the other samples.
                const float2 uvOffsetA = 1.0 / _GBufferDimensions.xy;
                const float2 uvOffsetB = 2.0 / _GBufferDimensions.xy;
                const int sampleOffsetCount = 8;
                const float2 sampleOffsets[sampleOffsetCount] =  {
                    float2(-1, 1),
                    float2(0, 1),
                    float2(1, 1),
                    float2(1, 0),
                    float2(1, -1),
                    float2(0, -1),
                    float2(-1, -1),
                    float2(-1, 0),
                };
                const float2 uv = input.texcoord;
                const float centerDepth = SampleSceneDepth(uv);
                const bool centerIsObject = IsObject(centerDepth);
                int cumulativeSampleCount = centerIsObject ? 1 : 0;
                float cumulativeDepth = centerIsObject ? centerDepth : 0;
                UNITY_UNROLL
                for (int i = 0; i < sampleOffsetCount; ++i)
                {
                    const float depthA = SampleSceneDepth(uv + sampleOffsets[i] * uvOffsetA);
                    const bool aIsObject = IsObject(depthA);
                    cumulativeDepth += aIsObject ? depthA : 0;
                    cumulativeSampleCount += aIsObject ? 1 : 0;

                    const float depthB = SampleSceneDepth(uv + sampleOffsets[i] * uvOffsetB);
                    const bool bIsObject = IsObject(depthB);
                    cumulativeDepth += bIsObject ? depthB : 0;
                    cumulativeSampleCount += bIsObject ? 1 : 0;
                }
                const float averagedDepth = cumulativeSampleCount > 0 ? cumulativeDepth / (float)cumulativeSampleCount : centerDepth;
                const float3 worldPos = ComputeWorldSpacePosition(uv, averagedDepth, _InvViewProj);
                return float4(worldPos, 1);
            }
            ENDHLSL
        }
    }
}
