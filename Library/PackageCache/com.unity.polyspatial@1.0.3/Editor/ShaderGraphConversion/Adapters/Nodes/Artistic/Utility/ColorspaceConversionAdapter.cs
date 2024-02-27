using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ColorspaceConversionAdapter : ANodeAdapter<ColorspaceConversionNode>
    {
        static Exception CreateUnknownColorspaceException(Colorspace colorspace)
        {
            return new NotSupportedException($"Unknown colorspace {colorspace}");
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Colorspace-Conversion-Node.html
            // (but we don't have vector comparisons, so we use a step function as an input to lerp instead)
            var conversion = ((ColorspaceConversionNode)node).conversion;
            string expr = (conversion.from == conversion.to) ? "Out = In;" : conversion.from switch
            {
                Colorspace.RGB => conversion.to switch
                {
                    Colorspace.Linear => @"
float3 linearRGBLo = In / 12.92;
float3 linearRGBHi = pow(max(abs((In + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
Out = lerp(linearRGBLo, linearRGBHi, step(0.04045, In));",
                    Colorspace.HSV => @"
float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
float D = Q.x - min(Q.w, Q.y);
float E = 1e-10;
float V = (D == 0) ? Q.x : (Q.x + E);
Out = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);",
                    _ => throw CreateUnknownColorspaceException(conversion.to),
                },
                Colorspace.Linear => conversion.to switch
                {
                    Colorspace.RGB => @"
float3 sRGBLo = In * 12.92;
float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
Out = lerp(sRGBLo, sRGBHi, step(0.0031308, In));",
                    Colorspace.HSV => @"
float3 sRGBLo = In * 12.92;
float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
float3 Linear = lerp(sRGBLo, sRGBHi, step(0.0031308, In));
float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
float4 P = lerp(float4(Linear.bg, K.wz), float4(Linear.gb, K.xy), step(Linear.b, Linear.g));
float4 Q = lerp(float4(P.xyw, Linear.r), float4(Linear.r, P.yzx), step(P.x, Linear.r));
float D = Q.x - min(Q.w, Q.y);
float  E = 1e-10;
Out = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);",
                    _ => throw CreateUnknownColorspaceException(conversion.to),
                },
                Colorspace.HSV => conversion.to switch
                {
                    Colorspace.RGB => @"
float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
Out = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);",
                    Colorspace.Linear => @"
float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
float3 RGB = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
float3 linearRGBLo = RGB / 12.92;
float3 linearRGBHi = pow(max(abs((RGB + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
Out = lerp(linearRGBLo, linearRGBHi, step(0.04045, RGB));",
                    _ => throw CreateUnknownColorspaceException(conversion.to),
                },
                _ => throw CreateUnknownColorspaceException(conversion.from),
            };
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ColorspaceConversion", expr);
        }
    }
}
