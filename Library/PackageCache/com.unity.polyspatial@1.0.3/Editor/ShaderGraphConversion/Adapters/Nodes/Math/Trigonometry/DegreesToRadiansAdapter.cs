using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class DegreesToRadiansAdapter : ANodeAdapter<DegreesToRadiansNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "DegreesToRadians", "Out = radians(In);");
        }
    }
}