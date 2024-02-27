
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ScreenPositionAdapter : ANodeAdapter<ScreenPositionNode>
    {

        // Depending on a number of factors, the following may or may not work.
        // -- namely, we are not Y-flipping or accounting for the up direction yet.
        // -- also, without simulating the view/projection transforms and screen space, it's difficult to
        // -- achieve expected outcomes in testing. Will need to wait for bugs and reported inconsistencies to correct.
        internal static MtlxNodeData SetupScreenSpace(ScreenSpaceType type, MtlxGraphData graph)
        {
            if(graph.GetOrAddNode("ClipPosition", MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, out var clipPos))
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
            if (type == ScreenSpaceType.Pixel)
                return clipPos;

            if (type == ScreenSpaceType.Raw) // clip/.w => .xy*.5+.5 => .xyz
            {
                if (graph.GetOrAddNode("ScreenPositionRaw", MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, out var raw))
                {
                    var clipW = graph.AddNode("ClipPositionW", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                    graph.AddPortAndEdge(clipPos.name, clipW.name, "in", MtlxDataTypes.Vector4);
                    clipW.AddPortString("channels", MtlxDataTypes.Vector4, "w");

                    var clipH = graph.AddNode("ClipPositionHomogenized", MtlxNodeTypes.Divide, MtlxDataTypes.Vector4);
                    graph.AddPortAndEdge(clipPos.name, clipH.name, "in1", MtlxDataTypes.Vector4);
                    graph.AddPortAndEdge(clipW.name, clipH.name, "in2", MtlxDataTypes.Float);

                    var clipHxy = graph.AddNode("ClipPositionHomoXY", MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(clipH.name, clipHxy.name, "in", MtlxDataTypes.Vector4);
                    clipHxy.AddPortString("channels", MtlxDataTypes.String, "xy");

                    var clipHzw = graph.AddNode("ClipPositionHomoZW", MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(clipH.name, clipHzw.name, "in", MtlxDataTypes.Vector4);
                    clipHzw.AddPortString("channels", MtlxDataTypes.String, "zw");

                    var scaled = graph.AddNode("ClipPositionHomoXYScaled", MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(clipHxy.name, scaled.name, "in1", MtlxDataTypes.Vector2);
                    scaled.AddPortValue("in2", MtlxDataTypes.Float, new float[] { .5f, .5f });

                    var offset = graph.AddNode("ClipPositionHomoXYOffset", MtlxNodeTypes.Add, MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(scaled.name, offset.name, "in1", MtlxDataTypes.Vector2);
                    offset.AddPortValue("in2", MtlxDataTypes.Float, new float[] { .5f, .5f });

                    // REALLY? Combining two vec2's should work in the spec, but it fails in the reference implementation...
                    // so we're really going to fully decompose and rebuild using floats.
                    var x = graph.AddNode("ClipPositionRawX", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                    var y = graph.AddNode("ClipPositionRawY", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                    var z = graph.AddNode("ClipPositionRawZ", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                    var w = graph.AddNode("ClipPositionRawW", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                    x.AddPortString("channels", MtlxDataTypes.String, "x");
                    y.AddPortString("channels", MtlxDataTypes.String, "y");
                    z.AddPortString("channels", MtlxDataTypes.String, "x");
                    w.AddPortString("channels", MtlxDataTypes.String, "y");
                    graph.AddPortAndEdge(offset.name, x.name, "in", MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(offset.name, y.name, "in", MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(clipHzw.name, z.name, "in", MtlxDataTypes.Vector2);
                    graph.AddPortAndEdge(clipHzw.name, w.name, "in", MtlxDataTypes.Vector2);


                    graph.AddPortAndEdge(x.name, raw.name, "in1", MtlxDataTypes.Float);
                    graph.AddPortAndEdge(y.name, raw.name, "in2", MtlxDataTypes.Float);
                    graph.AddPortAndEdge(z.name, raw.name, "in3", MtlxDataTypes.Float);
                    graph.AddPortAndEdge(w.name, raw.name, "in4", MtlxDataTypes.Float);
                }
                return raw;
            }

            QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.ScreenParams, MtlxDataTypes.Vector4, graph);
            if (graph.GetOrAddNode("ScreenWidth", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, out var screenWidth))
            {
                screenWidth.AddPortString("channels", MtlxDataTypes.String, "x");
                graph.AddPortAndEdge(PolySpatialShaderGlobals.ScreenParams, screenWidth.name, "in", MtlxDataTypes.Vector4);
            }
            if (graph.GetOrAddNode("ScreenHeight", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, out var screenHeight))
            {
                screenHeight.AddPortString("channels", MtlxDataTypes.String, "y");
                graph.AddPortAndEdge(PolySpatialShaderGlobals.ScreenParams, screenHeight.name, "in", MtlxDataTypes.Vector4);
            }

            if (graph.GetOrAddNode("NDCPosition", MtlxNodeTypes.Divide, MtlxDataTypes.Vector4, out var NDCPos)) // clip / screenDim
            {
                if (graph.GetOrAddNode("ScreenDimension4", MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, out var screenDim))
                {
                    graph.AddPortAndEdge("ScreenWidth", screenDim.name, "in1", MtlxDataTypes.Float);
                    graph.AddPortAndEdge("ScreenHeight", screenDim.name, "in2", MtlxDataTypes.Float);
                    screenDim.AddPortValue("in3", MtlxDataTypes.Float, new float[] { 1 });
                    screenDim.AddPortValue("in4", MtlxDataTypes.Float, new float[] { 1 });
                }

                graph.AddPortAndEdge(clipPos.name, NDCPos.name, "in1", MtlxDataTypes.Vector4);
                graph.AddPortAndEdge(screenDim.name, NDCPos.name, "in2", MtlxDataTypes.Vector4);
            }
            if (type == ScreenSpaceType.Default)
                return NDCPos;

            // ndc*2-1
            if (graph.GetOrAddNode("ScreenPositionCenter", MtlxNodeTypes.Subtract, MtlxDataTypes.Vector4, out var center))
            {
                var doubleCenterNode = graph.AddNode("ScreenPositionCenteredPreMult", MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4);
                graph.AddPortAndEdge(NDCPos.name, doubleCenterNode.name, "in1", MtlxDataTypes.Vector4);
                doubleCenterNode.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 2 });

                graph.AddPortAndEdge(doubleCenterNode.name, center.name, "in1", MtlxDataTypes.Vector4);
                center.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 1 });
            }
            if (type == ScreenSpaceType.Center)
                return center;

            // (frac(center.x*screenDim.x/screenDim.y, center.y), center.z, center.w)
            if (graph.GetOrAddNode("ScreenPositionTiled", MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, out var tiled))
            {
                var cx = graph.AddNode("ScreenPositionCenterX", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                var cy = graph.AddNode("ScreenPositionCenterY", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                var fzw = graph.AddNode("ScreenPositionCenterZW", MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2);

                cx.AddPortString("channels", MtlxDataTypes.String, "x");
                cy.AddPortString("channels", MtlxDataTypes.String, "y");
                fzw.AddPortString("channels", MtlxDataTypes.String, "xy");


                graph.AddPortAndEdge(center.name, cx.name, "in", MtlxDataTypes.Vector4);
                graph.AddPortAndEdge(center.name, cy.name, "in", MtlxDataTypes.Vector4);
                graph.AddPortAndEdge(center.name, fzw.name, "in", MtlxDataTypes.Vector4);

                var aspect = graph.AddNode("ScreenPositionAspectRatio", MtlxNodeTypes.Divide, MtlxDataTypes.Float); // aspect = screenWidth/screenHeight
                graph.AddPortAndEdge("ScreenWidth", aspect.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge("ScreenHeight", aspect.name, "in2", MtlxDataTypes.Float);

                var csx = graph.AddNode("ScreenPositionCenterScaledX", MtlxNodeTypes.Multiply, MtlxDataTypes.Float); // x * aspect
                graph.AddPortAndEdge(cx.name, csx.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(aspect.name, csx.name, "in2", MtlxDataTypes.Float);

                var cxy = graph.AddNode("ScreenPositionCenterScaledXY", MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2); // vector2(x*aspect, y)
                graph.AddPortAndEdge(csx.name, cxy.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(cy.name, cxy.name, "in2", MtlxDataTypes.Float);

                // fract => x - floor(x)
                var csxy = graph.AddNode("ScreenPositionCenterFloorXY", MtlxNodeTypes.Floor, MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(cxy.name, csxy.name, "in", MtlxDataTypes.Vector2);

                var fxy = graph.AddNode("ScreenPositionCenterSubXY", MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(cxy.name, fxy.name, "in1", MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(csxy.name, fxy.name, "in2", MtlxDataTypes.Vector2);


                var x = graph.AddNode("ScreenPositionTiledX", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                var y = graph.AddNode("ScreenPositionTiledY", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                var z = graph.AddNode("ScreenPositionTiledZ", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                var w = graph.AddNode("ScreenPositionTiledW", MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                x.AddPortString("channels", MtlxDataTypes.String, "x");
                y.AddPortString("channels", MtlxDataTypes.String, "y");
                z.AddPortString("channels", MtlxDataTypes.String, "x");
                w.AddPortString("channels", MtlxDataTypes.String, "y");
                graph.AddPortAndEdge(fxy.name, x.name, "in", MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(fxy.name, y.name, "in", MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(fzw.name, z.name, "in", MtlxDataTypes.Vector2);
                graph.AddPortAndEdge(fzw.name, w.name, "in", MtlxDataTypes.Vector2);


                graph.AddPortAndEdge(x.name, tiled.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(y.name, tiled.name, "in2", MtlxDataTypes.Float);
                graph.AddPortAndEdge(z.name, tiled.name, "in3", MtlxDataTypes.Float);
                graph.AddPortAndEdge(w.name, tiled.name, "in4", MtlxDataTypes.Float);
            }
            return tiled;
        }


        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is not ScreenPositionNode snode)
                return;

#if DISABLE_MATERIALX_EXTENSIONS
            var nodeData = SetupScreenSpace(snode.screenSpaceType, graph);
            externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, nodeData.name);
#else
            // For pixel coordinates, RealityKit provides a custom node.
            if (snode.screenSpaceType == ScreenSpaceType.Pixel)
            {
                var nodeData = QuickNode.NaryOp(
                    MtlxNodeTypes.RealityKitSurfaceScreenPosition, node, graph, externals, "ScreenPosition");
                nodeData.outputName = "screenPosition";
                return;
            }
            
            // Start with the base screen position: the object position transformed by model-view/projection.
            NodeDef baseDef = new(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3, new()
                        {
                            ["space"] = new StringInputDef("object"),
                        }),
                    }),
                    ["mat"] = new PerStageInputDef(
                        new InlineInputDef(
                            MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                            MtlxDataTypes.Matrix44, new(), "modelToView"),
                        new InlineInputDef(
                            MtlxNodeTypes.RealityKitSurfaceModelToView,
                            MtlxDataTypes.Matrix44, new(), "modelToView")),
                }),
                ["mat"] = new PerStageInputDef(
                    new InlineInputDef(
                        MtlxNodeTypes.RealityKitGeometryModifierViewToProjection,
                        MtlxDataTypes.Matrix44, new(), "viewToProjection"),
                    new InlineInputDef(
                        MtlxNodeTypes.RealityKitSurfaceViewToProjection,
                        MtlxDataTypes.Matrix44, new(), "viewToProjection")),
            });
            if (snode.screenSpaceType == ScreenSpaceType.Raw)
            {
                // Refer to:
                // https://github.cds.internal.unity3d.com/unity/unity/blob/c70d8d41fa33940eff956ece3020687378e5be1a/Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl#L20
                QuickNode.CompoundOp(node, graph, externals, sgContext, "ScreenPosition", new()
                {
                    ["Base"] = baseDef,
                    ["BaseW"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("Base"),
                        ["channels"] = new StringInputDef("w"),
                    }),
                    ["OutXY"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                            {
                                ["in"] = new InternalInputDef("Base"),
                                ["channels"] = new StringInputDef("xy"),
                            }),
                            ["in2"] = new InternalInputDef("BaseW"),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    }),
                    ["Out"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InternalInputDef("OutXY"),
                            ["channels"] = new StringInputDef("x"),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InternalInputDef("OutXY"),
                            ["channels"] = new StringInputDef("y"),
                        }),
                        ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InternalInputDef("Base"),
                            ["channels"] = new StringInputDef("z"),
                        }),
                        ["in4"] = new InternalInputDef("BaseW"),
                    }),
                });
                return;
            }

            // Divide by w (and clear zw) to get the Center position.
            var scale = (snode.screenSpaceType == ScreenSpaceType.Default) ? 0.5f : 1.0f;
            NodeDef centerDef = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InternalInputDef("Base"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("Base"),
                        ["channels"] = new StringInputDef("w"),
                    }),
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, scale, scale, 0.0f, 0.0f),
            });

            QuickNode.CompoundOp(node, graph, externals, sgContext, "ScreenPosition", new()
            {
                ["Base"] = baseDef,
                ["Out"] = snode.screenSpaceType switch
                {
                    // Take the fraction of Center to get the Tiled position.
                    ScreenSpaceType.Tiled => new(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = new InlineInputDef(centerDef),
                    }),
                    // Remap Center to [0, 1] to get the Default position.
                    ScreenSpaceType.Default => new(MtlxNodeTypes.Add, MtlxDataTypes.Vector4, new()
                    {
                        ["in1"] = new InlineInputDef(centerDef),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 0.5f, 0.5f, 0.0f, 0.0f),
                    }),
                    ScreenSpaceType.Center => centerDef,
                    _ => throw new NotSupportedException($"Unknown screen space type {snode.screenSpaceType}"),
                },
            });
#endif
        }
    }
}
