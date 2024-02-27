namespace UnityEditor.ShaderGraph.MaterialX
{
    class BitangentAdapter : GeometryVectorAdapter<BitangentVectorNode>
    {
        protected override string Hint => "Bitangent";
        protected override string NodeType => MtlxNodeTypes.GeomBitangent;
    }
}
