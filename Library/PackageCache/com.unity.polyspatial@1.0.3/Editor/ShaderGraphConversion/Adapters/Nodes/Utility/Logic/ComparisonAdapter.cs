
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class ComparisonAdapter : ANodeAdapter<ComparisonNode>
    {
        // We need to simulate a Predicate != 0 to handle correct behavior.
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ComparisonNode cnode)
            {
                var portMap = new Dictionary<string, string>();
                string nodeType = null;
                bool flip = false;

                portMap.Add("A", "value1");
                portMap.Add("B", "value2");

                switch (cnode.comparisonType)
                {
                    case ComparisonType.Equal:
                        nodeType = MtlxNodeTypes.IfEqual;
                        break;
                    case ComparisonType.NotEqual:
                        nodeType = MtlxNodeTypes.IfEqual;
                        flip = true;
                        break;

                    case ComparisonType.Greater:
                        nodeType = MtlxNodeTypes.IfGreater;
                        break;
                    case ComparisonType.LessOrEqual:
                        nodeType = MtlxNodeTypes.IfGreater;
                        flip = true;
                        break;

                    case ComparisonType.GreaterOrEqual:
                        nodeType = MtlxNodeTypes.IfGreaterOrEqual;
                        break;
                    case ComparisonType.Less:
                        nodeType = MtlxNodeTypes.IfGreaterOrEqual;
                        flip = true;
                        break;
                }

                var nodeData =QuickNode.NaryOp(nodeType, node, graph, externals, "Comparison", portMap);
                nodeData.AddPortValue("in1", MtlxDataTypes.Float, new float[] { flip ? 0 : 1 });
                nodeData.AddPortValue("in2", MtlxDataTypes.Float, new float[] { flip ? 1 : 0 });
            }
        }
    }
}
