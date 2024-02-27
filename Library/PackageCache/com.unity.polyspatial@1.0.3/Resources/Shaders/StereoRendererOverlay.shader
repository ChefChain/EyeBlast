Shader "Hidden/PolySpatial/StereoRendererOverlay"
{
    Properties
    {
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"}

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _LeftColorFramebuffer;
            sampler2D _LeftGBufferFramebuffer;
            sampler2D _RightColorFramebuffer;
            sampler2D _RightGBufferFramebuffer;
            float4x4 _LeftInvViewProj;
            float4x4 _RightInvViewProj;

            inline float4x4 invViewProj()
            {
                return unity_StereoEyeIndex == 0 ? _LeftInvViewProj : _RightInvViewProj;
            }

            inline float4 sampleGBuffer(float2 uv)
            {
                float4 leftDepth = tex2Dlod(_LeftGBufferFramebuffer, float4(uv,0,0));
                float4 rightDepth = tex2Dlod(_RightGBufferFramebuffer, float4(uv,0,0));
                return unity_StereoEyeIndex == 0 ? leftDepth : rightDepth;
            }

            float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
            {
                float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    positionCS.y = -positionCS.y;
                #endif
                float4 hpositionWS = mul(invViewProjMatrix, positionCS);
                return hpositionWS.xyz / hpositionWS.w;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 gBuffer = sampleGBuffer(v.uv);

                // Optionally this is how you would compute it off
                // depth buffer and invViewProj matrix, but due to
                // technical reasons RK does not utilize this well
                // so currently it relies on worldPos in GBuffer
                // float depth = gBuffer.r;
                // float3 worldPos = ComputeWorldSpacePosition(v.uv, depth, invViewProj());
                // o.vertex =  mul(UNITY_MATRIX_VP, float4(worldPos, 1));

                float3 worldPos = gBuffer.xyz;
                o.vertex =  mul(UNITY_MATRIX_VP, float4(worldPos, 1));

                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 leftColor = tex2D(_LeftColorFramebuffer, i.uv);
                float4 rightColor = tex2D(_RightColorFramebuffer, i.uv);
                return unity_StereoEyeIndex == 0 ? leftColor : rightColor;
            }
            ENDCG
        }
    }
}
