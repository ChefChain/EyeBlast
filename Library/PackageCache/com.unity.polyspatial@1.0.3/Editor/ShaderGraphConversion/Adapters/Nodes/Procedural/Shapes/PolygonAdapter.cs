using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolygonAdapter : AbstractUVNodeAdapter<PolygonNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Polygon-Node.html
            // (but MaterialX doesn't support fwidth, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Polygon", $@"
float2 uv = (UV * 2 - 1) / (float2(Width, -Height) * cos({Mathf.PI} / Sides));
float pCoord = atan2(uv.x, uv.y);
float r = {Mathf.PI * 2.0f} / Sides;
float distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
Out = splitlr(1, 0, 1, float2(distance, 0));");
        }
    }
}