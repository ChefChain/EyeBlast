using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RadiansToDegreesAdapter : ANodeAdapter<RadiansToDegreesNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "RadiansToDegrees", "Out = degrees(In);");
        }
    }
}