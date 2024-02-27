using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ReflectionAdapter : ANodeAdapter<ReflectionNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            Dictionary<string, string> portMap = new();
            portMap["In"] = "in";
            portMap["Normal"] = "normal";
            Dictionary<string, string> portTypes = new();
            portTypes["In"] = MtlxDataTypes.Vector3;
            portTypes["Normal"] = MtlxDataTypes.Vector3;
            QuickNode.NaryOp(
                MtlxNodeTypes.RealityKitReflect, node, graph, externals,
                "Reflection", portMap, portTypes, MtlxDataTypes.Vector3);
        }
    }
}