using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal static class AdapterMap
    {
        private static Dictionary<Type, INodeAdapter> s_nodeMap;
        private static void Init()
        {
            s_nodeMap = new();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(INodeAdapter)).Where(e => !e.IsGenericType))
            {
                var nodeProcessor = (INodeAdapter)Activator.CreateInstance(type);
                if (typeof(AbstractMaterialNode).IsAssignableFrom(nodeProcessor.GetSupportedNodeType()))
                {
                    s_nodeMap.Add(nodeProcessor.GetSupportedNodeType(), nodeProcessor);
                }
                else
                {
                    throw new Exception($"{type}.GetSupportedNodeType() must be a type inherited from AbstractMaterialNode.");
                }
            }
        }
        private static Dictionary<Type, INodeAdapter> NodeMap
        {
            get
            {
                if (s_nodeMap == null)
                    Init();
                return s_nodeMap;
            }
        }

        private static ISurfaceAdapter surfaceAdapter = new UsdPreviewSurfaceAdapter();

        internal static void SetSurfaceAdapter(ISurfaceAdapter adapter)
            => surfaceAdapter = adapter;

        internal static bool IsBlockSupported(BlockNode node)
            => surfaceAdapter.IsBlockSupported(node);

        internal static string GetSupportNotification(AbstractMaterialNode node)
        {
            return NodeMap.ContainsKey(node.GetType()) ? NodeMap[node.GetType()].SupportDetails(node) : "";
        }

        internal static bool IsNodeSupported(AbstractMaterialNode node)
        {
            // a little hack/special case for subgraphs-- since they are a paired concept and the outputnode has no adapter.
            if (node is SubGraphOutputNode && NodeMap.ContainsKey(typeof(SubGraphNode)))
                return true;

            return NodeMap.ContainsKey(node.GetType()) && NodeMap[node.GetType()].IsNodeSupported(node);
        }

        internal static void ProcessInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals,
            SubGraphContext sgContext = null)
        {
            if (IsNodeSupported(node) && node is not SubGraphOutputNode) // TODO: just add an adapter that does nothing?
                NodeMap[node.GetType()].BuildInstance(node, graph, externals, sgContext);
        }

        internal static void ProcessContext(GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            surfaceAdapter.BuildContextInstance(graphData, graph, externals);
        }
    }
}
