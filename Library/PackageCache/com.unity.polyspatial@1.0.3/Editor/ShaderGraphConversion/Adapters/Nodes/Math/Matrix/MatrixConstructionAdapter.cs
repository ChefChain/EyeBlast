using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MatrixConstructionAdapter : ANodeAdapter<MatrixConstructionNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            NodeDef matrix22 = new(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("M0"),
                    ["channels"] = new StringInputDef("xy"),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("M1"),
                    ["channels"] = new StringInputDef("xy"),
                }),
            });
            NodeDef matrix33 = new(MtlxNodeTypes.RealityKitCombine3, MtlxDataTypes.Matrix33, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new ExternalInputDef("M0"),
                    ["channels"] = new StringInputDef("xyz"),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new ExternalInputDef("M1"),
                    ["channels"] = new StringInputDef("xyz"),
                }),
                ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new ExternalInputDef("M2"),
                    ["channels"] = new StringInputDef("xyz"),
                }),
            });
            NodeDef matrix44 = new(MtlxNodeTypes.RealityKitCombine4, MtlxDataTypes.Matrix44, new()
            {
                ["in1"] = new ExternalInputDef("M0"),
                ["in2"] = new ExternalInputDef("M1"),
                ["in3"] = new ExternalInputDef("M2"),
                ["in4"] = new ExternalInputDef("M3"),
            });
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "MatrixConstruction", MatrixSplitAdapter.GetAxis(node) switch
            {
                MatrixAxis.Row => new()
                {
                    ["2x2"] = new(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix22, new()
                    {
                        ["in"] = new InlineInputDef(matrix22),
                    }),
                    ["3x3"] = new(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix33, new()
                    {
                        ["in"] = new InlineInputDef(matrix33),
                    }),
                    ["4x4"] = new(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix44, new()
                    {
                        ["in"] = new InlineInputDef(matrix44),
                    }),
                },
                MatrixAxis.Column => new()
                {
                    ["2x2"] = matrix22,
                    ["3x3"] = matrix33,
                    ["4x4"] = matrix44,
                },
                var axis => throw new NotSupportedException($"Unknown axis {axis}"),
            });
        }
    }
}
