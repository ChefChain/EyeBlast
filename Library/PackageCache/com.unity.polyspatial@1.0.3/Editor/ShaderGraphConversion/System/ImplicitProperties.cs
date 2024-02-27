using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal static class MtlxImplicitProperties
    {
        internal const string ObjectPosition = "ObjectPosition"; // vec3
        internal const string ObjectScale = "ObjectScale"; // vec3
        internal const string ObjectBoundsMin = "ObjectBoundsMin"; // vec3
        internal const string ObjectBoundsMax = "ObjectBoundsMax"; // vec3
        internal const string ObjectBoundsSize = "ObjectBoundsSize"; // vec3

        internal const string TransformModel = "TransformModel";             // mat4, model to world
    }
}
