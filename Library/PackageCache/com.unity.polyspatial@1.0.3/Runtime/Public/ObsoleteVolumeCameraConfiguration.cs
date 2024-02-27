using System;
using UnityEngine;

namespace Unity.PolySpatial
{
    [Obsolete("VolumeCameraConfiguration has been renamed to VolumeCameraWindowConfiguration (UnityUpgradable) -> VolumeCameraWindowConfiguration", true)]
    public partial class VolumeCameraConfiguration
    {
        public VolumeCamera.PolySpatialVolumeCameraMode Mode
        {
            get { return default; }
        }

        public Vector3 Dimensions
        {
            get { return default; }
        }
    }
}
