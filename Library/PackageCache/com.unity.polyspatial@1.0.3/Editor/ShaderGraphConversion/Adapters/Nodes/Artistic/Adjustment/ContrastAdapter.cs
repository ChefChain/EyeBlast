using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ContrastAdapter : ANodeAdapter<ContrastNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Contrast-Node.html
            var midpoint = Mathf.Pow(0.5f, 2.2f);
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Contrast", $"Out = (In - {midpoint}) * Contrast + {midpoint};");
        }
    }
}
