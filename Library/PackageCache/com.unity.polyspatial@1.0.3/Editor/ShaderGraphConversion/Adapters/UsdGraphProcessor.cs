using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class UsdGraphProcessor : IGraphProcessor
    {
        public string FileExtension => "usda";

        public bool IsEnabled() => true;

        public bool GenerateIntermediateFile() => true;

        public string ProcessGraph(MtlxGraphData graph)
        {
            var usdBuilder = new UsdBuilder();
            usdBuilder.ProcessGraph(graph);

            // For consistency, convert to unix-style line endings and paths
            return usdBuilder.GetUsdaString()
                .Replace("\r\n", "\n")
                .Replace("\\", "/");
        }
    }

    // This class converts the MtlxGraphData intermediate representation (IR) into
    // USDA.
    // TODO(LXR-1274): Add support for binary USD formats (usdc, usdz, etc.)
    internal class UsdBuilder
    {
        static readonly int spacesPerIndent = 4;

        // Initial text used for all exported USD files
        static readonly List<string> usdPreface = new List<string>
        {
            @"#usda 1.0",
            @"(",
            @"    metersPerUnit = 1",
            @"    upAxis = ""Y""",
            @")",
            @"",
        };

        static readonly string surfaceShaderOutput = @"token outputs:out";

        static readonly string vertexShaderOutput = @"token outputs:out";

        // RAII solution for creating and closing nested USD scopes
        struct UsdScope : IDisposable
        {
            UsdBuilder m_usdBuilder;
            int m_IndentLevel;

            public int ChildIndentLevel => m_IndentLevel + 1;

            public UsdScope(UsdBuilder usdBuilder, string definition, int indentLevel = 0)
            {
                m_usdBuilder = usdBuilder;
                m_IndentLevel = indentLevel;

                m_usdBuilder.AppendIndentedLine(definition, m_IndentLevel);
                m_usdBuilder.AppendIndentedLine("{", m_IndentLevel);
            }

            public UsdScope(UsdBuilder usdBuilder, List<string> definition, int indentLevel = 0)
            {
                m_usdBuilder = usdBuilder;
                m_IndentLevel = indentLevel;

                foreach (var definitionLine in definition)
                {
                    m_usdBuilder.AppendIndentedLine(definitionLine, m_IndentLevel);
                }
                m_usdBuilder.AppendIndentedLine("{", m_IndentLevel);
            }

            public UsdScope AddChildScope(string definition)
            {
                return new UsdScope(m_usdBuilder, definition, ChildIndentLevel);
            }

            public UsdScope AddChildScope(List<string> definition)
            {
                return new UsdScope(m_usdBuilder, definition, ChildIndentLevel);
            }

            public void Dispose()
            {
                m_usdBuilder.AppendIndentedLine("}", m_IndentLevel);
            }
        }

        StringBuilder m_StringBuilder = new();
        MtlxGraphData m_Graph;

        // Get the fully converted material as a USD-ascii string
        internal string GetUsdaString()
        {
            return m_StringBuilder.ToString();
        }

        // Each shader graph currently needs to be encoded as a separate .usda file
        internal void ProcessGraph(MtlxGraphData graph)
        {
            m_Graph = graph;
            m_StringBuilder.Clear();

            AppendIndentedLines(usdPreface, 0);

            using (var materialXScope = new UsdScope(this, @"def ""MaterialX"""))
            {
                using (var materialsScope = materialXScope.AddChildScope(@"def ""Materials"""))
                {
                    foreach (var node in m_Graph.Nodes)
                    {
                        if (node.nodetype.Equals("surfacematerial", StringComparison.OrdinalIgnoreCase))
                        {
                            string materialDefinition = $@"def Material ""{ node.name}""";
                            using (var materialScope = materialsScope.AddChildScope(materialDefinition))
                            {
                                ProcessMaterial(materialScope, node);
                            }
                        }
                    }
                }
            }
        }

        // Process the material node, including its properties and all subgraph nodes
        private void ProcessMaterial(UsdScope materialScope, MtlxNodeData materialNode)
        {
            Assert.IsTrue(materialNode.datatype.Equals("material", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(materialNode.nodetype.Equals("surfacematerial", StringComparison.OrdinalIgnoreCase));

            MtlxNodeData surfaceShaderNode = m_Graph.GetConnectedNode(materialNode, "surfaceshader");
            m_Graph.TryGetConnectedNode(materialNode, "vertexshader", out var vertexShaderNode);

            ProcessShaderProperties(materialScope);

            // Connect shader output to material output
            AppendIndentedLine(
                @$"token outputs:mtlx:surface.connect = </MaterialX/Materials/{materialNode.name}/{surfaceShaderNode.nodetype}.outputs:out>",
                materialScope.ChildIndentLevel);

            if (vertexShaderNode != null)
            {
                AppendIndentedLine(
                    @$"token outputs:realitykit:vertex.connect = </MaterialX/Materials/{materialNode.name}/GeometryModifier.outputs:out>",
                    materialScope.ChildIndentLevel);
            }
            m_StringBuilder.AppendLine("");

            HashSet<MtlxNodeData> shaderSubgraphRoots = new();
            ProcessShader(
                materialScope, materialNode, surfaceShaderNode, surfaceShaderNode.nodetype,
                @$"uniform token info:id = ""ND_{surfaceShaderNode.nodetype}_surfaceshader""",
                surfaceShaderOutput, shaderSubgraphRoots);
            if (vertexShaderNode != null)
            {
                m_StringBuilder.AppendLine("");

                ProcessShader(
                    materialScope, materialNode, vertexShaderNode, "GeometryModifier",
                    @"uniform token info:id = ""ND_realitykit_geometrymodifier_vertexshader""",
                    vertexShaderOutput, shaderSubgraphRoots);
            }

            ProcessShaderSubgraphs(materialScope, materialNode, shaderSubgraphRoots);
        }

        private void ProcessShaderProperties(UsdScope materialScope)
        {
            // Add all the custom inputs
            foreach (var input in m_Graph.Inputs)
            {
                ProcessShaderInput(input, materialScope);
            }

            // TODO(SND-131): Add all the "system" inputs.
            foreach (var input in m_Graph.SystemInputs)
            {
                ProcessShaderInput(input, materialScope);
            }

            m_StringBuilder.AppendLine("");
        }

        private void ProcessShaderInput(string input, UsdScope materialScope)
        {
            var inputNode = m_Graph.GetNode(input);
            if (inputNode == null)
            {
                Debug.LogWarning($"Missing node for shader input {input}");
                return;
            }

            // Currently, filenames are always textures. The texture may have been specified
            // as "None" at processing time, in which case we will have no ports.
            if (inputNode.datatype.Equals("filename"))
            {
                AppendIndentedLine(
                    @$"{GetUsdDataType(inputNode.datatype)} inputs:{inputNode.name} = @placeholder.png@ (colorSpace = ""srgb_texture"")",
                    materialScope.ChildIndentLevel);
                return;
            }

            Assert.AreEqual(inputNode.Ports.Count(), 1);
            foreach (var port in inputNode.Ports)
            {
                AppendPortInput(port, inputNode.name, materialScope.ChildIndentLevel);
            }
        }

        private void ProcessShader(
            UsdScope materialScope, MtlxNodeData materialNode, MtlxNodeData shaderNode, string shaderName,
            string shaderNodeInfo, string shaderOutput, HashSet<MtlxNodeData> rootNodes)
        {
            using (var shaderScope = materialScope.AddChildScope(@$"def Shader ""{shaderName}"""))
            {
                // Add node type info
                AppendIndentedLine(shaderNodeInfo, shaderScope.ChildIndentLevel);

                // Connect inputs, and gather up subgraph roots for further processing
                foreach (var port in shaderNode.Ports)
                {
                    // Both inputs and connected nodes have node representations (inputs as default values).
                    if (m_Graph.TryGetConnectedNode(shaderNode, port.name, out var connectedNode))
                    {
                        if (m_Graph.Inputs.Contains(connectedNode.name) ||
                            m_Graph.SystemInputs.Contains(connectedNode.name))
                        {
                            // Attach input and/or systemInputs.
                            string nodeConnection =
                                $"{GetUsdDataType(port.datatype)} inputs:{port.name}.connect = " +
                                $"</MaterialX/Materials/{materialNode.name}.inputs:{connectedNode.name}>";
                            AppendIndentedLine(nodeConnection, shaderScope.ChildIndentLevel);
                        }
                        else
                        {
                            // Attach connected node.
                            string nodeConnection =
                                $"{GetUsdDataType(port.datatype)} inputs:{port.name}.connect = " +
                                $"</MaterialX/Materials/{materialNode.name}/{connectedNode.name}" +
                                $".outputs:{connectedNode.outputName}>";
                            AppendIndentedLine(nodeConnection, shaderScope.ChildIndentLevel);
                            rootNodes.Add(connectedNode);
                        }
                    }
                    else
                    {
                        AppendPortInput(port, port.name, shaderScope.ChildIndentLevel);
                    }
                }

                // Add output
                AppendIndentedLine(shaderOutput, shaderScope.ChildIndentLevel);
            }
        }

        private void ProcessShaderSubgraphs(
            UsdScope materialScope, MtlxNodeData materialNode, HashSet<MtlxNodeData> pendingNodes)
        {
            HashSet<MtlxNodeData> processedNodes = new();
            while (pendingNodes.Count > 0)
            {
                m_StringBuilder.AppendLine("");

                var node = pendingNodes.First();
                pendingNodes.Remove(node);
                processedNodes.Add(node);

                using (var nodeScope = materialScope.AddChildScope(@$"def Shader ""{node.name}"""))
                {
                    string nodeInfoId = GetNodeInfoId(node);
                    AppendIndentedLine($@"uniform token info:id = ""{nodeInfoId}""", nodeScope.ChildIndentLevel);

                    foreach (var port in node.Ports)
                    {
                        // If this port is connected to a node, add that node to the queue for recursive processing
                        if (m_Graph.TryGetConnectedNode(node, port.name, out var connectedNode))
                        {
                            if (m_Graph.Inputs.Contains(connectedNode.name)
                                || m_Graph.SystemInputs.Contains(connectedNode.name))
                            {
                                string inputParameterConnection = $"{GetUsdDataType(port.datatype)} inputs:{port.name}.connect = </MaterialX/Materials/{materialNode.name}.inputs:{connectedNode.name}>";
                                AppendIndentedLine(inputParameterConnection, nodeScope.ChildIndentLevel);
                            }
                            else
                            {
                                string nodeConnection =
                                    $"{GetUsdDataType(port.datatype)} inputs:{port.name}.connect = " +
                                    $"</MaterialX/Materials/{materialNode.name}/{connectedNode.name}.outputs:{connectedNode.outputName}>";
                                AppendIndentedLine(nodeConnection, nodeScope.ChildIndentLevel);

                                if (!processedNodes.Contains(connectedNode))
                                    pendingNodes.Add(connectedNode);
                            }
                        }
                        // Files and strings are supplied via stringData
                        else if (port.datatype.Equals("string") || port.datatype.Equals("filename"))
                        {
                            AppendIndentedLine(@$"{GetUsdDataType(port.datatype)} inputs:{port.name} = ""{port.stringData}""", nodeScope.ChildIndentLevel);
                        }
                        else
                        {
                            AppendPortInput(port, port.name, nodeScope.ChildIndentLevel);
                        }
                    }
                    AppendIndentedLine(
                        $@"{GetUsdDataType(node.datatype)} outputs:{node.outputName}", nodeScope.ChildIndentLevel);
                }
            }
        }

        private void AppendPortInput(MtlxPortData port, string inputName, int indentLevel)
        {
            int dataLength = MtlxDataTypes.GetLength(port.datatype);

            // If there's no value, there's *probably* a connection that will supply this
            if (port.value == null || port.value.Length == 0)
            {
                AppendIndentedLine(
                    $"{GetUsdDataType(port.datatype)} inputs:{inputName} = {GetUsdDefaultValue(port.datatype)}",
                    indentLevel);
            }
            // Handle scalar numerical or boolean values
            else if (port.value.Length == 1 || dataLength == 1)
            {
                AppendIndentedLine(
                    $"{GetUsdDataType(port.datatype)} inputs:{inputName} = {port.value[0]}", indentLevel);
            }
            // Handle vector numerical values
            else
            {
                string indent = new(' ', indentLevel * spacesPerIndent);
                m_StringBuilder.Append($"{indent}{GetUsdDataType(port.datatype)} inputs:{inputName} = ");

                var isArray = MtlxDataTypes.IsArray(port.datatype);
                m_StringBuilder.Append(isArray ? '[' : '(');

                var valueLength = isArray ? port.value.Length : Math.Min(dataLength, port.value.Length);
                if (valueLength > 0)
                {
                    int elementLength = MtlxDataTypes.GetElementLength(port.datatype);
                    AppendValueElement(port, 0, elementLength);

                    int elementCount = valueLength / elementLength;
                    for (int i = 1; i < elementCount; ++i)
                    {
                        m_StringBuilder.Append(", ");
                        AppendValueElement(port, i * elementLength, elementLength);
                    }
                }

                m_StringBuilder.AppendLine(isArray ? "]" : ")");
            }
        }

        private void AppendValueElement(MtlxPortData port, int startIndex, int length)
        {
            if (length == 1)
            {
                m_StringBuilder.Append($"{port.value[startIndex]}");
            }
            else
            {
                m_StringBuilder.Append($"({port.value[startIndex]}");
                for (int i = 1; i < length; ++i)
                {
                    m_StringBuilder.Append($", {port.value[startIndex + i]}");
                }
                m_StringBuilder.Append(")");
            }
        }

        // Convert from MaterialX type to corresponding USD type
        private string GetUsdDataType(string datatype)
        {
            if (datatype.Equals("boolean", StringComparison.OrdinalIgnoreCase))
                return "bool";

            if (datatype.Equals("integer", StringComparison.OrdinalIgnoreCase))
                return "int";

            if (datatype.Equals("color3", StringComparison.OrdinalIgnoreCase))
                return "color3f";
            if (datatype.Equals("color4", StringComparison.OrdinalIgnoreCase))
                return "color4f";

            if (datatype.Equals("vector2", StringComparison.OrdinalIgnoreCase))
                return "float2";
            if (datatype.Equals("vector3", StringComparison.OrdinalIgnoreCase))
                return "float3";
            if (datatype.Equals("vector4", StringComparison.OrdinalIgnoreCase))
                return "float4";

            if (datatype.Equals("matrix44", StringComparison.OrdinalIgnoreCase))
                return "matrix4d";
            if (datatype.Equals("matrix33", StringComparison.OrdinalIgnoreCase))
                return "matrix3d";
            if (datatype.Equals("matrix22", StringComparison.OrdinalIgnoreCase))
                return "matrix2d";

            if (datatype.Equals("floatarray", StringComparison.OrdinalIgnoreCase))
                return "float[]";
            if (datatype.Equals("color4array", StringComparison.OrdinalIgnoreCase))
                return "color4f[]";

            if (datatype.Equals("filename", StringComparison.OrdinalIgnoreCase))
                return "asset";

            return datatype;
        }

        private string GetUsdDefaultValue(string datatype)
        {       
            if (datatype.Equals("float", StringComparison.OrdinalIgnoreCase) ||
                datatype.Equals("half", StringComparison.OrdinalIgnoreCase) ||
                datatype.Equals("boolean", StringComparison.OrdinalIgnoreCase))
            {
                return "0";
            }

            if (datatype.Equals("color3", StringComparison.OrdinalIgnoreCase))
                return "(1, 1, 1)";
            if (datatype.Equals("color4", StringComparison.OrdinalIgnoreCase))
                return "(1, 1, 1, 1)";

            if (datatype.Equals("vector2", StringComparison.OrdinalIgnoreCase))
                return "(0, 0)";
            if (datatype.Equals("vector3", StringComparison.OrdinalIgnoreCase))
                return "(0, 0, 0)";
            if (datatype.Equals("vector4", StringComparison.OrdinalIgnoreCase))
                return "(0, 0, 0, 0)";

            if (datatype.Equals("matrix44", StringComparison.OrdinalIgnoreCase))
                return "((1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1))";
            if (datatype.Equals("matrix33", StringComparison.OrdinalIgnoreCase))
                return "((1, 0, 0), (0, 1, 0), (0, 0, 1))";
            if (datatype.Equals("matrix22", StringComparison.OrdinalIgnoreCase))
                return "((1, 0), (0, 1))";

            Debug.LogError($"Can't determine default for data type {datatype}");
            return "()";
        }

        // USD handles "overloaded" materialX nodes by fully expanding the matrix of options into a set of unique node IDs.
        private string GetNodeInfoId(MtlxNodeData node)
        {
            // TODO(LWXR-1273): Support other nodes with more complicated input/output mappings. For examples, see:
            // transformmatrix, arrayappend, remap, smoothstep
            switch (node.nodetype)
            {
                case "swizzle":
                case "convert":
                    return $"ND_{node.nodetype}_{RequireFirstInputPortType(node)}_{node.datatype}";

                case "dotproduct":
                case "extract":
                case "magnitude":
                case "determinant":
                    return $"ND_{node.nodetype}_{RequireFirstInputPortType(node)}";

                case "noise2d":
                case "noise3d":
                case "fractal3d":
                    return GetFloatVariantNodeInfoId(node, "amplitude");

                case "add":
                case "subtract":
                case "multiply":
                case "divide":
                case "modulo":
                case "power":
                case "safepower":
                case "min":
                case "max":
                    return GetFloatVariantNodeInfoId(node, "in2");

                case "clamp":
                case "smoothstep":
                    return GetFloatVariantNodeInfoId(node, "low", "high");

                case "remap":
                    return GetFloatVariantNodeInfoId(node, "inlow", "inhigh", "outlow", "outhigh");

                case "contrast":
                    return GetFloatVariantNodeInfoId(node, "amount", "pivot");

                case "range":
                    return GetFloatVariantNodeInfoId(node, "inlow", "inhigh", "gamma", "outlow", "outhigh");
                
                case "ifequal":
                    return GetFirstInputPortType(node, "value") switch
                    {
                        MtlxDataTypes.Integer => $"ND_ifequal_{node.datatype}I",
                        MtlxDataTypes.Boolean => $"ND_ifequal_{node.datatype}B",
                        _ => GetDefaultNodeInfoId(node),
                    };
                case "switch":
                    return (GetFirstInputPortType(node, "which") == MtlxDataTypes.Integer) ?
                        $"ND_switch_{node.datatype}I" : GetDefaultNodeInfoId(node);

                case "combine2":
                    if (node.datatype == MtlxDataTypes.Color4)
                    {
                        return "ND_combine2_color4CF";
                    }
                    else if (node.datatype == MtlxDataTypes.Vector4)
                    {
                        return (RequireFirstInputPortType(node) == MtlxDataTypes.Vector3) ?
                            "ND_combine2_vector4VF" : "ND_combine2_vector4VV";
                    }
                    else 
                    {
                        return GetDefaultNodeInfoId(node);
                    }
                case "transformmatrix":
                    return (node.datatype, GetFirstInputPortType(node, "mat")) switch
                    {
                        (MtlxDataTypes.Vector2, MtlxDataTypes.Matrix33) => "ND_transformmatrix_vector2M3",
                        (MtlxDataTypes.Vector3, MtlxDataTypes.Matrix44) => "ND_transformmatrix_vector3M4",
                        _ => GetDefaultNodeInfoId(node),
                    };
                default:
                    switch (node.nodetype)
                    {
                        case MtlxNodeTypes.RealityKitSurfaceModelToWorld:
                        case MtlxNodeTypes.RealityKitSurfaceModelToView:
                        case MtlxNodeTypes.RealityKitSurfaceWorldToView:
                        case MtlxNodeTypes.RealityKitSurfaceViewToProjection:
                        case MtlxNodeTypes.RealityKitSurfaceProjectionToView:
                        case MtlxNodeTypes.RealityKitSurfaceScreenPosition:
                        case MtlxNodeTypes.RealityKitGeometryModifierModelToWorld:
                        case MtlxNodeTypes.RealityKitGeometryModifierWorldToModel:
                        case MtlxNodeTypes.RealityKitGeometryModifierModelToView:
                        case MtlxNodeTypes.RealityKitGeometryModifierViewToProjection:
                        case MtlxNodeTypes.RealityKitGeometryModifierProjectionToView:
                        case MtlxNodeTypes.RealityKitGeometryModifierVertexID:
                        case MtlxNodeTypes.RealityKitSurfaceCustomAttribute:
                        case MtlxNodeTypes.RealityKitEnvironmentRadiance:
                            return $"ND_{node.nodetype}";

                        default:
                            return GetDefaultNodeInfoId(node);
                    }
            }
        }

        private string GetFloatVariantNodeInfoId(MtlxNodeData node, params string[] inputs)
        {
            if (node.datatype == MtlxDataTypes.Float)
                return GetDefaultNodeInfoId(node);

            string commonInputType = null;
            foreach (var input in inputs)
            {
                var inputType = GetFirstInputPortType(node, input);
                if (inputType != null)
                {
                    if (commonInputType == null)
                        commonInputType = inputType;
                    else if (commonInputType != inputType)
                        Debug.LogError($"Mismatched input types on node {node.name} ({commonInputType}/{inputType})");
                }
            }
            return (commonInputType == MtlxDataTypes.Float) ?
                $"ND_{node.nodetype}_{node.datatype}FA" : GetDefaultNodeInfoId(node);
        }
        
        private string GetDefaultNodeInfoId(MtlxNodeData node)
        {
            return $"ND_{node.nodetype}_{node.datatype}";
        }

        private string RequireFirstInputPortType(MtlxNodeData node, string prefix = "in")
        {
            var inputType = GetFirstInputPortType(node, prefix);
            if (inputType != null)
                return inputType;

            Debug.LogError($"Failed to find input node for node {node.name}");
            return "unknown";
        }

        private string GetFirstInputPortType(MtlxNodeData node, string prefix)
        {
            foreach (var port in node.Ports)
            {
                if (port.name.StartsWith(prefix))
                    return port.datatype;
            }
            return null;
        }

        private void AppendIndentedLine(string line, int indentLevel = 0)
        {
            string indent = new(' ', indentLevel * spacesPerIndent);
            m_StringBuilder.AppendLine($"{indent}{line}");
        }

        private void AppendIndentedLines(List<string> lines, int indentLevel = 0)
        {
            string indent = new(' ', indentLevel * spacesPerIndent);
            foreach (var line in lines)
            {
                m_StringBuilder.AppendLine($"{indent}{line}");
            }
        }
    }
}
