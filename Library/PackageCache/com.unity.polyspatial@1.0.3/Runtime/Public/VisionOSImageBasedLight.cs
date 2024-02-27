using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    public class VisionOSImageBasedLight : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The first image source, if any.")]
        Texture m_FirstSource;

        [SerializeField]
        [Tooltip("The second image source, if any.  Will be blended with the first if both are present.")]
        Texture m_SecondSource;

        [SerializeField]
        [Tooltip("The blend parameter, if both images are specified (0 for fully first, 1 for fully second).")]
        float m_Blend;

        [SerializeField]
        [Tooltip("Whether or not the light inherits the rotation of the object's transform.")]
        bool m_InheritsRotation;

        [SerializeField]
        [Tooltip("The power of two by which to scale the light's intensity.")]
        float m_IntensityExponent;

        /// <summary>
        /// The first image source, if any.
        /// </summary>
        public Texture FirstSource
        {
            get => m_FirstSource;
            set
            {
                m_FirstSource = value;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// The second image source, if any.  If both images are present, they will be
        /// blended together according to the Blend property.
        /// </summary>
        public Texture SecondSource
        {
            get => m_SecondSource;
            set
            {
                m_SecondSource = value;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// The blend parameter to use if both image sources are specified (e.g., 0.25 to include 25% of the first and
        /// 75% of the second).  Note that this value is not clamped to [0, 1]; similar to Mathf.LerpUnclamped, values
        /// outside [0, 1] will amplify the difference between the two sources.
        /// </summary>
        public float Blend
        {
            get => m_Blend;
            set
            {
                m_Blend = value;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// If true, the image based light will inherit the world space rotation of the GameObject containing it.
        /// </summary>
        public bool InheritsRotation
        {
            get => m_InheritsRotation;
            set
            {
                m_InheritsRotation = value;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// The power of two to use as the image based light's intensity.  For example, a value of 0.0 yields a linear
        /// intensity of 1.0; a value of 1.0 yields an intensity of 2.0, a value of 2.0 yields 4.0, etc.
        /// </summary>
        public float IntensityExponent
        {
            get => m_IntensityExponent;
            set
            {
                m_IntensityExponent = value;
                this.MarkDirty();
            }
        }
    }
}