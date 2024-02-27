using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal static class NodeUtils
    {
        internal static string GetNodeName(AbstractMaterialNode node, string hint = "")
        {
            return $"{PolySpatialShaderGraph.SanitizeName(hint)}Node_{node.objectId}";
        }

        internal static string RemoveWhitespace(string str)
        {
            return Regex.Replace(str, @"\s+", "");
        }

        internal static string GetDataTypeName(AbstractMaterialNode node)
            => SlotUtils.GetDataTypeName(GetPrimaryOutput(node));

        internal static MaterialSlot GetPrimaryOutput(AbstractMaterialNode node)
        {
            var outputs = new List<MaterialSlot>();
            node.GetOutputSlots(outputs);
            return outputs.FirstOrDefault();
        }

        internal static MaterialSlot GetPrimaryInput(AbstractMaterialNode node)
        {
            var inputs = new List<MaterialSlot>();
            node.GetInputSlots(inputs);
            return inputs.FirstOrDefault();
        }

        internal static MaterialSlot GetInputByName(
            AbstractMaterialNode node, string rawDisplayName, bool ignoreWhitespace = false)
        {
            var inputs = new List<MaterialSlot>();
            node.GetInputSlots(inputs);
            return GetSlotByName(inputs, rawDisplayName, ignoreWhitespace);
        }

        internal static MaterialSlot GetOutputByName(
            AbstractMaterialNode node, string rawDisplayName, bool ignoreWhitespace = false)
        {
            var outputs = new List<MaterialSlot>();
            node.GetOutputSlots(outputs);
            return GetSlotByName(outputs, rawDisplayName, ignoreWhitespace);
        }

        internal static MaterialSlot GetSlotByName(
            AbstractMaterialNode node, string rawDisplayName, bool ignoreWhitespace = false)
        {
            var slots = new List<MaterialSlot>();
            node.GetSlots<MaterialSlot>(slots);
            return GetSlotByName(slots, rawDisplayName, ignoreWhitespace);
        }

        static MaterialSlot GetSlotByName(
            List<MaterialSlot> slots, string rawDisplayName, bool ignoreWhitespace = false)
        {
            var displayName = rawDisplayName;
            if (ignoreWhitespace)
                displayName = RemoveWhitespace(displayName);

            foreach (var slot in slots)
            {
                var slotName = slot.RawDisplayName();
                if (ignoreWhitespace)
                    slotName = RemoveWhitespace(slotName);

                if (slotName == displayName)
                    return slot;
            }
            return null;
        }
    }

    internal static class SlotUtils
    {
        internal static string GetName(MaterialSlot slot)
        {
            return slot.RawDisplayName();
        }

        internal static ShaderStage GetEffectiveShaderStage(MaterialSlot slot, SubGraphContext sgContext)
        {
            if (sgContext == null)
                return UnityEditor.Graphing.NodeUtils.GetEffectiveShaderStage(slot, false);
            
            // Fragment is the default stage, so any occurrence of Vertex means that's the only one we can use.
            foreach (var edge in slot.owner.owner.GetEdges(slot.slotReference))
            {
                switch (edge.inputSlot.node)
                {
                    case SubGraphOutputNode:
                    {
                        var output = NodeUtils.GetOutputByName(sgContext.Node, edge.inputSlot.slot.RawDisplayName());
                        if (GetEffectiveShaderStage(output, sgContext.Parent) == ShaderStage.Vertex)
                            return ShaderStage.Vertex;
                        break;
                    }
                    case var node:
                    {
                        List<MaterialSlot> outputs = new();
                        node.GetOutputSlots(outputs);
                        foreach (var output in outputs)
                        {
                            if (GetEffectiveShaderStage(output, sgContext) == ShaderStage.Vertex)
                                return ShaderStage.Vertex;
                        }
                        break;
                    }
                }
            }

            return ShaderStage.Fragment;
        }

        internal static TextureSamplerState GetPropertyRedirectedTextureSamplerState(
            MaterialSlot slot, SubGraphContext sgContext)
        {
            var (inputSlot, outputSlot) = GetPropertyRedirectedInputOutputSlots(slot, sgContext);
            switch (outputSlot?.owner)
            {
                case SamplerStateNode samplerStateNode:
                    return ((SamplerStateShaderProperty)samplerStateNode.AsShaderProperty()).value;
                
                case PropertyNode propertyNode:
                    return ((SamplerStateShaderProperty)propertyNode.property).value;
                
                default:
                    return ((SamplerStateMaterialSlot)inputSlot).defaultSamplerState;
            }
        }

        internal static (MaterialSlot inputSlot, MaterialSlot outputSlot) GetPropertyRedirectedInputOutputSlots(
            MaterialSlot slot, SubGraphContext sgContext)
        {
            while (sgContext != null)
            {
                var sourceSlot = GetRedirectedSourceConnectionSlot(slot);
                if (sourceSlot?.owner is not PropertyNode propertyNode)
                    return (slot, sourceSlot);
                
                slot = NodeUtils.GetInputByName(sgContext.Node, GetName(NodeUtils.GetPrimaryOutput(propertyNode)));
                sgContext = sgContext.Parent;
            }
            return (slot, GetRedirectedSourceConnectionSlot(slot));
        }

        internal static MaterialSlot GetRedirectedSourceConnectionSlot(MaterialSlot slot)
        {
            while (true)
            {
                var sourceSlot = GetSourceConnectionSlot(slot);
                if (sourceSlot?.owner is not RedirectNodeData)
                    return sourceSlot;
                
                slot = NodeUtils.GetPrimaryInput(sourceSlot.owner);  
            }
        }

        internal static MaterialSlot GetSourceConnectionSlot(MaterialSlot slot)
        {
            if (!slot.isConnected)
                return null;
            return slot.owner.owner.GetEdges(slot.slotReference).First().outputSlot.slot;
        }

        internal static string GetDataTypeName(MaterialSlot slot)
        {
            return slot switch
            {
                ColorRGBMaterialSlot => MtlxDataTypes.Color3,
                ColorRGBAMaterialSlot => MtlxDataTypes.Color4,
                _ => slot.concreteValueType switch
                {
                    ConcreteSlotValueType.Boolean => MtlxDataTypes.Float, // MtlxDataTypes.Boolean,
                    ConcreteSlotValueType.Vector1 => MtlxDataTypes.Float,
                    ConcreteSlotValueType.Vector2 => MtlxDataTypes.Vector2,
                    ConcreteSlotValueType.Vector3 => MtlxDataTypes.Vector3,
                    ConcreteSlotValueType.Vector4 => MtlxDataTypes.Vector4,
                    ConcreteSlotValueType.Matrix2 => MtlxDataTypes.Matrix22,
                    ConcreteSlotValueType.Matrix3 => MtlxDataTypes.Matrix33,
                    ConcreteSlotValueType.Matrix4 => MtlxDataTypes.Matrix44,
                    ConcreteSlotValueType.Texture2D => MtlxDataTypes.Filename,
                    ConcreteSlotValueType.Texture3D => MtlxDataTypes.Filename,
                    ConcreteSlotValueType.Cubemap => MtlxDataTypes.Filename,
                    ConcreteSlotValueType.Gradient => MtlxDataTypes.Color4Array,
                    _ => null
                }
            };
        }

        internal static string GetDefaultFilename(MaterialSlot slot)
        {
            // TODO: Texture3DInputMaterialSlot, etc.
            try
            {
                var tslot = (Texture2DInputMaterialSlot)slot;
                // this is the most cross-pipeline default inline value representation for textures.
                var root = $"{Application.streamingAssetsPath}/{tslot.owner.owner.path}";
                return Path.GetRelativePath(root, UnityEditor.AssetDatabase.GetAssetPath(tslot.texture));
            }
            catch
            {
                return null;
            }
        }

        internal static float[] GetDefaultValue(MaterialSlot slot)
        {
            switch (slot)
            {
                case ColorRGBMaterialSlot c3:
                {
                    var linearValue = ((Color)(Vector4)c3.value).linear;
                    return new[] { linearValue.r, linearValue.g, linearValue.b };
                }
                case ColorRGBAMaterialSlot c4:
                {
                    var linearValue = ((Color)c4.value).linear;
                    return new[] { linearValue.r, linearValue.g, linearValue.b, linearValue.a };
                }
                case BooleanMaterialSlot b:
                    return new[] { b.value ? 1f : 0f };

                case Vector1MaterialSlot f:
                    return new[] { f.value };

                case Vector2MaterialSlot v2:
                    return new[] { v2.value.x, v2.value.y };

                case Vector3MaterialSlot v3:
                    return new[] { v3.value.x, v3.value.y, v3.value.z };

                case DynamicVectorMaterialSlot vd:
                    return new[] { vd.value.x, vd.value.y, vd.value.z, vd.value.w };

                case Vector4MaterialSlot v4:
                    return new[] { v4.value.x, v4.value.y, v4.value.z, v4.value.w };

                case Matrix2MaterialSlot m2:
                    return GetMatrix2Value(m2.value);

                case Matrix3MaterialSlot m3:
                    return GetMatrix3Value(m3.value);

                case Matrix4MaterialSlot m4:
                    return GetMatrix4Value(m4.value);

                case DynamicMatrixMaterialSlot md:
                    return GetConcreteValue(md.concreteValueType, md.value);
                    
                case DynamicValueMaterialSlot dv:
                    return GetConcreteValue(dv.concreteValueType, dv.value);

                default:
                    return null;
            }
        }

        static float[] GetConcreteValue(ConcreteSlotValueType concreteType, Matrix4x4 matrix)
        {
            return concreteType switch
            {
                ConcreteSlotValueType.Matrix2 => GetMatrix2Value(matrix),
                ConcreteSlotValueType.Matrix3 => GetMatrix3Value(matrix),
                _ => GetMatrix4Value(matrix),
            };
        }

        static float[] GetMatrix2Value(Matrix4x4 matrix)
        {
            return new[]
            {
                matrix.m00, matrix.m01,
                matrix.m10, matrix.m11,
            };
        }

        static float[] GetMatrix3Value(Matrix4x4 matrix)
        {
            return new[]
            {
                matrix.m00, matrix.m01, matrix.m02,
                matrix.m10, matrix.m11, matrix.m12,
                matrix.m20, matrix.m21, matrix.m22,
            };
        }

        static float[] GetMatrix4Value(Matrix4x4 matrix)
        {
            return new[]
            {
                matrix.m00, matrix.m01, matrix.m02, matrix.m03,
                matrix.m10, matrix.m11, matrix.m12, matrix.m13,
                matrix.m20, matrix.m21, matrix.m22, matrix.m23,
                matrix.m30, matrix.m31, matrix.m32, matrix.m33,
            };
        }
    }


    internal static class QuickNode
    {
        // TODO: refactor to use NaryOp

        internal static void HandleUVSlot(UVMaterialSlot slot, string name, string dstNodeName, string dstPortName, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // handle the UV Slot.
            if (!slot.isConnected)
            {
                var index = (int)slot.channel;
                var uvRead = CreateUVNode(graph, name, index);

                // Flip V coordinate to get back to Unity coordinate space for processing.
                var multiplyNode = graph.AddNode($"{name}Multiply", MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(uvRead.name, multiplyNode.name, "in1", MtlxDataTypes.Vector2);
                multiplyNode.AddPortValue("in2",  MtlxDataTypes.Vector2, new[] { 1.0f, -1.0f });

                var addNode = graph.AddNode($"{name}Add", MtlxNodeTypes.Add, MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(multiplyNode.name, addNode.name, "in1", MtlxDataTypes.Vector2);
                addNode.AddPortValue("in2",  MtlxDataTypes.Vector2, new[] { 0.0f, 1.0f });

                graph.AddPortAndEdge(addNode.name, dstNodeName, dstPortName, MtlxDataTypes.Vector2);
            }
            else
            {
                var dstNode = graph.GetNode(dstNodeName);
                if (!dstNode.HasPort(dstPortName))
                    // default value not needed, it's either external or endogenous and will be connected.
                    dstNode.AddPort(dstPortName, MtlxDataTypes.Vector2);
                externals.AddExternalPortAndEdge(slot, dstNodeName, dstPortName);
            }
        }

        internal static MtlxNodeData CreateUVNode(MtlxGraphData graph, string name, int index)
        {
            MtlxNodeData uvRead;
            if (index == 0)
            {
                uvRead = graph.AddNode(name, MtlxNodeTypes.GeomTexCoord, MtlxDataTypes.Vector2);
                uvRead.AddPortValue("index", MtlxDataTypes.Integer, new float[] { index });
            }
            else
            {
                // Apple recommends using a primvar reader for UV1.
                uvRead = graph.AddNode(name, MtlxNodeTypes.UsdPrimvarReader, MtlxDataTypes.Vector2);
                uvRead.AddPortString("varname", MtlxDataTypes.String, $"vertexUV{index}");
            }
            return uvRead;
        }

        internal static string GetUVSupportDetails(UVMaterialSlot slot)
        {
            return GetUVSupportDetails((int)slot.channel);
        }

        internal static string GetUVSupportDetails(int channel)
        {
            return (channel > 1) ? "Only UV0 and UV1 are supported." : "";
        }

        internal static void EnsureImplicitProperty(string nodeName, string dataType, MtlxGraphData graph)
        {
            if (!graph.HasNode(nodeName))
            {
                var nodeData = graph.AddNode(nodeName, MtlxNodeTypes.Constant, dataType, false, true);
                // For testing, it's nice to have explicit defaults-
                if (MtlxDataTypes.IsString(dataType))
                    nodeData.AddPortString("value", dataType, "ERR");
                else if (MtlxDataTypes.IsMatrix(dataType))
                    nodeData.AddPortValue("value", dataType, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
                else
                    nodeData.AddPortValue("value", dataType, new float[MtlxDataTypes.GetLength(dataType)]);
            }
        }

        internal static void AddImplicitPropertyFromNode(
            string nodeName, string dataType, AbstractMaterialNode node, MtlxGraphData graph,
            ExternalEdgeMap externals, string slotName, string swizzleType = null, string channels = null)
        {
            var slot = NodeUtils.GetSlotByName(node, slotName);
            if (slot.isConnected)
            {
                EnsureImplicitProperty(nodeName, dataType, graph);
                var outputNodeName = nodeName;
                if (swizzleType != null && channels != null)
                {
                    outputNodeName = NodeUtils.GetNodeName(node, $"{nodeName}{channels}");
                    var outputNode = graph.AddNode(outputNodeName, MtlxNodeTypes.Swizzle, swizzleType);
                    outputNode.AddPortString("channels", MtlxDataTypes.String, channels);
                    graph.AddPortAndEdge(nodeName, outputNodeName, "in", dataType);
                }
                externals?.AddExternalPort(slot.slotReference, outputNodeName);
            }
        }

        internal static MtlxNodeData UnaryOp(string nodetype, AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, string hint, string mtlxPortName = "in", string coerceType = "", string usgSlotName = "")
        {
            string nodeName = NodeUtils.GetNodeName(node, hint);
            string nodeTypeName = nodetype;
            string outputDataType = string.IsNullOrEmpty(coerceType) ? NodeUtils.GetDataTypeName(node) : coerceType;

            var outputSlot = NodeUtils.GetPrimaryOutput(node);
            var nodeData = graph.AddNode(nodeName, nodeTypeName, outputDataType);

            var inputSlot = NodeUtils.GetSlotByName(node, usgSlotName) ?? NodeUtils.GetPrimaryInput(node);
            var inputValue = SlotUtils.GetDefaultValue(inputSlot);
            var inputDataType = string.IsNullOrEmpty(coerceType) ? SlotUtils.GetDataTypeName(inputSlot) : coerceType;
            nodeData.AddPortValue(mtlxPortName, inputDataType, inputValue);


            externals?.AddExternalPort(outputSlot.slotReference, nodeName);
            externals?.AddExternalPortAndEdge(inputSlot, nodeData.name, mtlxPortName);

            return nodeData;
        }

        // TODO: Refactor to use NaryOp
        internal static MtlxNodeData BinaryOp(
            string nodetype,
            AbstractMaterialNode node,
            MtlxGraphData graph,
            ExternalEdgeMap externals,
            string hint,
            string leftParam = "A",
            string mtlxBaseName = "in")
        {
            string nodeName = NodeUtils.GetNodeName(node, hint);
            string nodeTypeName = nodetype;
            string outputDataTypeName = NodeUtils.GetDataTypeName(node);

            var nodeData = graph.AddNode(nodeName, nodeTypeName, outputDataTypeName);

            var inputSlots = new List<MaterialSlot>();
            var outputSlots = new List<MaterialSlot>();
            node.GetInputSlots(inputSlots);
            node.GetOutputSlots(outputSlots);

            externals.AddExternalPort(outputSlots[0].slotReference, nodeName);

            foreach (var slot in inputSlots)
            {
                var portName = slot.shaderOutputName == leftParam ? $"{mtlxBaseName}1" : $"{mtlxBaseName}2";
                var value = SlotUtils.GetDefaultValue(slot);
                var slotType = SlotUtils.GetDataTypeName(slot);
                nodeData.AddPortValue(portName, slotType, value);

                var upstreamSlot = SlotUtils.GetSourceConnectionSlot(slot);
                if (upstreamSlot != null)
                {
                    externals.AddExternalPort(slot.slotReference, nodeName, portName);
                    externals.AddExternalEdge(upstreamSlot.slotReference, slot.slotReference);
                }
            }

            return nodeData;
        }

        internal static MtlxNodeData NaryOp(
            string nodetype,
            AbstractMaterialNode node,
            MtlxGraphData graph,
            ExternalEdgeMap externals,
            string hint,
            Dictionary<string, string> usgToMtlxPortMap = null,
            Dictionary<string, string> usgToMtlxPortType = null,
            string outputType = null
            )
        {
            string nodeName = NodeUtils.GetNodeName(node, hint);
            string nodeTypeName =  nodetype;
            string outputDataTypeName = string.IsNullOrEmpty(outputType) ?  NodeUtils.GetDataTypeName(node) : outputType;

            var nodeData = graph.AddNode(nodeName, nodeTypeName, outputDataTypeName);

            var outputSlot = NodeUtils.GetPrimaryOutput(node);
            externals?.AddExternalPort(outputSlot.slotReference, nodeData.name);


            var inputSlots = new List<MaterialSlot>();
            node.GetInputSlots<MaterialSlot>(inputSlots);

            if (usgToMtlxPortMap != null)
            {
                foreach(var slot in inputSlots)
                {
                    var slotName = SlotUtils.GetName(slot);

                    // only process ports that are mapped.
                    if (!usgToMtlxPortMap.TryGetValue(slotName, out var portName))
                        continue;

                    var portType = SlotUtils.GetDataTypeName(slot);
                    // override the discovered concrete type if desired
                    if (usgToMtlxPortType != null && usgToMtlxPortType.TryGetValue(slotName, out var overridePortType))
                        portType = overridePortType;

                    AddInputPortAndEdge(externals, nodeData, slot, portName, portType);
                }
            }
            return nodeData;
        }

        internal static void AddInputPortAndEdge(
            ExternalEdgeMap externals, MtlxNodeData nodeData, MaterialSlot slot, string portName, string portType)
        {
            var floatValue = SlotUtils.GetDefaultValue(slot);
            var stringValue = SlotUtils.GetDefaultFilename(slot);

            if (floatValue != null)
                nodeData.AddPortValue(portName, portType, floatValue);
            else if (!string.IsNullOrEmpty(stringValue))
                nodeData.AddPortString(portName, portType, stringValue);
            else
                nodeData.AddPort(portName, portType);

            externals.AddExternalPortAndEdge(slot, nodeData.name, portName);
        }

        // Creates nodes and connections for a compound operation: that is, one that is defined in
        // terms of simpler nodes.  The expression provided will be parsed using a limited subset of
        // HLSL and turned into a nodeDefs map.
        internal static void CompoundOp(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext, 
            string hint, string expr, Dictionary<string, string> inputTypeOverrides = null)
        {
            CompoundOp(
                node, graph, externals, sgContext, hint,
                CompoundOpParser.Parse(node, sgContext, expr, inputTypeOverrides));
        }

        // Creates nodes and connections for a compound operation: that is, one that is defined in
        // terms of simpler nodes.  The nodeDefs map contains mappings for shader graph outputs
        // ("Out" being the typical name for single outputs) as well as intermediate results used by
        // multiple internal nodes.
        internal static void CompoundOp(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals,
            SubGraphContext sgContext, string hint, Dictionary<string, NodeDef> nodeDefs)
        {
            CompoundOpContext ctx = new(node, graph, externals, sgContext, hint, nodeDefs);

            List<MaterialSlot> outputSlots = new();
            node.GetOutputSlots(outputSlots);
            foreach (var outputSlot in outputSlots)
            {
                if (nodeDefs.TryGetValue(outputSlot.RawDisplayName(), out var nodeDef))
                    nodeDef.AddNodesAndEdges(ctx, outputSlot.RawDisplayName());
            }
        }
    }
}
