
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ScreenAdapter : ANodeAdapter<ScreenNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ScreenParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Width", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ScreenParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Height", MtlxDataTypes.Float, "y");
        }
    }
}
