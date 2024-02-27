
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TriangleWaveAdapter : ANodeAdapter<TriangleWaveNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            string nodeName = NodeUtils.GetNodeName(node, "TriWave");

            string dataTypeName = NodeUtils.GetDataTypeName(node);

            var inputSlots = new List<MaterialSlot>();
            var outputSlots = new List<MaterialSlot>();
            node.GetInputSlots(inputSlots);
            node.GetOutputSlots(outputSlots);
            externals.AddExternalPort(outputSlots[0].slotReference, nodeName);

            // Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;

            // setup the input slot handling- dot node is useful if an input is reused in the logic.
            var dotNode = graph.AddNode($"{nodeName}Input", MtlxNodeTypes.Dot, dataTypeName);
            dotNode.AddPort("in", dataTypeName);

            var upstreamSlot = SlotUtils.GetSourceConnectionSlot((inputSlots[0]));
            if (upstreamSlot != null)
            {
                externals.AddExternalPort(inputSlots[0].slotReference, dotNode.name, "in");
                externals.AddExternalEdge(upstreamSlot.slotReference, inputSlots[0].slotReference);
            }

            // 0.5 +
            var addNode = graph.AddNode($"{nodeName}InAdd", MtlxNodeTypes.Add, dataTypeName);
            addNode.AddPortValue("in1", dataTypeName, new float[] {0.5f, 0.5f, 0.5f, 0.5f}); // just in case.
            graph.AddPortAndEdge(dotNode.name, addNode.name, "in2", dataTypeName);

            // floor(...)
            var floorNode = graph.AddNode($"{nodeName}InFloor", MtlxNodeTypes.Floor, dataTypeName);
            graph.AddPortAndEdge(addNode.name, floorNode.name, "in", dataTypeName);

            // In - floor(...)
            var inSubNode = graph.AddNode($"{nodeName}InSub", MtlxNodeTypes.Subtract, dataTypeName);
            graph.AddPortAndEdge(dotNode.name, inSubNode.name, "in1", dataTypeName);
            graph.AddPortAndEdge(floorNode.name, inSubNode.name, "in2", dataTypeName);

            // 2 * (In - floor(...)) )
            var inMulNode = graph.AddNode($"{nodeName}InMul", MtlxNodeTypes.Multiply, dataTypeName);
            inMulNode.AddPortValue("in1", dataTypeName, new float[] { 2.0f, 2.0f, 2.0f, 2.0f });
            graph.AddPortAndEdge(inSubNode.name, inMulNode.name, "in2", dataTypeName);

            // abs(2 * (...))
            var absNode = graph.AddNode($"{nodeName}Abs", MtlxNodeTypes.Absolute, dataTypeName);
            graph.AddPortAndEdge(inMulNode.name, absNode.name, "in", dataTypeName);

            // 2 * abs(...)
            var mulNode = graph.AddNode($"{nodeName}Mul", MtlxNodeTypes.Multiply, dataTypeName);
            mulNode.AddPortValue("in1", dataTypeName, new float[] { 2.0f, 2.0f, 2.0f, 2.0f });
            graph.AddPortAndEdge(absNode.name, mulNode.name, "in2", dataTypeName);

            // 2 * abs(...) - 1.0
            var subNode = graph.AddNode(nodeName, MtlxNodeTypes.Subtract, dataTypeName);
            graph.AddPortAndEdge(mulNode.name, subNode.name, "in1", dataTypeName);
            subNode.AddPortValue("in2", dataTypeName, new float[] { 1f, 1f, 1f, 1f });
        }
    }
}
