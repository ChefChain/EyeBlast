using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class ExternalEdgeMap
    {
        internal struct PortReference { internal string node, port; }
        internal struct ExternalEdge { internal Graphing.SlotReference src, dst; }

        internal Dictionary<Graphing.SlotReference, Graphing.SlotReference> slotToSlot;
        internal Dictionary<Graphing.SlotReference, PortReference> slotToPort;
        internal Dictionary<(string, string), Graphing.SlotReference> portToSlot;
        internal List<ExternalEdge> externalEdges;

        internal ExternalEdgeMap()
        {
            slotToSlot = new();
            slotToPort = new();
            externalEdges = new();
            portToSlot = new();
        }

        internal void AddExternalPort(Graphing.SlotReference slot, string mtlxNode, string mtlxPort = "")
        {
            // No need to throw an exception on duplicate key if the value being assigned is the same.
            if (slotToPort.TryGetValue(slot, out var current) && current.node == mtlxNode && current.port == mtlxPort)
                return;
            
            slotToPort.Add(slot, new PortReference { node = mtlxNode, port = mtlxPort });
            portToSlot.TryAdd((mtlxNode, mtlxPort), slot);
        }

        internal void AddExternalEdge(Graphing.SlotReference src, Graphing.SlotReference dst)
        {
            externalEdges.Add(new ExternalEdge { src = src, dst = dst });
        }

        internal void AddExternalPortAndEdge(MaterialSlot slot, string mtlxNode, string mtlxPort)
        {
            if (slot.isConnected)
            {
                var upstreamSlot = SlotUtils.GetSourceConnectionSlot(slot);
                AddExternalEdge(upstreamSlot.slotReference, slot.slotReference);
                AddExternalPort(slot.slotReference, mtlxNode, mtlxPort);
            }
        }

        internal void RemapExternalPort(Graphing.SlotReference mapFrom, Graphing.SlotReference mapTo)
        {
            slotToSlot.Add(mapFrom, mapTo);
        }

        internal void ResolveExternals(MtlxGraphData graph, bool flattenTypeConversions = true)
        {
            foreach (var externalEdge in externalEdges)
            {
                try
                {
                    var srcE = externalEdge.src;
                    var dstE = externalEdge.dst;

                    while (slotToSlot.ContainsKey(srcE))
                        srcE = slotToSlot[srcE];
                    while (slotToSlot.ContainsKey(dstE))
                        dstE = slotToSlot[dstE];

                    var src = slotToPort[srcE];
                    var dst = slotToPort[dstE];

                    graph.AddEdge(src.node, dst.node, dst.port);
                }
                catch
                {
                    // NOTE: It's not erroneous for a node to not be supported.
                    // If an upstream node is not supported, the port will just use whatever value was stored on it.
                    // In this sense, when writing an adapter, it's important to give all ports default values.
                }
            }

            if (flattenTypeConversions)
                ResolveTypeConversions(graph);
        }
        static void ResolveTypeConversions(MtlxGraphData graph)
        {
            var newEdgeSet = new Dictionary<(string, string), string>();

            foreach (var edgeKV in graph.edges)
            {
                var dstNodeName = edgeKV.Key.Item1;
                var dstPortName = edgeKV.Key.Item2;
                var srcNodeName = edgeKV.Value;

                var dstNode = graph.GetNode(dstNodeName);
                var dstPort = dstNode.GetPort(dstPortName);
                var srcNode = graph.GetNode(srcNodeName);

                var srcType = srcNode.datatype;
                var dstType = dstPort.datatype;

                var castTitle = $"From{srcNodeName}To{dstNodeName}{dstPortName}";

                // no conversion needed, keep the original edge.
                if (srcType == dstType)
                {
                    newEdgeSet.Add((dstNodeName, dstPortName), srcNodeName);
                    continue;
                }

                // NOTE: There are no mtlx nodes with support for a boolean typed output.
                // As such, there does not appear to be any way to convert TO a boolean type.
                // we'd have to drop boolean and use integers in all cases instead-- TODO?
                if (dstType == MtlxDataTypes.Boolean
                    || MtlxDataTypes.GetLength(dstType) > 4
                    || MtlxDataTypes.GetLength(srcType) > 4)
                {
                    throw new System.Exception($"Mtlx cannot convert from {srcType} to {dstType}, from '{srcNodeName}' to '{dstNodeName}.{dstPortName}'.");
                }

                // The spec can't handle conversion between bool/int/float types very well,
                // so we use an explicit `convert` to get rid of non-floating point.
                if (MtlxDataTypes.GetLength(srcType) == 1 && srcType != MtlxDataTypes.Float)
                {
                    var convertNodeName = $"CastSrcToFloat{castTitle}";
                    var convertNode = graph.AddNode(convertNodeName, MtlxNodeTypes.Convert, MtlxDataTypes.Float);
                    convertNode.AddPort("in", srcType);

                    newEdgeSet.Add((convertNodeName, "in"), srcNodeName);

                    // abstract the source node, so we are only dealing in floating point.
                    srcType = MtlxDataTypes.Float;
                    srcNodeName = convertNodeName;

                    // Avoid the (illegal) float-to-float swizzle if float is our final destination.
                    if (srcType == dstType)
                    {
                        newEdgeSet.Add((dstNodeName, dstPortName), srcNodeName);
                        continue;
                    }
                }

                // previous steps will have abstracted int/bool to float, so we know we're just dealing with
                // float, color, and vector-- which `Swizzle` should cover.
                if (MtlxDataTypes.GetLength(srcType) >= MtlxDataTypes.GetLength(dstType)
                    || MtlxDataTypes.GetLength(srcType) == 1 && MtlxDataTypes.GetLength(dstType) != 1)
                {
                    var convertNodeName = $"CastSwizzle{castTitle}";
                    var convertNode = graph.AddNode(convertNodeName, MtlxNodeTypes.Swizzle, dstType);
                    convertNode.AddPort("in", srcType);
                    convertNode.AddPortString("channels", MtlxDataTypes.String, MtlxBuilder.ResolveSwizzle(srcType, dstType));

                    newEdgeSet.Add((convertNodeName, "in"), srcNodeName);
                    srcNodeName = convertNodeName;
                }
                else if (MtlxDataTypes.GetLength(srcType) < MtlxDataTypes.GetLength(dstType))
                {
                    var inputNodeName = $"CastConvert{castTitle}";
                    var inputNode = graph.AddNode(inputNodeName, MtlxNodeTypes.Dot, srcType);
                    inputNode.AddPort("in", srcType);
                    newEdgeSet.Add((inputNodeName, "in"), srcNodeName);
                    var outputNode = graph.AddNode($"CastConvertFinal{castTitle}", $"combine{MtlxDataTypes.GetLength(dstType)}", dstType);

                    bool c = MtlxDataTypes.IsColor(srcType);
                    for (int i = 0; i < MtlxDataTypes.GetLength(dstType); ++i)
                    {
                        outputNode.AddPortValue($"in{i+1}", MtlxDataTypes.Float, new float[] { 0 });
                        if (i < MtlxDataTypes.GetLength(srcType))
                        {
                            var swNode = graph.AddNode($"CastSwizzle{i+1}{castTitle}", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                            swNode.AddPort("in", srcType);
                            swNode.AddPortString("channels", MtlxDataTypes.String, (c ? "rgba" : "xyzw").Substring(i, 1));
                            newEdgeSet.Add((swNode.name, "in"), inputNodeName);
                            newEdgeSet.Add((outputNode.name, $"in{i+1}"), swNode.name);
                        }
                    }

                    srcNodeName = outputNode.name;
                }

                // Finally, add the last edge.
                newEdgeSet.Add((dstNodeName, dstPortName), srcNodeName);
            }
            graph.edges.Clear();
            graph.edges = newEdgeSet;
        }
    }
}
