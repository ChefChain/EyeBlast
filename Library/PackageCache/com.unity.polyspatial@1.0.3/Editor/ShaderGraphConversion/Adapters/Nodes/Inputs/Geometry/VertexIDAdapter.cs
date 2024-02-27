namespace UnityEditor.ShaderGraph.MaterialX
{
    class VertexIDAdapter : ANodeAdapter<VertexIDNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }
        
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.NaryOp(
                MtlxNodeTypes.RealityKitGeometryModifierVertexID, node, graph, externals, "VertexID");
            nodeData.datatype = MtlxDataTypes.Integer;
            nodeData.outputName = "vertexId";
        }
    }
}