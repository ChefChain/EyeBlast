using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RotateAdapter : AbstractUVNodeAdapter<RotateNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Rotate-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Rotate", new()
            {
                // Add back the center.
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Rotate2d, MtlxDataTypes.Vector2, new()
                    {
                        // Subtract the center before rotating.
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new ExternalInputDef("UV"),
                            ["in2"] = new ExternalInputDef("Center"),
                        }),
                        // Negate rotation to match Unity's convention.
                        ["amount"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("Rotation"),
                            ["in2"] = ((RotateNode)node).unit switch
                            {
                                RotationUnit.Radians => new FloatInputDef(MtlxDataTypes.Float, -Mathf.Rad2Deg),
                                RotationUnit.Degrees => new FloatInputDef(MtlxDataTypes.Float, -1.0f),
                                var unit => throw new NotSupportedException($"Unrecognized rotation unit: {unit}"),
                            },
                        }),
                    }),
                    ["in2"] = new ExternalInputDef("Center"),
                }),
            });
        }
    }
}
