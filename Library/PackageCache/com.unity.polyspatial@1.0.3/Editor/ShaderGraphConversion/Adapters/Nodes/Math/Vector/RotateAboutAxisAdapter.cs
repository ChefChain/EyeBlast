
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RotateAboutAxisAdapter : ANodeAdapter<RotateAboutAxisNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("In", "in");
            portMap.Add("Axis", "axis");
            // portMap.Add("Rotation", "amount");
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Rotate3d, node, graph, externals, "RotateAboutAxis", portMap);

            var angleSlot = NodeUtils.GetSlotByName(node, "Rotation");

            var rotationScale = -1f;
            if (node is RotateAboutAxisNode rotateNode && rotateNode.unit == RotationUnit.Radians)
                rotationScale *= Mathf.Rad2Deg;

            var negate = graph.AddNode(NodeUtils.GetNodeName(node, "RotateAboutAxisAngle"), MtlxNodeTypes.Multiply, MtlxDataTypes.Float);
            negate.AddPortValue("in1", MtlxDataTypes.Float, SlotUtils.GetDefaultValue(angleSlot));
            negate.AddPortValue("in2", MtlxDataTypes.Float, new float[] { rotationScale });
            externals.AddExternalPortAndEdge(angleSlot, negate.name, "in1");

            graph.AddPortAndEdge(negate.name, nodeData.name, "amount", MtlxDataTypes.Float);
        }
    }
}
