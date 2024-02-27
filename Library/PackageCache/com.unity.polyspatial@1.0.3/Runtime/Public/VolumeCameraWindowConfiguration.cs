using UnityEngine;

namespace Unity.PolySpatial
{
    [CreateAssetMenu(fileName = "VolumeCameraWindowConfiguration", menuName = "PolySpatial/Volume Camera Window Configuration")]
    public class VolumeCameraWindowConfiguration : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Whether the camera should restrict the rendered content to objects within its bounding box or be unbounded.")]
        VolumeCamera.PolySpatialVolumeCameraMode m_Mode = VolumeCamera.PolySpatialVolumeCameraMode.Bounded;

        [SerializeField]
        [Tooltip("The dimensions that are mapped to a unit cube in the destination view. Only available when the mode is set to Bounded.")]
        Vector3 m_OutputDimensions = Vector3.one;

        // This will lock the current ratio and scale the camera uniformly.
        // ReSharper disable once NotAccessedField.Local
        [SerializeField]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        bool m_IsUniformScale;
#pragma warning restore CS0414

        /// <summary>
        /// The mode (Bounded, Unbounded) of the camera's output. This value cannot be changed at runtime and can only be modified on the asset.
        /// </summary>
        public VolumeCamera.PolySpatialVolumeCameraMode Mode
        {
            get => m_Mode;
        }

        /// <summary>
        /// The dimensions of the camera's output. This value cannot be changed at runtime and can only be modified on the asset.
        /// </summary>
        public Vector3 Dimensions
        {
            get => m_OutputDimensions;
        }

        internal VolumeCamera.PolySpatialVolumeCameraMode PrivateMode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        internal Vector3 PrivateDimensions
        {
            get => m_OutputDimensions;
            set => m_OutputDimensions = value;
        }
    }
}
