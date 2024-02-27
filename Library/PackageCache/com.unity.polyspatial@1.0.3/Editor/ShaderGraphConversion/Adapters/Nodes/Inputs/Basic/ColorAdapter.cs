using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class ColorAdapter : ANodeAdapter<ColorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ColorNode cnode)
            {
                // Color4s are clamped to [0, 1], so use Vector4s for colors that may exceed that range.
                var nodeType = (cnode.color.mode == ColorMode.HDR) ? MtlxDataTypes.Vector4 : MtlxDataTypes.Color4;
                var nodeData = QuickNode.NaryOp(
                    MtlxNodeTypes.Constant, node, graph, externals, "Color", outputType: nodeType);

                // RealityKit expects color constants in linear color space.
                var c = cnode.color.color.linear;
                var value = new float[] { c.r, c.g, c.b, c.a };

                nodeData.AddPortValue("value", nodeType, value);
            }
        }
    }
}
