
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class StepAdapter : ANodeAdapter<StepNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {

            var dataType = NodeUtils.GetDataTypeName(node);

            // early out on single float input.
            if (dataType == MtlxDataTypes.Float)
            {
                var portMap = new Dictionary<string, string>();
                portMap.Add("Edge", "value2");
                portMap.Add("In", "value1");
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.IfGreaterOrEqual, node, graph, externals, "Step", portMap);
                nodeData.AddPortValue("in1", MtlxDataTypes.Float, new float[] { 1 });
                nodeData.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 0 });
                return;
            }

            var length = MtlxDataTypes.GetLength(dataType);
            var outSlot = NodeUtils.GetPrimaryOutput(node);
            var edgeSlot = NodeUtils.GetSlotByName(node, "Edge");
            var inSlot = NodeUtils.GetSlotByName(node, "In");

            var inNode = QuickNode.UnaryOp(MtlxNodeTypes.Dot, node, graph, null, "StepIn", usgSlotName: "In");
            var edgeNode = QuickNode.UnaryOp(MtlxNodeTypes.Dot, node, graph, null, "StepEdge", usgSlotName: "Edge");
            var outputNode = QuickNode.NaryOp($"combine{length}", node, graph, null, "StepOut");

            externals.AddExternalPortAndEdge(edgeSlot, edgeNode.name, "in");
            externals.AddExternalPortAndEdge(inSlot, inNode.name, "in");
            externals.AddExternalPort(outSlot.slotReference, outputNode.name);

            for (int i = 0; i < length; ++i)
            {
                NodeUtils.GetNodeName(node, $"StepIn{i}");

                var edgeElement = graph.AddNode(NodeUtils.GetNodeName(node, $"StepEdge{i}"), MtlxNodeTypes.Extract, MtlxDataTypes.Float);
                var inElement = graph.AddNode(NodeUtils.GetNodeName(node, $"StepIn{i}"), MtlxNodeTypes.Extract, MtlxDataTypes.Float);
                graph.AddPortAndEdge(edgeNode.name, edgeElement.name, "in", dataType);
                graph.AddPortAndEdge(inNode.name, inElement.name, "in", dataType);
                edgeElement.AddPortValue("index", MtlxDataTypes.Integer, new float[] { i });
                inElement.AddPortValue("index", MtlxDataTypes.Integer, new float[] { i });

                var compNode = graph.AddNode(NodeUtils.GetNodeName(node, $"StepComp{i}"), MtlxNodeTypes.IfGreaterOrEqual, MtlxDataTypes.Float);
                graph.AddPortAndEdge(inElement.name, compNode.name, "value1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(edgeElement.name, compNode.name, "value2", MtlxDataTypes.Float);
                compNode.AddPortValue("in1", MtlxDataTypes.Float, new float[] { 1.0f });
                compNode.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 0.0f });

                graph.AddPortAndEdge(compNode.name, outputNode.name, $"in{i + 1}", MtlxDataTypes.Float);
            }
        }
    }
}
