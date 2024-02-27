using System;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    [Obsolete("PolySpatialImageBasedLight has been deprecated.  Use VisionOSImageBasedLight instead (UnityUpgradable) -> VisionOSImageBasedLight", true)]
    public class PolySpatialImageBasedLight : MonoBehaviour
    {
        public Texture FirstSource { get; set; }

        public Texture SecondSource { get; set; }

        public float Blend { get; set; }

        public bool InheritsRotation { get; set; }

        public float IntensityExponent { get; set; }
    }
}
