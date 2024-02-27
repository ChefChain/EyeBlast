using System.Linq;
using System.Collections.Generic;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;

namespace UnityEditor.ShaderGraph.MaterialX
{
    // NOTE:
    //  Graph Inputs are any nodes of type MtlxNodeTypes.Constant.
    //  Graph Outputs are all ports of the node whose Node.datatype is MtlxDataTypes.Surface.
    //  The Material node is the node whose Node.nodetype is MtlxNodeTypes.Material.
    //
    // For sanitization, constant nodes with a "prop" prefix are material properties.
    // The remaining string of that node's name is the material property name.

    internal class MtlxGraphData
    {
        // TODO: Linq wrapper to get inputs & outputs with sanitized property names.
        internal string path { get; private set; }
        internal string name { get; private set; }
        internal IEnumerable<MtlxNodeData> Nodes => nodes.Values;
        internal IEnumerable<string> Inputs => inputs;
        internal IEnumerable<string> SystemInputs => systemInputs;


        internal List<FileTextureReferences> EvalTextureReferences()
        {
            var result = new List<FileTextureReferences>();
            foreach (var node in Nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (port.datatype == MtlxDataTypes.Filename)
                    {
                        result.Add(new FileTextureReferences { nodeName = node.name, portName = port.name, fileName = port.stringData });
                    }
                }
            }
            return result;
        }

        internal struct FileTextureReferences
        {
            internal string nodeName;
            internal string portName;
            internal string fileName;
        }

        private Dictionary<string, MtlxNodeData> nodes;
        private List<string> inputs;
        private List<string> systemInputs;
        internal Dictionary<(string, string), string> edges;

        internal MtlxGraphData(string name, string path)
        {
            this.name = PolySpatialShaderGraph.SanitizeName(name);
            this.path = path;
            nodes = new();
            edges = new();
            inputs = new();
            systemInputs = new();
        }

        internal bool HasNode(string name) => nodes.ContainsKey(name);


        internal bool GetOrAddNode(string name, string nodeType, string dataType, out MtlxNodeData node)
        {
            if (HasNode(name))
            {
                node = GetNode(name);
                return false;
            }
            node = AddNode(name, nodeType, dataType);
            return true;
        }

        internal MtlxNodeData AddNode(string name, string nodeType, string dataType, bool isInput = false, bool isSystemInput = false)
        {
            var node = new MtlxNodeData(name, nodeType, dataType);
            nodes.Add(name, node);
            if (isInput)
                inputs.Add(name);
            if (isSystemInput)
                systemInputs.Add(name);
            return node;
        }

        internal void AddPortAndEdge(string srcNode, string dstNode, string dstPort, string dstType)
        {
            nodes[dstNode].AddPort(dstPort, dstType);
            AddEdge(srcNode, dstNode, dstPort);
        }
        internal void AddEdge(string srcNode, string dstNode, string dstPort)
            => edges.Add((dstNode, dstPort), srcNode);
        internal bool HasConnection(string dstNode, string dstPort)
            => edges.ContainsKey((dstNode, dstPort));
        internal string GetConnectedNode(string dstNode, string dstPort)
            => edges.GetValueOrDefault((dstNode, dstPort));
        internal MtlxNodeData GetConnectedNode(MtlxNodeData dstNode, string dstPort)
        {
            string connectedNodeName = GetConnectedNode(dstNode.name, dstPort);
            return nodes[connectedNodeName];
        }
        internal bool TryGetConnectedNode(MtlxNodeData dstNode, string dstPort, out MtlxNodeData srcNode)
        {
            if (!edges.TryGetValue((dstNode.name, dstPort), out var connectedNodeName))
            {
                srcNode = default;
                return false;
            }

            srcNode = nodes[connectedNodeName];
            return true;
        }
        internal MtlxNodeData GetNode(string nodeName)
            => nodes.GetValueOrDefault(nodeName);

        internal void ReScope(string scopeName)
        {
            var scopedNodes = new Dictionary<string, MtlxNodeData>();
            var scopedEdges = new Dictionary<(string, string), string>();

            foreach (var nodeKV in nodes)
            {
                var node = nodeKV.Value;
                node.name = ReScope(scopeName, node.name);
                scopedNodes.Add(node.name, node);
            }
            foreach (var edgeKV in edges)
            {
                var dstNode = ReScope(scopeName, edgeKV.Key.Item1);
                var dstPort = edgeKV.Key.Item2;
                var srcNode = ReScope(scopeName, edgeKV.Value);
                scopedEdges.Add((dstNode, dstPort), srcNode);
            }

            nodes.Clear();
            edges.Clear();

            nodes = scopedNodes;
            edges = scopedEdges;
        }

        string ReScope(string scopeName, string nodeName)
        {
            return systemInputs.Contains(nodeName) ? nodeName : $"{scopeName}{nodeName}";
        }

        internal void Merge(MtlxGraphData other)
        {
            foreach (var edge in other.edges)
                edges.Add(edge.Key, edge.Value);
            foreach (var node in other.nodes)
            {
                if (other.systemInputs.Contains(node.Key))
                    nodes.TryAdd(node.Key, node.Value);
                else
                    nodes.Add(node.Key, node.Value);
            }
            foreach (var systemInput in other.systemInputs)
                if (!systemInputs.Contains(systemInput))
                    systemInputs.Add(systemInput);
        }
    }

    internal class MtlxNodeData
    {
        internal string name { get; set; }
        internal string nodetype { get; private set; }
        internal string datatype { get; set; }
        internal string outputName { get; set; } = "out";

        internal IEnumerable<MtlxPortData> Ports => ports.Values;

        private Dictionary<string, MtlxPortData> ports;


        internal MtlxNodeData(string name, string nodeType, string dataType)
        {
            this.name = name;
            this.nodetype = nodeType;
            this.datatype = dataType;
            ports = new();
        }

        internal bool HasPort(string name)
            => ports.ContainsKey(name);
        internal MtlxPortData GetPort(string name)
            => ports[name];
        internal void AddPort(string name, string type)
            => ports.Add(name, new MtlxPortData(name, type));
        internal void AddPortString(string name, string type, string stringData = null)
            => ports.Add(name, new MtlxPortData(name, type, stringData));
        internal void AddPortValue(string name, string type, float[] value = null)
            => ports.Add(name, new MtlxPortData(name, type, value));
    }
    internal class MtlxPortData
    {
        internal string name { get; private set; }
        internal string datatype { get; private set; }
        internal float[] value { get; private set; } // TODO: remove array.
        internal string stringData { get; private set; }

        internal void SetValue(float[] value)
            => this.value = value;

        internal MtlxPortData(string name, string type)
        {
            this.name = name;
            this.datatype = type;
        }

        internal MtlxPortData(string name, string type, float[] value)
        {
            this.name = name;
            this.datatype = type;

            if (value != null)
            {
                this.value = new float[value.Length];
                value.CopyTo(this.value, 0);
            }
        }

        internal MtlxPortData(string name, string type, string stringData)
        {
            this.name = name;
            this.datatype = type;
            this.stringData = stringData;
        }
    }
}
