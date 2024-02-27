namespace UnityEditor.ShaderGraph.MaterialX
{
    class HueAdapter : ANodeAdapter<HueNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Hue-Node.html
            var hueNode = (HueNode)node;
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Hue", $@"
float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
float D = Q.x - min(Q.w, Q.y);
float E = 1e-10;
float hue = abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)) + {(hueNode.hueMode == HueMode.Degrees ? "Offset / 360" : "Offset")};
float3 hsv = float3(hue + step(hue, 0.0) - step(1.0, hue), D / (Q.x + E), Q.x);

float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);");
        }
    }
}
