using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MatrixSplitAdapter : ANodeAdapter<MatrixSplitNode>
    {
        internal static MatrixAxis GetAxis(AbstractMaterialNode node)
        {
            // We need to use reflection for MatrixSplitNode.axis/MatrixConstructionNode.axis because they are private.
            return (MatrixAxis)node.GetType().GetProperty(
                "axis", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(node);
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var dataType = SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node));
            var elementLength = MtlxDataTypes.GetElementLength(dataType);
            var outputType = MtlxDataTypes.GetTypeOfLength(elementLength);

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Matrix-Split-Node.html
            Dictionary<string, NodeDef> nodeDefs = new();
            InputDef matrixDef;
            switch (GetAxis(node))
            {
                case MatrixAxis.Row:
                    nodeDefs["Matrix"] = new(MtlxNodeTypes.Transpose, dataType, new()
                    {
                        ["in"] = new ExternalInputDef("In"),
                    });
                    matrixDef = new InternalInputDef("Matrix");
                    break;

                case MatrixAxis.Column:
                    matrixDef = new ExternalInputDef("In");
                    break;
                
                case var axis:
                    throw new NotSupportedException($"Unknown axis type {axis}");
            }
            for (var i = 0; i < 4; ++i)
            {
                NodeDef outputDef;
                if (i < elementLength)
                {
                    var columnSelector = new float[elementLength];
                    columnSelector[i] = 1.0f;
                    outputDef = new(MtlxNodeTypes.TransformMatrix, outputType, new()
                    {
                        ["in"] = new FloatInputDef(outputType, columnSelector),
                        ["mat"] = matrixDef, 
                    });
                }
                else
                {
                    // Return zero for rows/columns outside the source matrix dimension.
                    outputDef = new(MtlxNodeTypes.Constant, outputType, new()
                    {
                        ["value"] = new FloatInputDef(outputType, new float[elementLength]),
                    });
                }
                nodeDefs[$"M{i}"] = outputDef;
            }
            QuickNode.CompoundOp(node, graph, externals, sgContext, "MatrixSplit", nodeDefs);
        }
    }
}