using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using System.Linq;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleGradientAdapter : ANodeAdapter<SampleGradient>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var gradientSlot = NodeUtils.GetSlotByName(node, "Gradient");
            var (inputSlot, outputSlot) = SlotUtils.GetPropertyRedirectedInputOutputSlots(gradientSlot, sgContext);
            var gradient = outputSlot?.owner switch
            {
                PropertyNode propertyNode => ((GradientShaderProperty)propertyNode.property).value,
                GradientNode gradientNode => gradientNode.gradient,
                _ => ((GradientInputMaterialSlot)inputSlot).value,
            };
            var colorKeys = gradient.colorKeys;
            var alphaKeys = gradient.alphaKeys;

            InputDef GetColorDef(int index)
            {
                var key = colorKeys[index];
                var color = key.color;
                var valueDef = new FloatInputDef(MtlxDataTypes.Color3, color.r, color.g, color.b);
                if (index == 0)
                    return valueDef;

                if (gradient.mode == GradientMode.Fixed)
                {
                    // color = lerp(color, Gradient.colors[c].rgb, step(Gradient.colors[c-1].w, Time));
                    return new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Color3, new()
                    {
                        ["value1"] = new ExternalInputDef("Time"),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, colorKeys[index - 1].time),
                        ["in1"] = valueDef,
                        ["in2"] = GetColorDef(index - 1),
                    });
                }

                // color = lerp(color, Gradient.colors[c].rgb, colorPos);
                return new InlineInputDef(MtlxNodeTypes.Mix, MtlxDataTypes.Color3, new()
                {
                    ["fg"] = valueDef,
                    ["bg"] = GetColorDef(index - 1),
                    // float colorPos = saturate((Time - Gradient.colors[c-1].w) /
                    //     (Gradient.colors[c].w - Gradient.colors[c-1].w));
                    ["mix"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new ExternalInputDef("Time"),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, colorKeys[index - 1].time),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, key.time - colorKeys[index - 1].time),
                        }),
                    }),
                });
            }

            InputDef GetAlphaDef(int index)
            {
                var key = alphaKeys[index];
                var valueDef = new FloatInputDef(MtlxDataTypes.Float, key.alpha);
                if (index == 0)
                    return valueDef;

                if (gradient.mode == GradientMode.Fixed)
                {
                    // alpha = lerp(alpha, Gradient.alphas[a].x, step(Gradient.alphas[a-1].y, Time));
                    return new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Float, new()
                    {
                        ["value1"] = new ExternalInputDef("Time"),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, alphaKeys[index - 1].time),
                        ["in1"] = valueDef,
                        ["in2"] = GetAlphaDef(index - 1),
                    });
                }

                // alpha = lerp(alpha, Gradient.alphas[a].x, alphaPos);
                return new InlineInputDef(MtlxNodeTypes.Mix, MtlxDataTypes.Float, new()
                {
                    ["fg"] = valueDef,
                    ["bg"] = GetAlphaDef(index - 1),
                    // float alphaPos = saturate((Time - Gradient.alphas[a-1].y) /
                    //     (Gradient.alphas[a].y - Gradient.alphas[a-1].y));
                    ["mix"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new ExternalInputDef("Time"),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, alphaKeys[index - 1].time),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, key.time - alphaKeys[index - 1].time),
                        }),
                    }),
                });
            }

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Sample-Gradient-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "SampleGradient", new()
            {
                ["Out"] = new(MtlxNodeTypes.Combine2, MtlxDataTypes.Color4, new()
                {
                    ["in1"] = GetColorDef(colorKeys.Length - 1),
                    ["in2"] = GetAlphaDef(alphaKeys.Length - 1),
                }),
            });
        }
    }
}
