
using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TransformAdapter : ANodeAdapter<TransformNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Transform", GetTransformExpr((TransformNode)node));            
        }

        string GetTransformExpr(TransformNode node)
        {
            var from = node.spaceTransform.from;
            var to = node.spaceTransform.to;

            // Assume that world space and "absolute world space" are the same
            // (I believe it's only HDRP that uses camera-relative world space).
            if (from == CoordinateSpace.AbsoluteWorld)
                from = CoordinateSpace.World;
            if (to == CoordinateSpace.AbsoluteWorld)
                to = CoordinateSpace.World;

            // Handle the cases for which we have direct transformations.
            var directExpr = GetDirectTransformExpr(node, from, to, "In", "Out");
            if (directExpr != null)
                return directExpr;

            // Failing that, just convert via world space.
            var toWorldExpr = GetDirectTransformExpr(node, from, CoordinateSpace.World, "In", "float3 inWS");
            var fromWorldExpr = GetDirectTransformExpr(node, CoordinateSpace.World, to, "inWS", "Out");
            return $"{toWorldExpr} {fromWorldExpr}";
        }

        string GetDirectTransformExpr(
            TransformNode node, CoordinateSpace from, CoordinateSpace to, string input, string output)
        {
            // Handle the identity transformation.
            if (from == to)
                return $"{output} = {input};";

            switch (from)
            {
                case CoordinateSpace.Object:
                    if (to == CoordinateSpace.World)
                        return GetMatrixMulExpr(node, "unity_ObjectToWorld", "unity_WorldToObject", input, output);
                    break;
                
                case CoordinateSpace.World:
                    switch (to)
                    {
                        case CoordinateSpace.Object:
                            return GetMatrixMulExpr(node, "unity_WorldToObject", "unity_ObjectToWorld", input, output);
                        
                        case CoordinateSpace.View:
                            return GetMatrixMulExpr(node, "UNITY_MATRIX_V", "UNITY_MATRIX_I_V", input, output);
                        
                        case CoordinateSpace.Tangent:
                            return GetMatrixMulExpr(
                                node, "polySpatial_WorldToTangent", "polySpatial_TangentToWorld", input, output);

                        case CoordinateSpace.Screen:
                        {
                            // Refer to:
                            // https://github.cds.internal.unity3d.com/unity/unity/blob/60b04ed503be439a6a9a8387f374abc47f198144/Packages/com.unity.shadergraph/Editor/Data/Util/SpaceTransformUtil.cs#L382
                            var uvExpr = node.conversionType switch
                            {
                                ConversionType.Position => $@"
float4 inPS = mul(UNITY_MATRIX_VP, float4({input}, 1.0));
float3 uv = inPS.xyz / inPS.w;",
                                _ => $@"
float3 uv = normalize(mul(UNITY_MATRIX_VP, float4({input}, 0.0)).xyz);",
                            };
                            return $"{uvExpr} {output} = float3(uv.xy * 0.5 + 0.5, uv.z);";
                        }
                    }
                    break;
                
                case CoordinateSpace.View:
                    if (to == CoordinateSpace.World)
                        return GetMatrixMulExpr(node, "UNITY_MATRIX_I_V", "UNITY_MATRIX_V", input, output);
                    break;
                
                case CoordinateSpace.Tangent:
                    if (to == CoordinateSpace.World)
                    {
                        return GetMatrixMulExpr(
                            node, "polySpatial_TangentToWorld", "polySpatial_WorldToTangent", input, output);
                    }
                    break;

                case CoordinateSpace.Screen:
                    if (to == CoordinateSpace.World)
                    {
                        // Refer to:
                        // https://github.cds.internal.unity3d.com/unity/unity/blob/60b04ed503be439a6a9a8387f374abc47f198144/Packages/com.unity.shadergraph/Editor/Data/Util/SpaceTransformUtil.cs#L406
                        return node.conversionType switch
                        {
                            ConversionType.Position => $@"
float4 hpositionWS = mul(UNITY_MATRIX_I_VP, float4({input}.xy * 2.0 - 1.0, {input}.z, 1.0));
{output} = hpositionWS.xyz / hpositionWS.w;",
                            _ => $@"
{output} = mul(UNITY_MATRIX_I_VP, float4({input}.xy * 2.0 - 1.0, {input}.z, 0.0)).xyz;",
                        };
                    }
                    break;
            }

            return null;
        }

        string GetMatrixMulExpr(TransformNode node, string matrix, string inverse, string input, string output)
        {
            var result = node.conversionType switch
            {
                ConversionType.Direction => $"normalize(mul({matrix}, float4({input}, 0.0)).xyz)",
                ConversionType.Normal => $"normalize(mul(float4({input}, 0.0), {inverse}).xyz)",
                ConversionType.Position => $"mul({matrix}, float4({input}, 1.0)).xyz",
                _ => throw new NotSupportedException($"Unknown conversion type {node.conversionType}"),
            };
            return $"{output} = {result};";
        }
    }
}
