
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class LogAdapter : ANodeAdapter<LogNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is LogNode lnode)
            {
                if (lnode.logBase == LogBase.BaseE)
                {
                    QuickNode.UnaryOp(MtlxNodeTypes.NaturalLog, node, graph, externals, "Log");
                    return;
                }

                // Need to change the base ala ln(In)/Ln(Base)
                var nBase = lnode.logBase == LogBase.Base10 ? 10 : 2;

                var outputSlot = NodeUtils.GetPrimaryOutput(node);
                var inputSlot = NodeUtils.GetSlotByName(node, "In");
                var dataType = SlotUtils.GetDataTypeName(outputSlot);

                var nodeName = NodeUtils.GetNodeName(node, "Log");
                var baseName = NodeUtils.GetNodeName(node, "LogBase");
                var inputName = NodeUtils.GetNodeName(node, "LogIn");

                // log(x)
                var inputNode = graph.AddNode(inputName, MtlxNodeTypes.NaturalLog, dataType);
                inputNode.AddPortValue("in", dataType, SlotUtils.GetDefaultValue(inputSlot));
                externals.AddExternalPortAndEdge(inputSlot, inputNode.name, "in");

                // log(b)
                var baseNode = graph.AddNode(baseName, MtlxNodeTypes.NaturalLog, dataType);

                List<float> baseValue = new List<float>();
                for (int i = 0; i < MtlxDataTypes.GetLength(dataType); ++i)
                    baseValue.Add(nBase);
                baseNode.AddPortValue("in", dataType, baseValue.ToArray());

                // log(x) / log(b)
                var outputNode = graph.AddNode(nodeName, MtlxNodeTypes.Divide, dataType);
                graph.AddPortAndEdge(inputNode.name, outputNode.name, "in1", dataType);
                graph.AddPortAndEdge(baseNode.name, outputNode.name, "in2", dataType);
                externals.AddExternalPort(outputSlot.slotReference, outputNode.name);
            }
        }
    }
}
