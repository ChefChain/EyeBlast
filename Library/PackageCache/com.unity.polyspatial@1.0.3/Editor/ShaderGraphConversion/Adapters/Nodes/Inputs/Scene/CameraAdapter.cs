
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CameraAdapter : ANodeAdapter<CameraNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            QuickNode.AddImplicitPropertyFromNode(PolySpatialShaderGlobals.WorldSpaceCameraPos, MtlxDataTypes.Vector3, node, graph, externals, "Position");
            QuickNode.AddImplicitPropertyFromNode(PolySpatialShaderGlobals.WorldSpaceCameraDir, MtlxDataTypes.Vector3, node, graph, externals, "Direction");
#else
            // Flip camera position in z to switch from RealityKit to Unity coordinate space.
            QuickNode.CompoundOp(node, graph, externals, sgContext, "CameraPosition", new()
            {
                ["Position"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitCameraPosition, MtlxDataTypes.Vector3, new()),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, -1.0f),
                }),
            });

            // Invert the world-to-view matrix in order to find the camera direction.
            var worldToViewNode = graph.AddNode(
                NodeUtils.GetNodeName(node, "WorldToView"), MtlxNodeTypes.RealityKitSurfaceWorldToView,
                MtlxDataTypes.Matrix44);
            worldToViewNode.outputName = "worldToView";

            var viewToWorldNode = graph.AddNode(
                NodeUtils.GetNodeName(node, "ViewToWorld"), MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44);
            graph.AddPortAndEdge(worldToViewNode.name, viewToWorldNode.name, "in", MtlxDataTypes.Matrix44);

            // (0, 0, -1) is "forward" in RealityKit coordinates.
            var transformNode = graph.AddNode(
                NodeUtils.GetNodeName(node, "Transform"), MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4);
            graph.AddPortAndEdge(viewToWorldNode.name, transformNode.name, "mat", MtlxDataTypes.Matrix44);
            transformNode.AddPortValue("in", MtlxDataTypes.Vector4, new[] { 0.0f, 0.0f, -1.0f, 0.0f });

            // Flip transformed position in Z to convert to Unity coordinate space.
            var flipNode = graph.AddNode(
                NodeUtils.GetNodeName(node, "Flip"), MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4);
            graph.AddPortAndEdge(transformNode.name, flipNode.name, "in1", MtlxDataTypes.Vector4);
            flipNode.AddPortValue("in2", MtlxDataTypes.Vector4, new[] { 1.0f, 1.0f, -1.0f, 1.0f });

            externals.AddExternalPort(NodeUtils.GetOutputByName(node, "Direction").slotReference, flipNode.name);
#endif

            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Orthographic", MtlxDataTypes.Float, "w");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Near Plane", MtlxDataTypes.Float, "y");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Far Plane", MtlxDataTypes.Float, "z");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Z Buffer Sign", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Width", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Height", MtlxDataTypes.Float, "y");
        }
    }
}
