using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ChannelMixerAdapter : ANodeAdapter<ChannelMixerNode>
    {
        static string GetFloat3Expr(Vector3 value)
        {
            return $"float3({value.x}, {value.y}, {value.z})";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Channel-Mixer-Node.html
            var channelMixer = ((ChannelMixerNode)node).channelMixer;
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "ChannelMixer", $@"
Out = float3(
    dot(In, {GetFloat3Expr(channelMixer.outRed)}),
    dot(In, {GetFloat3Expr(channelMixer.outGreen)}),
    dot(In, {GetFloat3Expr(channelMixer.outBlue)}));");
        }
    }
}