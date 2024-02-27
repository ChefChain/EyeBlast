
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class BranchAdapter : ANodeAdapter<BranchNode>
    {
        // We need to simulate a Predicate != 0 to handle correct behavior.
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();

            // setup the branching values as per normal, nothing special here.
            portMap.Add("True", "in1");
            portMap.Add("False", "in2");
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.IfGreater, node, graph, externals, "Branch", portMap);

            // get our predicate slot information
            var predicateSlot = NodeUtils.GetSlotByName(node, "Predicate");
            var predicateValue = SlotUtils.GetDefaultValue(predicateSlot);

            // setup our absolute value node, so that we can do `abs(predicate) > 0` because `Predicate != 0` isn't supported.
            var absNodeData = graph.AddNode(NodeUtils.GetNodeName(node, "BranchAbs"), MtlxNodeTypes.Absolute, MtlxDataTypes.Float);
            absNodeData.AddPortValue("in", MtlxDataTypes.Float, predicateValue);

            // sanitized predicate forms endogenous input to the IfGreater node
            graph.AddPortAndEdge(absNodeData.name, nodeData.name, "value1", MtlxDataTypes.Float);

            // then we promote the absolute value node as the predicate input for this adapter.
            externals.AddExternalPortAndEdge(predicateSlot, absNodeData.name, "in");
        }
    }
}
