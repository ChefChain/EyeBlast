#if INCLUDE_UNITY_XR_HANDS || PACKAGE_DOCS_GENERATION

using System;
using UnityEngine.XR.Hands;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Defines extension methods for platform-specific hand data.
    /// </summary>
    public static class VisionOSHandExtensions
    {
        /// <summary>
        /// Gets the pose of the joint, if available, but without the
        /// Unity-defined change to the rotation to make the reported
        /// rotation cross-platform.
        /// </summary>
        /// <param name="joint">
        /// The joint this extension method extends. To call this extension
        /// method, write it like
        /// <c>myJoint.TryGetApplyRotation(out var rotation)</c>.
        /// </param>
        /// <param name="rotation">
        /// If this method returns <see langword="true"/>, this will be
        /// populated with the Apple-defined rotation for the given joint, but
        /// still converted to Unity space.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful and the rotation was available,
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetVisionOSRotation(this XRHandJoint joint, out Quaternion rotation)
        {
            var rotations = joint.handedness == Handedness.Left ? s_LeftHandRotations : s_RightHandRotations;
            var nullableRotation = rotations[joint.id.ToIndex()];
            rotation = nullableRotation ?? Quaternion.identity;
            return nullableRotation.HasValue;
        }

        internal static Quaternion?[] GetVisionOSRotations(Handedness handedness)
         => handedness == Handedness.Left ? s_LeftHandRotations : s_RightHandRotations;

        static Quaternion?[] s_LeftHandRotations = new Quaternion?[XRHandJointID.EndMarker.ToIndex()];
        static Quaternion?[] s_RightHandRotations = new Quaternion?[XRHandJointID.EndMarker.ToIndex()];
    }
}

#endif // INCLUDE_UNITY_XR_HANDS || PACKAGE_DOCS_GENERATION
