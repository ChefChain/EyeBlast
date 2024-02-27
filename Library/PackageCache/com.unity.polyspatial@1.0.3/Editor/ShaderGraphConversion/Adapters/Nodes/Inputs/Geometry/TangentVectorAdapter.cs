namespace UnityEditor.ShaderGraph.MaterialX
{
    class TangentAdapter : GeometryVectorAdapter<TangentVectorNode>
    {
        protected override string Hint => "Tangent";
        protected override string NodeType => MtlxNodeTypes.GeomTangent;
    }
}
