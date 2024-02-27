using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class DielectricSpecularAdapter : ANodeAdapter<DielectricSpecularNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var material = ((DielectricSpecularNode)node).material;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Dielectric-Specular-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "DielectricSpecular", new()
            {
                ["Out"] = new(MtlxNodeTypes.Constant, MtlxDataTypes.Float, new()
                {
                    ["value"] = new FloatInputDef(MtlxDataTypes.Float, material.type switch
                    {
                        DielectricMaterialType.Common => Mathf.Lerp(0.034f, 0.048f, material.range),
                        DielectricMaterialType.RustedMetal => 0.030f,
                        DielectricMaterialType.Water => 0.020f,
                        DielectricMaterialType.Ice => 0.018f,
                        DielectricMaterialType.Glass => 0.040f,
                        DielectricMaterialType.Custom => Mathf.Pow(material.indexOfRefraction - 1.0f, 2.0f) /
                            Mathf.Pow(material.indexOfRefraction + 1.0f, 2.0f),
                        _ => throw new NotSupportedException($"Unknown dielectric material type: {material.type}"),
                    }),
                }),
            });
        }
    }
}