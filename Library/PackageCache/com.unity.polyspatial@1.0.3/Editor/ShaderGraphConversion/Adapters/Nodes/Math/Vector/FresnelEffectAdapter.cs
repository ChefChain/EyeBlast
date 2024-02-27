using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class FresnelEffectAdapter : ANodeAdapter<FresnelNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Fresnel-Effect-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "FresnelEffect",
                "Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);");
        }
    }
}