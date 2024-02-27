
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ObjectAdapter : ANodeAdapter<ObjectNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectPosition, MtlxDataTypes.Vector3, node, graph, externals, "Position");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectScale, MtlxDataTypes.Vector3, node, graph, externals, "Scale");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsMin, MtlxDataTypes.Vector3, node, graph, externals, "World Bounds Min");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsMax, MtlxDataTypes.Vector3, node, graph, externals, "World Bounds Max");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsSize, MtlxDataTypes.Vector3, node, graph, externals, "Bounds Size");
        }
    }
}
