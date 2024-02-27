using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RoundedRectangleAdapter : AbstractUVNodeAdapter<RoundedRectangleNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Rounded-Rectangle-Node.html
            // (but MaterialX doesn't support fwidth, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "RoundedRectangle", @"
float clampedRadius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
float2 uv = abs(UV * 2 - 1) - float2(Width, Height) + clampedRadius;
float d = length(max(0, uv)) / clampedRadius;
Out = splitlr(1, 0, 1, float2(d, 0));");
        }
    }
}