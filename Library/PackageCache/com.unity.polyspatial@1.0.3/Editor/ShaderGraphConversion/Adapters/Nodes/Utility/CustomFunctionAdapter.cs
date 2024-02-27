using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class CustomFunctionAdapter : ANodeAdapter<CustomFunctionNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
            return ((CustomFunctionNode)node).sourceType == HlslSourceType.String;
        }

        public override string SupportDetails(AbstractMaterialNode node)
        {
            var customFunctionNode = (CustomFunctionNode)node;
            if (customFunctionNode.sourceType != HlslSourceType.String)
                return "Only string functions are supported.";
            
            // Parse the expression just to see if it throws an exception.
            try
            {
                CompoundOpParser.Parse(node, null, customFunctionNode.functionBody);
            }
            catch (CompoundOpParser.ParseException e)
            {
                return e.ShortMessage;
            }

            return "";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var customFunctionNode = (CustomFunctionNode)node;
            if (customFunctionNode.sourceType != HlslSourceType.String)
                return;
            
            try
            {
                QuickNode.CompoundOp(
                    node, graph, externals, sgContext,
                    customFunctionNode.functionName, customFunctionNode.functionBody);
            }
            catch (CompoundOpParser.ParseException e)
            {
                MtlxPostProcessor.LogWarningForGraph(node.owner, $"Couldn't parse custom function: {e}");
            }
        }
    }
}