
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class KeywordAdapter : ANodeAdapter<KeywordNode>
    {
        internal const string k_MaterialXKeywordReferenceName = "MATERIAL_X";

        [BuiltinKeyword]
        static KeywordDescriptor MaterialXKeyword()
        {
            return new KeywordDescriptor()
            {
                displayName = "MaterialX",
                referenceName = k_MaterialXKeywordReferenceName,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Global,
                value = 0,
                entries = new KeywordEntry[0],
                stages = KeywordShaderStage.All,
            };
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is not KeywordNode knode)
                return;

            var keywordName = knode.keyword.referenceName;
            var hint = $"Keyword{keywordName}";
            if (keywordName == k_MaterialXKeywordReferenceName)
            {
                // The MaterialX keyword is always "On" when we export to MaterialX.
                QuickNode.UnaryOp(MtlxNodeTypes.Dot, node, graph, externals, hint, "in", "", "On");
                return;
            }

            if (knode.keyword.keywordType == KeywordType.Boolean)
            {
                QuickNode.EnsureImplicitProperty(keywordName, MtlxDataTypes.Boolean, graph);

                var nodeData = QuickNode.BinaryOp(MtlxNodeTypes.IfEqual, node, graph, externals, hint, "On");
                graph.AddPortAndEdge(keywordName, nodeData.name, "value1", MtlxDataTypes.Boolean);
                nodeData.AddPortValue("value2", MtlxDataTypes.Boolean, new[] { 1.0f });
            }
            else
            {
                var outputType = NodeUtils.GetDataTypeName(node);

                Dictionary<string, InputDef> GetInputDefs(int index)
                {
                    var entry = knode.keyword.entries[index];
                    return new()
                    {
                        ["value1"] = new ImplicitInputDef(
                            $"{keywordName}_{entry.referenceName}", MtlxDataTypes.Boolean),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Boolean, 1.0f),
                        ["in1"] = new ExternalInputDef(entry.displayName),
                        ["in2"] = (index == knode.keyword.entries.Count - 2) ?
                            new ExternalInputDef(knode.keyword.entries[index + 1].displayName) :
                            new InlineInputDef(MtlxNodeTypes.IfEqual, outputType, GetInputDefs(index + 1)),
                    };
                }

                QuickNode.CompoundOp(node, graph, externals, sgContext, hint, new()
                {
                    ["Out"] = new(MtlxNodeTypes.IfEqual, outputType, GetInputDefs(0)),
                });
            }
        }
    }
}
