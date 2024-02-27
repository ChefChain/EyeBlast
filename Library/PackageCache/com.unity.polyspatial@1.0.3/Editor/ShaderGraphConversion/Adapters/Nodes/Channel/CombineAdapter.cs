
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CombineAdapter : ANodeAdapter<CombineNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Combine-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Combine",
                "RGBA = float4(R, G, B, A); RGB = float3(R, G, B); RG = float2(R, G);");
        }
    }
}
