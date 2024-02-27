using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    // This is a node supported in MaterialX that doesn't have a direct equivalent in Unity shader graph:
    // a left/right split matte that selects/blends between ValueL and ValueR based on the U component of the
    // input UV relative to the provided Center.  It outputs ValueL if U is less than Center, ValueR if U is
    // greater than Center, and blends between ValueL and ValueR if U is within one pixel of Center.
    //
    // We use it in the text SDF shader graph as a workaround for the lack of the fragment derivative functions
    // in MaterialX, because the reference implementation of this node uses those functions:
    // https://github.com/AcademySoftwareFoundation/MaterialX/blob/main/libraries/stdlib/genglsl/mx_aastep.glsl
    // See also the MaterialX spec for a description of the node:
    // https://materialx.org/assets/MaterialX.v1.38.Spec.pdf
    [Title("Utility", "Split LR")]
    class SplitLRNode : CodeFunctionNode
    {
        public SplitLRNode()
        {
            name = "Split LR";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("PolySpatial_SplitLR", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string PolySpatial_SplitLR(
            [Slot(0, Binding.None)] Vector1 ValueL,
            [Slot(1, Binding.None)] Vector1 ValueR,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Center,
            [Slot(3, Binding.MeshUV0)] Vector2 UV,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    $precision afwidth = length($precision2(ddx(UV.x), ddy(UV.x))) * 0.70710678118654757;
    Out = lerp(ValueL, ValueR, smoothstep(Center - afwidth, Center + afwidth, UV.x));
}";
        }
    }
}