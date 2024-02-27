
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MainLightDirectionAdapter : ANodeAdapter<MainLightDirectionNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.LightPositionPrefix + 0, MtlxDataTypes.Vector4, node,
                graph, externals, "Direction", MtlxDataTypes.Vector3, "xyz");
        }
    }
}
