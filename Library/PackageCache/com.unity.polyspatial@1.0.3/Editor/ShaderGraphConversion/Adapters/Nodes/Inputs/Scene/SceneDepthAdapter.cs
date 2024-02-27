using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SceneDepthAdapter : ANodeAdapter<SceneDepthNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is SceneDepthNode snode)
            {
                // Having access to the depth buffer is unlikely, but we can at least give view position and clip position Z.

                // these are duplicated from ScreenPositionAdapter-- there should be a standard way to get evaluated
                // implicit properties. This is just a one-off for now though. --- Adding unique/reusable nodes should be clearer.
                if (graph.GetOrAddNode("ClipPosition", MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, out var clipPos))
                {
                    if (graph.GetOrAddNode("WorldSpacePosition3", MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3, out var wPos))
                        wPos.AddPortString("space", MtlxDataTypes.String, "world");

                    if (graph.GetOrAddNode("WorldSpacePosition4", MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, out var wPos4))
                        graph.AddPortAndEdge(wPos.name, wPos4.name, "in", MtlxDataTypes.Vector3);

                    if (graph.GetOrAddNode("ViewProjection", MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, out var viewProj))
                    {
                        QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.ViewMatrix, MtlxDataTypes.Matrix44, graph);
                        QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.ProjectionMatrix, MtlxDataTypes.Matrix44, graph);
                        graph.AddPortAndEdge(PolySpatialShaderGlobals.ViewMatrix, viewProj.name, "in1", MtlxDataTypes.Matrix44);
                        graph.AddPortAndEdge(PolySpatialShaderGlobals.ProjectionMatrix, viewProj.name, "in2", MtlxDataTypes.Matrix44);
                    }

                    graph.AddPortAndEdge(wPos4.name, clipPos.name, "in", MtlxDataTypes.Vector4);
                    graph.AddPortAndEdge(viewProj.name, clipPos.name, "mat", MtlxDataTypes.Matrix44);
                }

                if (graph.GetOrAddNode("ViewPosition", MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, out var viewPos))
                {
                    graph.AddPortAndEdge("WorldSpacePosition4", viewPos.name, "in", MtlxDataTypes.Vector4);
                    graph.AddPortAndEdge(PolySpatialShaderGlobals.ViewMatrix, viewPos.name, "mat", MtlxDataTypes.Matrix44);
                }

                var sourceNodeName = "";
                switch (snode.depthSamplingMode)
                {
                    case DepthSamplingMode.Eye:
                    case DepthSamplingMode.Linear01:
                        sourceNodeName = clipPos.name;
                        break;
                    case DepthSamplingMode.Raw:
                        sourceNodeName = viewPos.name;
                        break;
                }


                var outputNode = graph.AddNode(NodeUtils.GetNodeName(node, $"Depth{snode.depthSamplingMode}"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                graph.AddPortAndEdge(sourceNodeName, outputNode.name, "in", MtlxDataTypes.Vector4);
                outputNode.AddPortString("channels", MtlxDataTypes.String, "z");

                var outputSlot = NodeUtils.GetPrimaryOutput(node);
                externals.AddExternalPort(outputSlot.slotReference, outputNode.name);
            }
        }
    }
}

