using System;
using System.Text;
using System.Linq;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class MtlxGraphProcessor : IGraphProcessor
    {
        public string FileExtension => "mtlx";

        public bool IsEnabled() => true;

        public bool GenerateIntermediateFile() => true;

        public string ProcessGraph(MtlxGraphData graph)
        {
            // For consistency, convert to unix-style line endings and paths
            return MtlxBuilder.ProcessGraph(graph)
                .ToString()
                .Replace("\r\n", "\n")
                .Replace("\\", "/");
        }
    }

    internal static class MtlxBuilder
    {
        internal static StringBuilder ProcessGraph(MtlxGraphData graph)
        {
            StringBuilder builder = new();
            Append(builder, 0, "<?xml version =\"1.0\"?>");
            Append(builder, 0, "<materialx version = \"1.38\" colorspace = \"lin_rec709\">");

            foreach (var node in graph.Nodes)
            {
                ProcessNode(builder, node, graph);
            }

            Append(builder, 0, "</materialx>");
            return builder;
        }

        private static void Append(StringBuilder builder, int tabs, string line)
        {
            string indent = new(' ', tabs * 4);
            builder.AppendLine($"{indent}{line}");
        }

        private static void ProcessNode(StringBuilder builder, MtlxNodeData node, MtlxGraphData graph)
        {
            Append(builder, 1, $"<{node.nodetype} name = \"{node.name}\" type = \"{node.datatype}\">");
            foreach (var port in node.Ports)
            {
                ProcessPort(builder, port, node, graph);
            }
            Append(builder, 1, $"</{node.nodetype}>");
        }

        private static void ProcessPort(StringBuilder builder, MtlxPortData port, MtlxNodeData node, MtlxGraphData graph)
        {
            var srcName = graph.GetConnectedNode(node.name, port.name);
            if (srcName == null)
            {
                ProcessPortByValue(builder, port);
            }
            else
            {
                var srcNode = graph.GetNode(srcName);
                ProcessPortByEdge(builder, srcNode, port);
            }
        }

        private static void ProcessPortByValue(StringBuilder builder, MtlxPortData port)
        {
            try
            {
                var value = port.datatype switch
                {
                    MtlxDataTypes.Boolean       => port.value[0] != 0 ? "true" : "false",
                    MtlxDataTypes.Integer       => $"{(int)port.value[0]}",
                    MtlxDataTypes.Float         => $"{port.value[0]}",
                    MtlxDataTypes.Filename      => port.stringData,
                    MtlxDataTypes.String        => port.stringData,
                    MtlxDataTypes.Color4Array   => string.Join(",", port.value),
                    MtlxDataTypes.FloatArray    => string.Join(",", port.value),
                    _                           => string.Join(",", port.value.Take(MtlxDataTypes.GetLength(port.datatype)))
                };
                Append(builder, 2, $"<input name = \"{port.name}\" type = \"{port.datatype}\" value = \"{value}\" />");
            }
            catch
            {
                // There was no valid value, so we should not output the input port and instead fallback to the mtlx native default value.
                // Append(builder, 2, $"<input name = \"{port.name}\" type = \"{port.datatype}\" />");
            }
        }

        private static void ProcessPortByEdge(StringBuilder builder, MtlxNodeData srcNode, MtlxPortData dstPort)
        {
            var channels = ResolveSwizzle(srcNode.datatype, dstPort.datatype);
            if (string.IsNullOrEmpty(channels))
            {
                Append(builder, 2, $"<input name = \"{dstPort.name}\" type = \"{dstPort.datatype}\" nodename = \"{srcNode.name}\" />");
            }
            else
            {
                // TODO: Boolean value conversions don't like 'channels', but also fails without it.
                // need to determine if there is a more appropriate way to trivially convert (preferably w/out adding the 'convert' node).
                // Mm... I think we can probably do that when resolving external edges.
                Append(builder, 2, $"<input name = \"{dstPort.name}\" type = \"{dstPort.datatype}\" nodename = \"{srcNode.name}\" channels = \"{channels}\" />");
            }
        }

        // Note: the support for inline input attribute swizzling via 'channels' may not work in some cases.
        // this operation is instead handled by the ExternalEdgeMap, who now has an optional secondary pass to
        // fixup the type conversions using this method. ExternalEdgeMap shouldn't be flattening conversions though,
        // so this is a bit of a code smell for now.
        private const string colors = "rgba";
        private const string bases = "xyzw";
        private const string scolor = "rrrr";
        private const string sbase = "xxxx";

        internal static string ResolveSwizzle(string srcType, string dstType)
        {
            if (srcType == dstType)
                return null;

            var dstLen = MtlxDataTypes.GetLength(dstType);
            bool IsScalar = MtlxDataTypes.GetLength(srcType) == 1;
            var charset =
                MtlxDataTypes.IsColor(srcType) && IsScalar ? scolor
                : MtlxDataTypes.IsColor(srcType) ? colors
                : IsScalar ? sbase
                : bases;

            return charset.Substring(0, dstLen);
        }
    }
}
