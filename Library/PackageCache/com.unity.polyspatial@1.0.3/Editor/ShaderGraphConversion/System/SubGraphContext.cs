namespace UnityEditor.ShaderGraph.MaterialX
{
    // Represents the subgraph context active when building an instance of a node.  For the top-level
    // shader graph, the context is null.  Each time a SubGraphNode is built, a new context is created with
    // a reference to that node and to the context supplied to BuildInstance (that is, the parent context).
    // Using this linked list of contexts, node adapters may rise through the subgraph hierarchy in order
    // to resolve property values passed from parent graphs to subgraphs.
    internal class SubGraphContext
    {
        internal SubGraphNode Node { get; private set; }
        internal SubGraphContext Parent { get; private set; }

        internal SubGraphContext(SubGraphNode node, SubGraphContext parent)
        {
            Node = node;
            Parent = parent;
        }
    }
}