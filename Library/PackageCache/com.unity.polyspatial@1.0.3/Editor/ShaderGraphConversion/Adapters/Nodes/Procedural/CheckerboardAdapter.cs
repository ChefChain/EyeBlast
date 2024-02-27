using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CheckerboardAdapter : AbstractUVNodeAdapter<CheckerboardNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Checkerboard-Node.html
            // (but MaterialX doesn't support ddx/ddy, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Checkerboard", @"
float2 fracUV = frac((UV + 0.5) * Frequency);
float horizontalValue = splitlr(0, 1, 0.5, fracUV);
float verticalValue = splitlr(0, 1, 0.5, fracUV.yx);
float xorValue = horizontalValue + verticalValue - 2 * horizontalValue * verticalValue;
Out = lerp(ColorB, ColorA, xorValue);");
        }
    }
}