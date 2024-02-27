using System;
using UnityEngine;

namespace Unity.PolySpatial
{
    [Tooltip("Image Based Light Receiver")]
    [Obsolete("PolySpatialImageBasedLightReceiver has been deprecated.  Use VisionOSImageBasedLightReceiver instead (UnityUpgradable) -> VisionOSImageBasedLightReceiver", true)]
    public class PolySpatialImageBasedLightReceiver : MonoBehaviour
    {
        public VisionOSImageBasedLight ImageBasedLight { get; set; }
    }
}
