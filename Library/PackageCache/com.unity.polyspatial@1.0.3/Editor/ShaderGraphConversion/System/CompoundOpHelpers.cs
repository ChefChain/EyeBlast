using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class CompoundOpContext
    {
        internal AbstractMaterialNode Node { get; private set; }
        internal MtlxGraphData Graph { get; private set; }
        internal ExternalEdgeMap Externals { get; private set; }
        internal SubGraphContext SGContext { get; private set; }
        internal string Hint { get; private set; }
        internal Dictionary<string, NodeDef> NodeDefs { get; private set; }
        internal Dictionary<string, MtlxNodeData> NodeData  { get; private set; } = new();
        internal Dictionary<string, MtlxNodeData> ExternalDotNodes  { get; private set; } = new();

        internal CompoundOpContext(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals,
            SubGraphContext sgContext, string hint, Dictionary<string, NodeDef> nodeDefs)
        {
            Node = node;
            Graph = graph;
            Externals = externals;
            SGContext = sgContext;
            Hint = hint;
            NodeDefs = nodeDefs;
        }
    }

    internal static class DictionaryExtensions
    {
        public static string ToContentsString<V>(this Dictionary<string, V> nodeDefs)
            where V : AbstractDef
        {
            return string.Join(", ", nodeDefs.Select(entry => $"[\"{entry.Key}\"] = {entry.Value}"));
        }    
    }

    internal abstract class AbstractDef
    {
    }

    internal class NodeDef : AbstractDef
    {
        internal string NodeType { get; private set; }
        internal string OutputType { get; private set; }
        internal string OutputName { get; private set; }
        internal Dictionary<string, InputDef> Inputs { get; private set; }
        
        internal NodeDef(
            string nodeType, string outputType, Dictionary<string, InputDef> inputs, string outputName = "out")
        {
            NodeType = nodeType;
            OutputType = outputType;
            Inputs = inputs;
            OutputName = outputName;
        }

        internal MtlxNodeData AddNodesAndEdges(CompoundOpContext ctx, string key)
        {
            if (!ctx.NodeData.TryGetValue(key, out var nodeDatum))
            {
                nodeDatum = ctx.Graph.AddNode(
                    NodeUtils.GetNodeName(ctx.Node, $"{ctx.Hint}_{key}"), NodeType, OutputType);
                nodeDatum.outputName = OutputName;
                ctx.NodeData.Add(key, nodeDatum);

                var outputSlot = NodeUtils.GetOutputByName(ctx.Node, key);
                if (outputSlot != null)
                    ctx.Externals.AddExternalPort(outputSlot.slotReference, nodeDatum.name);
                
                foreach (var (inputName, inputDef) in Inputs)
                {
                    inputDef.AddPortsAndEdges(ctx, nodeDatum, key, inputName);
                }
            }
            return nodeDatum;
        }

        public override bool Equals(Object obj)
        {
            if (obj is not NodeDef other ||
                other.NodeType != NodeType ||
                other.OutputType != OutputType ||
                other.OutputName != OutputName ||
                other.Inputs.Count != Inputs.Count)
            {
                return false;
            }
            foreach (var entry in other.Inputs)
            {
                if (!(Inputs.TryGetValue(entry.Key, out var value) && entry.Value.Equals(value)))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();
            hashCode.Add(NodeType);
            hashCode.Add(OutputType);
            hashCode.Add(OutputName);
            foreach (var entry in Inputs)
            {
                hashCode.Add(entry.Key);
                hashCode.Add(entry.Value);
            }
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $"NodeDef({ToRawString()})";
        }

        internal string ToRawString()
        {
            return $"\"{NodeType}\", \"{OutputType}\", \"{OutputName}\", {{{Inputs.ToContentsString()}}}";
        }
    }

    internal abstract class InputDef : AbstractDef
    {
        internal abstract void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey);
    }

    // An input that resolves to a constant numeric value.
    internal class FloatInputDef : InputDef
    {
        internal string PortType { get; private set; }
        internal float[] Values { get; private set; }

        internal FloatInputDef(string portType, params float[] values)
        {
            PortType = portType;
            Values = values;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            nodeDatum.AddPortValue(inputKey, PortType, Values);
        }

        public override bool Equals(Object obj)
        {
            if (obj is not FloatInputDef other || other.PortType != PortType || other.Values.Length != Values.Length)
                return false;
            
            for (var i = 0; i < other.Values.Length; ++i)
            {
                if (other.Values[i] != Values[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();
            hashCode.Add(PortType);
            foreach (var value in Values)
            {
                hashCode.Add(value);
            }
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $"FloatInputDef(\"{PortType}\", {string.Join(", ", Values)})";
        }
    }

    // An input that resolves to a constant string value.
    internal class StringInputDef : InputDef
    {
        internal string Value { get; private set; }

        internal StringInputDef(string value)
        {
            Value = value;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            nodeDatum.AddPortString(inputKey, MtlxDataTypes.String, Value);
        }

        public override bool Equals(Object obj)
        {
            return obj is StringInputDef other && other.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"StringInputDef(\"{Value}\")";
        }
    }

    // An input that resolves to the output of a node defined and mapped in the compound op.
    internal class InternalInputDef : InputDef
    {
        internal string Source { get; private set; }

        internal InternalInputDef(string source)
        {
            Source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var sourceNode = ctx.NodeDefs[Source].AddNodesAndEdges(ctx, Source);
            ctx.Graph.AddPortAndEdge(sourceNode.name, nodeDatum.name, inputKey, sourceNode.datatype);
        }

        public override bool Equals(Object obj)
        {
            return obj is InternalInputDef other && other.Source == Source;
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        public override string ToString()
        {
            return $"InternalInputDef(\"{Source}\")";
        }
    }

    // An input that resolves to one of the inputs of the original shader graph node.
    internal class ExternalInputDef : InputDef
    {
        internal string Source { get; private set; }

        internal ExternalInputDef(string source)
        {
            Source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            if (!ctx.ExternalDotNodes.TryGetValue(Source, out var dotNode))
            {
                var slot = NodeUtils.GetInputByName(ctx.Node, Source, true);
                var dataType = SlotUtils.GetDataTypeName(slot);

                // "Dot" is the identity function, not a dot product; we create a new node to represent
                // the external input because ExternalEdgeMap can only map a slot to a single port, but we
                // assume that this will be optimized out in the final shader code.
                var dotNodeType = MtlxNodeTypes.Dot;
                var dotNodeInput = "in";
                if (dataType == MtlxDataTypes.Matrix22)
                {
                    // The RealityKit MaterialX node defs seem to have omitted a "dot" node for Matrix22,
                    // so use a constant node instead.
                    dotNodeType = MtlxNodeTypes.Constant;
                    dotNodeInput = "value";
                }
                dotNode = ctx.Graph.AddNode(NodeUtils.GetNodeName(
                    ctx.Node, $"{ctx.Hint}_{Source}"), dotNodeType, dataType);
                ctx.ExternalDotNodes.Add(Source, dotNode);
                
                if (slot.isConnected)
                {
                    QuickNode.AddInputPortAndEdge(ctx.Externals, dotNode, slot, dotNodeInput, dataType);
                }
                else
                {
                    switch (slot)
                    {
                        case UVMaterialSlot uvSlot:
                        {
                            // Unconnected UV inputs need a GeomTexCoord node (flipping the v coord)
                            var texCoordNode = QuickNode.CreateUVNode(
                                ctx.Graph, $"{dotNode.name}UV", (int)uvSlot.channel);

                            var multiplyNode = ctx.Graph.AddNode(
                                $"{dotNode.name}Multiply", MtlxNodeTypes.Multiply, dataType);
                            ctx.Graph.AddPortAndEdge(texCoordNode.name, multiplyNode.name, "in1", dataType);
                            multiplyNode.AddPortValue("in2", dataType, new[] { 1.0f, -1.0f });

                            var addNode = ctx.Graph.AddNode($"{dotNode.name}Add", MtlxNodeTypes.Add, dataType);
                            ctx.Graph.AddPortAndEdge(multiplyNode.name, addNode.name, "in1", dataType);
                            addNode.AddPortValue("in2", dataType, new[] { 0.0f, 1.0f });

                            ctx.Graph.AddPortAndEdge(addNode.name, dotNode.name, dotNodeInput, dataType);
                            break;
                        }
                        case Texture2DInputMaterialSlot:
                        case Texture3DInputMaterialSlot:
                        case CubemapInputMaterialSlot:
                        {
                            // Unconnected texture inputs need an implicit property
                            var variableName = ctx.Node.GetVariableNameForSlot(slot.id);
                            QuickNode.EnsureImplicitProperty(variableName, MtlxDataTypes.Filename, ctx.Graph);
                            ctx.Graph.AddPortAndEdge(variableName, dotNode.name, dotNodeInput, MtlxDataTypes.Filename);
                            break;
                        }
                        case ViewDirectionMaterialSlot:
                        {
                            var geomNode = ctx.Graph.AddNode(
                                $"{dotNode.name}Geom", MtlxNodeTypes.RealityKitViewDirection, MtlxDataTypes.Vector3);
                            var flipNode = ctx.Graph.AddNode(
                                $"{dotNode.name}Flip", MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3);
                            ctx.Graph.AddPortAndEdge(geomNode.name, flipNode.name, "in1", MtlxDataTypes.Vector3);
                            flipNode.AddPortValue("in2", MtlxDataTypes.Vector3, new[] { 1.0f, 1.0f, -1.0f });
                            ctx.Graph.AddPortAndEdge(flipNode.name, dotNode.name, dotNodeInput, MtlxDataTypes.Vector3);
                            break;
                        }
                        case NormalMaterialSlot normalSlot and { space: CoordinateSpace.Tangent }:
                            // RealityKit doesn't accept tangent space normals, but we know that they're (0, 0, 1).
                            dotNode.AddPortValue(dotNodeInput, MtlxDataTypes.Vector3, new[] { 0.0f, 0.0f, 1.0f });
                            break;

                        case SpaceMaterialSlot spaceMaterialSlot:
                        {
                            var nodeType = slot switch
                            {
                                TangentMaterialSlot => MtlxNodeTypes.GeomTangent,
                                BitangentMaterialSlot => MtlxNodeTypes.GeomBitangent,
                                NormalMaterialSlot => MtlxNodeTypes.GeomNormal,
                                PositionMaterialSlot => MtlxNodeTypes.GeomPosition,
                                _ => throw new NotSupportedException($"Unsupported slot type {slot.GetType()}"),
                            };
                            var geomNode = ctx.Graph.AddNode($"{dotNode.name}Geom", nodeType, MtlxDataTypes.Vector3);
                            var space = spaceMaterialSlot.space switch
                            {
                                CoordinateSpace.Object => "object",
                                CoordinateSpace.World => "world",
                                CoordinateSpace.Tangent => "tangent",
                                _ => throw new NotSupportedException($"Unsupported space {spaceMaterialSlot.space}"),
                            };
                            geomNode.AddPortString("space", MtlxDataTypes.String, space);

                            // Flip to convert between RealityKit and Unity coordinates.
                            var flipNode = ctx.Graph.AddNode(
                                $"{dotNode.name}Flip", MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3);
                            ctx.Graph.AddPortAndEdge(geomNode.name, flipNode.name, "in1", MtlxDataTypes.Vector3);
                            flipNode.AddPortValue("in2", MtlxDataTypes.Vector3, new[] { 1.0f, 1.0f, -1.0f });

                            ctx.Graph.AddPortAndEdge(flipNode.name, dotNode.name, dotNodeInput, MtlxDataTypes.Vector3);
                            break;
                        }
                        default:
                            // Other unconnected inputs just use the default value.
                            QuickNode.AddInputPortAndEdge(ctx.Externals, dotNode, slot, dotNodeInput, dataType);
                            break;
                    }
                }
            }
            ctx.Graph.AddPortAndEdge(dotNode.name, nodeDatum.name, inputKey, dotNode.datatype);
        }

        public override bool Equals(Object obj)
        {
            return obj is ExternalInputDef other && other.Source == Source;
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        public override string ToString()
        {
            return $"ExternalInputDef(\"{Source}\")";
        }
    }

    // An input that resolves to the output of an unmapped node described inline in the constructor.
    internal class InlineInputDef : InputDef
    {
        internal NodeDef Source { get; private set; }

        internal InlineInputDef(
            string nodeType, string outputType, Dictionary<string, InputDef> inputs, string outputName = "out")
        {
            Source = new NodeDef(nodeType, outputType, inputs, outputName);
        }

        internal InlineInputDef(NodeDef source)
        {
            Source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var sourceNode = Source.AddNodesAndEdges(ctx, $"{nodeKey}_{inputKey}");
            ctx.Graph.AddPortAndEdge(sourceNode.name, nodeDatum.name, inputKey, sourceNode.datatype);
        }

        public override bool Equals(Object obj)
        {
            return obj is InlineInputDef other && other.Source.Equals(Source);
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        public override string ToString()
        {
            return $"InlineInputDef({Source.ToRawString()})";
        }
    }

    // An input that resolves to an implicit property in the graph: that is, a property that does not correspond to one
    // of the user-specified inputs.  This includes both global properties (like "_SinTime") and properties
    // automatically supplied by PolySpatial on a per-renderer basis (like "polySpatial_Lightmap").
    internal class ImplicitInputDef : InputDef
    {
        internal string NodeName { get; private set; }
        internal string DataType { get; private set; }

        internal ImplicitInputDef(string nodeName, string dataType)
        {
            NodeName = nodeName;
            DataType = dataType;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            QuickNode.EnsureImplicitProperty(NodeName, DataType, ctx.Graph);
            ctx.Graph.AddPortAndEdge(NodeName, nodeDatum.name, inputKey, DataType);
        }

        public override bool Equals(Object obj)
        {
            return obj is ImplicitInputDef other && other.NodeName == NodeName && other.DataType == DataType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeName, DataType);
        }

        public override string ToString()
        {
            return $"ImplicitInputDef(\"{NodeName}\", \"{DataType}\")";
        }
    }

    // An input that resolves to the texture size property associated with a texture.
    // The texture must be supplied as an input to the original shader graph node.
    internal class TextureSizeInputDef : InputDef
    {
        internal string Source { get; private set; }

        internal TextureSizeInputDef(string source)
        {
            Source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var slot = NodeUtils.GetInputByName(ctx.Node, Source, true);
            var sizeNodeName = TextureSizeAdapter.EnsureTextureSizeProperty(slot, ctx.Graph);
            ctx.Graph.AddPortAndEdge(sizeNodeName, nodeDatum.name, inputKey, MtlxDataTypes.Vector3);
        }

        public override bool Equals(Object obj)
        {
            return obj is TextureSizeInputDef other && other.Source == Source;
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        public override string ToString()
        {
            return $"TextureSizeInputDef(\"{Source}\")";
        }
    }

    // An input that resolves to a different result depending on which stage the original shader graph
    // node's outputs are targeting.  Both stages must have the same type.
    internal class PerStageInputDef : InputDef
    {
        internal InputDef Vertex { get; private set; }
        internal InputDef Fragment { get; private set; }

        internal PerStageInputDef(InputDef vertex, InputDef fragment)
        {
            Vertex = vertex;
            Fragment = fragment;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var outputs = new List<MaterialSlot>();
            ctx.Node.GetOutputSlots(outputs);
            foreach (var output in outputs)
            {
                // Fragment is the default stage, so any occurrence of Vertex means that's the only one we can use.
                if (SlotUtils.GetEffectiveShaderStage(output, ctx.SGContext) == ShaderStage.Vertex)
                {
                    Vertex.AddPortsAndEdges(ctx, nodeDatum, nodeKey, inputKey);
                    return;
                }
            }
            Fragment.AddPortsAndEdges(ctx, nodeDatum, nodeKey, inputKey);
        }

        public override bool Equals(Object obj)
        {
            return obj is PerStageInputDef other && other.Vertex.Equals(Vertex) && other.Fragment.Equals(Fragment);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Vertex, Fragment);
        }

        public override string ToString()
        {
            return $"PerStageInputDef({Vertex}, {Fragment})";
        }
    }
}