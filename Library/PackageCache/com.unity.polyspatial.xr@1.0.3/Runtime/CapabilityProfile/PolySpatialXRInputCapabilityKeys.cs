using Unity.XR.CoreUtils.Capabilities;

namespace Unity.PolySpatial.XR.Capabilities
{
    /// <summary>
    /// Class that defines the PolySpatial XR capability keys.
    /// </summary>
    internal static class PolySpatialXRInputCapabilityKeys
    {
        /// <summary>
        /// The World Touch Input capability.
        /// </summary>
        [CustomCapabilityKey(100)]
        public const string WorldTouch = "PolySpatialXR/World Touch";
    }
}
