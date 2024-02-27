using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CustomInterpolatorAdapter : ANodeAdapter<CustomInterpolatorNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        public override string SupportDetails(AbstractMaterialNode node)
        {
            if (node is not CustomInterpolatorNode customInterpolatorNode)
                return "";

            switch (customInterpolatorNode.customBlockNodeName)
            {
                case "Color":
                case "UV0":
                case "UV1":
                case "UserAttribute":
                    return "";

                default:
                    return $"Custom interpolator '{customInterpolatorNode.customBlockNodeName}' not supported.";
            }
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is not CustomInterpolatorNode customInterpolatorNode)
                return;

            switch (customInterpolatorNode.customBlockNodeName)
            {
                case "Color":
                    QuickNode.NaryOp(
                        MtlxNodeTypes.GeomColor, node, graph, externals, "Color", null, null, MtlxDataTypes.Color4);
                    break;

                case "UV0":
                {
                    var nodeData = QuickNode.CreateUVNode(graph, NodeUtils.GetNodeName(node, "UV0"), 0);
                    externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, nodeData.name);
                    break;
                }
                case "UV1":
                {
                    var nodeData = QuickNode.CreateUVNode(graph, NodeUtils.GetNodeName(node, "UV1"), 1);
                    externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, nodeData.name);
                    break;
                }
                case "UserAttribute":
                {
                    var nodeData = QuickNode.NaryOp(
                        MtlxNodeTypes.RealityKitSurfaceCustomAttribute, node, graph, externals,
                        "UserAttribute", null, null, MtlxDataTypes.Vector4);
                    nodeData.outputName = "customAttribute";
                    break;
                }
            }
        }
    }
}
