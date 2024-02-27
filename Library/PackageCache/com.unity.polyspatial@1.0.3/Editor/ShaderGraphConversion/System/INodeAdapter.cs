using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal interface ISurfaceAdapter
    {
        bool IsBlockSupported(BlockNode node);
        void BuildContextInstance(GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals);
    }

    internal interface INodeAdapter
    {
        Type GetSupportedNodeType();
        bool IsNodeSupported(AbstractMaterialNode node);
        string SupportDetails(AbstractMaterialNode node);

        void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext);
    }

    internal abstract class ANodeAdapter<T> : INodeAdapter where T : AbstractMaterialNode
    {
        // TODO: Provide a "this is partially supported" hook so that we can provide more feedback on badges.
        public Type GetSupportedNodeType() => typeof(T);
        public virtual bool IsNodeSupported(AbstractMaterialNode node) => node is T;
        public virtual string SupportDetails(AbstractMaterialNode node) => "";

        public virtual void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            BuildInstance(node, graph, externals);
        }

        public virtual void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            throw new NotImplementedException();
        }
    }
}
