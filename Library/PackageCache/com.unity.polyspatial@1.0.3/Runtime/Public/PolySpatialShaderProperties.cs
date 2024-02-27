using UnityEngine;

namespace Unity.PolySpatial
{
    // Shader properties specific to PolySpatial, used in the editor for node generation
    // (such as for the lighting node) and also available for direct access by users.
    public static class PolySpatialShaderProperties
    {
        public const string VolumeToWorld = "polySpatial_VolumeToWorld";

        public const string Lightmap = "polySpatial_Lightmap";
        public const string LightmapInd = "polySpatial_LightmapInd";
        public const string LightmapST = "polySpatial_LightmapST";

        public const string SHAr = "polySpatial_SHAr";
        public const string SHAg = "polySpatial_SHAg";
        public const string SHAb = "polySpatial_SHAb";
        public const string SHBr = "polySpatial_SHBr";
        public const string SHBg = "polySpatial_SHBg";
        public const string SHBb = "polySpatial_SHBb";
        public const string SHC = "polySpatial_SHC";

        public const int ReflectionProbeCount = 2;
        public const string ReflectionProbeTexturePrefix = "polySpatial_SpecCube";
        public const string ReflectionProbeWeightPrefix = "polySpatial_SpecCubeWeight";

        internal static readonly int VolumeToWorldID = Shader.PropertyToID(VolumeToWorld);

        internal static readonly int LightmapID = Shader.PropertyToID(Lightmap);
        internal static readonly int LightmapIndID = Shader.PropertyToID(LightmapInd);
        internal static readonly int LightmapSTID = Shader.PropertyToID(LightmapST);

        internal static readonly int SHArID = Shader.PropertyToID(SHAr);
        internal static readonly int SHAgID = Shader.PropertyToID(SHAg);
        internal static readonly int SHAbID = Shader.PropertyToID(SHAb);
        internal static readonly int SHBrID = Shader.PropertyToID(SHBr);
        internal static readonly int SHBgID = Shader.PropertyToID(SHBg);
        internal static readonly int SHBbID = Shader.PropertyToID(SHBb);
        internal static readonly int SHCID = Shader.PropertyToID(SHC);

        internal static readonly int[] ReflectionProbeTextureIDs =
            GetReflectionProbePropertyIDs(ReflectionProbeTexturePrefix);
        internal static readonly int[] ReflectionProbeWeightIDs =
            GetReflectionProbePropertyIDs(ReflectionProbeWeightPrefix);
        
        static int[] GetReflectionProbePropertyIDs(string prefix)
        {
            var propertyIDs = new int[ReflectionProbeCount];
            for (var i = 0; i < ReflectionProbeCount; ++i)
            {
                propertyIDs[i] = Shader.PropertyToID(prefix + i);
            }
            return propertyIDs;
        }
    }
}