Shader "Hidden/PolySpatial/StereoRendererBlit"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilinear
            #pragma multi_compile _ USE_TEXTURE2D_X_AS_ARRAY
            #pragma multi_compile _ BLIT_SINGLE_SLICE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            ENDHLSL
        }
    }
}
