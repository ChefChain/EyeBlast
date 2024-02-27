
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PositionAdapter : ANodeAdapter<PositionNode>
    {
        static internal string SpaceToMtlxString(Internal.CoordinateSpace space) => space switch
        {
            CoordinateSpace.AbsoluteWorld => "world",
            CoordinateSpace.World => "world",
            CoordinateSpace.Object => "object",
            CoordinateSpace.Tangent => "tangent",
            CoordinateSpace.View => "view",
            CoordinateSpace.Screen => "screen",
            _ => ""
        };

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Position", new()
            {
                // Convert to vector4 with w = 1
                ["Out"] = new(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = ((GeometryNode)node).space switch
                    {
                        CoordinateSpace.View => new InlineInputDef(
                            MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3, new()
                            {
                                ["space"] = new StringInputDef("object"),
                            }),
                            ["mat"] = new PerStageInputDef(
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                                    MtlxDataTypes.Matrix44, new(), "modelToView"),
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitSurfaceModelToView,
                                    MtlxDataTypes.Matrix44, new(), "modelToView")),
                        }),

                        // Flip z coordinate to convert RealityKit space to Unity space.
                        var space => new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3, new()
                            {
                                ["space"] = new StringInputDef(SpaceToMtlxString(space)),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, new[] { 1.0f, 1.0f, -1.0f }),
                        }),
                    },
                }),
            });
        }
    }
}
