
using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TransformationMatrixAdapter : ANodeAdapter<TransformationMatrixNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var matrixExpr = ((TransformationMatrixNode)node).matrixType switch
            {
                UnityMatrixType.Model => "unity_ObjectToWorld",
                UnityMatrixType.InverseModel => "unity_WorldToObject",
                UnityMatrixType.View => "UNITY_MATRIX_V",
                UnityMatrixType.InverseView => "UNITY_MATRIX_I_V",
                UnityMatrixType.Projection => "UNITY_MATRIX_P",
                UnityMatrixType.InverseProjection => "UNITY_MATRIX_I_P",
                UnityMatrixType.ViewProjection => "UNITY_MATRIX_VP",
                UnityMatrixType.InverseViewProjection => "UNITY_MATRIX_I_VP",
                var otherType => throw new NotSupportedException($"Unknown matrix type {otherType}"),
            };
            QuickNode.CompoundOp(node, graph, externals, sgContext, "TransformationMatrix", $"Out = {matrixExpr};");
        }
    }
}