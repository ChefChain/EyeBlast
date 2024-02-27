using System;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    // A "tag" component indicate the corresponding GO should show a hover effect. To function, the GO must also have a
    // MeshRenderer (to display the hover effect on) and a Collider (against which the view ray can intersect).
    [Obsolete("PolySpatialHoverEffect has been deprecated.  Use VisionOSHoverEffect instead (UnityUpgradable) -> VisionOSHoverEffect", true)]
    public class PolySpatialHoverEffect : MonoBehaviour
    {
    }
}
