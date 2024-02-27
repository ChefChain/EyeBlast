using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    abstract class GeometryVectorAdapter<T> : ANodeAdapter<T>
        where T : GeometryNode
    { 
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            switch (((GeometryNode)node).space)
            {
                case CoordinateSpace.View:
                    QuickNode.CompoundOp(node, graph, externals, sgContext, Hint, new()
                    {
                        ["Out"] = new(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector3, new()
                            {
                                ["in"] = new InlineInputDef(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, new()
                                {
                                    // Convert to vector4, then zero out w component to transform as vector.
                                    ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
                                    {
                                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
                                        {
                                            ["in"] = new InlineInputDef(NodeType, MtlxDataTypes.Vector3, new()
                                            {
                                                ["space"] = new StringInputDef("object"),
                                            }),
                                        }),
                                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, 1.0f, 0.0f),
                                    }),
                                    ["mat"] = new PerStageInputDef(
                                        new InlineInputDef(
                                            MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                                            MtlxDataTypes.Matrix44, new(), "modelToView"),
                                        new InlineInputDef(
                                            MtlxNodeTypes.RealityKitSurfaceModelToView,
                                            MtlxDataTypes.Matrix44, new(), "modelToView")),
                                }),
                            }),
                        }),
                    });
                    break;

                case CoordinateSpace.Tangent:
                    // Tangent space vectors don't need to be flipped.
                    QuickNode.CompoundOp(node, graph, externals, sgContext, Hint, new()
                    {
                        ["Out"] = new(NodeType, MtlxDataTypes.Vector3, new()
                        {
                            ["space"] = new StringInputDef("tangent"),
                        }),
                    });
                    break;

                case var space:
                    QuickNode.CompoundOp(node, graph, externals, sgContext, Hint, new()
                    {
                        // Flip z coordinate to convert RealityKit space to Unity space.
                        ["Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new InlineInputDef(NodeType, MtlxDataTypes.Vector3, new()
                            {
                                ["space"] = new StringInputDef(PositionAdapter.SpaceToMtlxString(space)),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, new[] { 1.0f, 1.0f, -1.0f }),
                        }),
                    });
                    break;
            }            
        }

        protected abstract string Hint { get; }
        protected abstract string NodeType { get; }
    }
}