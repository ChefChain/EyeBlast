using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalBlendAdapter : ANodeAdapter<NormalBlendNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (!(node is NormalBlendNode normalBlendNode))
                return;
            
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Blend-Node.html
            switch (normalBlendNode.blendMode)
            {
                case NormalBlendMode.Default:
                    QuickNode.CompoundOp(
                        node, graph, externals, sgContext, "NormalBlend",
                        "Out = normalize(float3(A.rg + B.rg, A.b * B.b));");
                    break;
                
                case NormalBlendMode.Reoriented:
                    QuickNode.CompoundOp(
                        node, graph, externals, sgContext, "NormalBlend", @"
float3 t = A.xyz + float3(0.0, 0.0, 1.0);
float3 u = B.xyz * float3(-1.0, -1.0, 1.0);
Out = (t / t.z) * dot(t, u) - u;");
                    break;
                
                default:
                    throw new NotSupportedException($"Unrecognized blend mode: {normalBlendNode.blendMode}");
            }
        }
    }
}