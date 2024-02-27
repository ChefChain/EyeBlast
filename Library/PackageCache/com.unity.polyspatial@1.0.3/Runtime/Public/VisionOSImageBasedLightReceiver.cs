using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    [Tooltip("Image Based Light Receiver")]
    public class VisionOSImageBasedLightReceiver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The image-based light to apply to this object.")]
        VisionOSImageBasedLight m_ImageBasedLight;

        /// <summary>
        /// A reference to the image based light to apply to this object and its descendants.
        /// </summary>
        public VisionOSImageBasedLight ImageBasedLight
        {
            get => m_ImageBasedLight;
            set
            {
                m_ImageBasedLight = value;
                this.MarkDirty();
            }
        }
    }
}
