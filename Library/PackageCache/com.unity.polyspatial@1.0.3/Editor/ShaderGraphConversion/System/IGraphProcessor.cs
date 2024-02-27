namespace UnityEditor.ShaderGraph.MaterialX
{
    internal interface IGraphProcessor
    {
        string FileExtension { get; }
        bool IsEnabled();
        bool GenerateIntermediateFile();
        string ProcessGraph(MtlxGraphData graph);
    }
}
