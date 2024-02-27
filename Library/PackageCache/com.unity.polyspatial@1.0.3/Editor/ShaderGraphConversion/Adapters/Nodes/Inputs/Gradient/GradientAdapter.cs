using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class GradientAdapter : ANodeAdapter<GradientNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
            // gradients are statically evaluted and have no real functionality except
            // what is available from SampleGradient- meaning we'll solve gradients entirely
            // in the SampleGradientAdapater.
            return true;
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
        }
    }
}
