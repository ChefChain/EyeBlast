using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class SwizzleAdapter : ANodeAdapter<SwizzleNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is SwizzleNode snode)
            {
                var portMap = new Dictionary<string, string>();
                portMap.Add("In", "in");

                var value = snode.maskInput;
                var inputType = SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node));

                // Special case for float -> float, which MaterialX doesn't handle.
                if (inputType == MtlxDataTypes.Float && value.Length == 1)
                {
                    QuickNode.NaryOp(MtlxNodeTypes.Dot, node, graph, externals, "Swizzle", portMap);
                    return;
                }

                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Swizzle, node, graph, externals, "Swizzle", portMap);

                StringBuilder sb = new();
                if (MtlxDataTypes.IsColor(inputType) && value.Any(c => "xyzw".Contains(c)))
                {
                    foreach (char c in value)
                    {
                        switch(c)
                        {
                            case 'x': sb.Append('r'); break;
                            case 'y': sb.Append('g'); break;
                            case 'z': sb.Append('b'); break;
                            case 'w': sb.Append('a'); break;
                        }
                    }
                    value = sb.ToString();
                }
                else if (MtlxDataTypes.IsVector(inputType) && value.Any(c => "rgba".Contains(c)))
                {
                    foreach (char c in value)
                    {
                        switch (c)
                        {
                            case 'r': sb.Append('x'); break;
                            case 'g': sb.Append('y'); break;
                            case 'b': sb.Append('z'); break;
                            case 'a': sb.Append('w'); break;
                        }
                    }
                    value = sb.ToString();
                }

                nodeData.AddPortString("channels", MtlxDataTypes.String, value);
            }
        }
    }
}
