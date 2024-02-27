namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalAdapter : GeometryVectorAdapter<NormalVectorNode>
    {
        protected override string Hint => "Normal";
        protected override string NodeType => MtlxNodeTypes.GeomNormal;
    }
}
