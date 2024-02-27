
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RemapAdapter : ANodeAdapter<RemapNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // This is complicated because USG remap takes in two vec2s for min/max pairs, instead of 4 separate floats.
            // We first need to swizzle out the appropriate components before we can feed them into the Mtlx equivalent.
            var nodeName = NodeUtils.GetNodeName(node, "Remap");

            // We are going to swizzle out each component, so we'll 'dot' the inputs for internal reuse.
            var dotIn = graph.AddNode($"{nodeName}InMinMax", MtlxNodeTypes.Dot, MtlxDataTypes.Vector2);
            var dotOut = graph.AddNode($"{nodeName}OutMinMax", MtlxNodeTypes.Dot, MtlxDataTypes.Vector2);

            var inSlot = NodeUtils.GetSlotByName(node, "In Min Max");
            var outSlot = NodeUtils.GetSlotByName(node, "Out Min Max");

            dotIn.AddPortValue("in", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(inSlot));
            dotOut.AddPortValue("in", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(outSlot));

            externals.AddExternalPortAndEdge(inSlot, dotIn.name, "in");
            externals.AddExternalPortAndEdge(outSlot, dotOut.name, "in");

            // Setup the internal swizzles-- NOTE: should have used `extract`
            var inMin = graph.AddNode($"{nodeName}InMin", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            var inMax = graph.AddNode($"{nodeName}InMax", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            var outMin = graph.AddNode($"{nodeName}OutMin", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            var outMax = graph.AddNode($"{nodeName}OutMax", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);

            graph.AddPortAndEdge(dotIn.name, inMin.name, "in", MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(dotIn.name, inMax.name, "in", MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(dotOut.name, outMin.name, "in", MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(dotOut.name, outMax.name, "in", MtlxDataTypes.Vector2);

            inMin.AddPortString("channels", MtlxDataTypes.String, "x");
            inMax.AddPortString("channels", MtlxDataTypes.String, "y");
            outMin.AddPortString("channels", MtlxDataTypes.String, "x");
            outMax.AddPortString("channels", MtlxDataTypes.String, "y");

            // and the actual remap node.
            var nodeType = NodeUtils.GetDataTypeName(node);
            var nodeData = graph.AddNode(nodeName, MtlxNodeTypes.Remap, nodeType);

            var inputSlot = NodeUtils.GetSlotByName(node, "In");
            var outputSlot = NodeUtils.GetPrimaryOutput(node);

            graph.AddPortAndEdge(inMin.name, nodeData.name, "inlow", MtlxDataTypes.Float);
            graph.AddPortAndEdge(inMax.name, nodeData.name, "inhigh", MtlxDataTypes.Float);
            graph.AddPortAndEdge(outMin.name, nodeData.name, "outlow", MtlxDataTypes.Float);
            graph.AddPortAndEdge(outMax.name, nodeData.name, "outhigh", MtlxDataTypes.Float);
            nodeData.AddPortValue("in", nodeType, SlotUtils.GetDefaultValue(inputSlot));

            externals.AddExternalPortAndEdge(inputSlot, nodeName, "in");
            externals.AddExternalPort(outputSlot.slotReference, nodeData.name);
        }
    }
}





