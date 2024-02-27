


using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TriplanarAdapter : ANodeAdapter<TriplanarNode>
    {
        // Blend, sampler state, and normal unpack don't work.
        // TODO: SS and Normal Unpack (probably) can be addressed in the same pass as SampleTexture2D,
        // but without the underlying component images (sampled in the triplanar operation), the blend input can't really be used.

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var outputNode = graph.AddNode(NodeUtils.GetNodeName(node, "Triplanar"), MtlxNodeTypes.Triplanar, MtlxDataTypes.Color4);
            externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, outputNode.name);

            outputNode.AddPortValue("default", MtlxDataTypes.Color4, new float[] { 1, 1, 1, 1 });
            outputNode.AddPortString("filtertype", MtlxDataTypes.String, "cubic");

            var textureSlot = NodeUtils.GetSlotByName(node, "Texture");
            var positionSlot = NodeUtils.GetSlotByName(node, "Position");
            var normalSlot = NodeUtils.GetSlotByName(node, "Normal");
            var tileSlot = NodeUtils.GetSlotByName(node, "Tile");
            var blendSlot = NodeUtils.GetSlotByName(node, "Blend");

            ////////////////////////////////////////////////////
            // Forward the position and tile slot as a multiply operation, since USG does position * tile.
            var mulNode = graph.AddNode(NodeUtils.GetNodeName(node, "TriplanarTilePosition"), MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3);
            mulNode.AddPort("in1", MtlxDataTypes.Vector3);
            mulNode.AddPortValue("in2", MtlxDataTypes.Float, SlotUtils.GetDefaultValue(tileSlot));

            externals.AddExternalPortAndEdge(positionSlot, mulNode.name, "in1");
            externals.AddExternalPortAndEdge(tileSlot, mulNode.name, "in2");

            graph.AddPortAndEdge(mulNode.name, outputNode.name, "position", MtlxDataTypes.Vector3);

            // if we aren't connected, we need to get worldspace position.
            if (!positionSlot.isConnected)
            {
                var positionNode = graph.AddNode(NodeUtils.GetNodeName(node, "TriplanarPosition"), MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3);
                positionNode.AddPortString("space", MtlxDataTypes.String, "world");
                graph.AddEdge(positionNode.name, mulNode.name, "in1");
            }

            ////////////////////////////////////////////////////
            // handle the normal slot, mtlx defaults to object space, whereas we default to world space.
            MtlxNodeData normalNode;
            if (normalSlot.isConnected)
            {
                normalNode = graph.AddNode(NodeUtils.GetNodeName(node, "TriplanarNormal"), MtlxNodeTypes.Dot, MtlxDataTypes.Vector3);
                externals.AddExternalPortAndEdge(normalSlot, normalNode.name, "in");
            }
            else
            {
                normalNode = graph.AddNode(NodeUtils.GetNodeName(node, "TriplanarNormal"), MtlxNodeTypes.GeomNormal, MtlxDataTypes.Vector3);
                normalNode.AddPortString("space", MtlxDataTypes.String, "world");
            }
            graph.AddPortAndEdge(normalNode.name, outputNode.name, "normal", MtlxDataTypes.Vector3);

            ////////////////////////////////////////////////////
            // forward the texture slot, since we're reusing it across all axes.
            var texture = SlotUtils.GetDefaultFilename(textureSlot) ?? "FNF";
            var texNode = graph.AddNode(NodeUtils.GetNodeName(node, "TriplanarTexture"), MtlxNodeTypes.Constant, MtlxDataTypes.Filename);
            texNode.AddPortString("value", MtlxDataTypes.Filename, texture);
            if (textureSlot.isConnected)
            {
                externals.AddExternalPortAndEdge(textureSlot, texNode.name, "value");
            }
            else
            {
                var variableName = node.GetVariableNameForSlot(textureSlot.id);
                QuickNode.EnsureImplicitProperty(variableName, MtlxDataTypes.Filename, graph);
                graph.AddEdge(variableName, texNode.name, "value");
            }
            graph.AddPortAndEdge(texNode.name, outputNode.name, "filex", MtlxDataTypes.Filename);
            graph.AddPortAndEdge(texNode.name, outputNode.name, "filey", MtlxDataTypes.Filename);
            graph.AddPortAndEdge(texNode.name, outputNode.name, "filez", MtlxDataTypes.Filename);
        }
    }
}
