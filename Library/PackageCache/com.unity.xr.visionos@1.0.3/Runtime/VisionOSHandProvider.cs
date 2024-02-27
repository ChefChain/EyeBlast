#if INCLUDE_UNITY_XR_HANDS
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

namespace UnityEngine.XR.VisionOS
{
    class VisionOSHandProvider : XRHandSubsystemProvider, IVisionOSProvider
    {
        // TODO: Add HandTracking feature to future versions of AR Foundation
        // Use Body3D as a proxy for now
        const Feature k_HandFeatureProxy = Feature.Body3D;
        internal const string handSubsystemId = "VisionOS-Hands";
        static readonly Quaternion k_LeftHandAligment = Quaternion.AngleAxis(-90f, Vector3.up) * Quaternion.AngleAxis(180f, Vector3.right);
        static readonly Quaternion k_RightHandAlignment = Quaternion.AngleAxis(-90f, Vector3.up);

        XRHandSubsystem.UpdateSuccessFlags m_LastSuccessFlags = XRHandSubsystem.UpdateSuccessFlags.None;

        public AR_Authorization_Type RequiredAuthorizationType => NativeApi.HandTracking.ar_hand_tracking_provider_get_required_authorization_type();
        public bool IsSupported => NativeApi.HandTracking.ar_hand_tracking_provider_is_supported();
        public bool ShouldBeActive => running;
        public IntPtr CurrentProvider { get; private set; } = IntPtr.Zero;

        IntPtr m_LeftHandAnchor = IntPtr.Zero;
        IntPtr m_RightHandAnchor = IntPtr.Zero;

        public VisionOSHandProvider()
        {
            VisionOSProviderRegistration.RegisterProvider(k_HandFeatureProxy, this);
        }

        public override void Start() { }

        public override void Stop() { }

        public override void Destroy()
        {
            VisionOSProviderRegistration.UnregisterProvider(k_HandFeatureProxy, this);
            CurrentProvider = IntPtr.Zero;
            m_LeftHandAnchor = IntPtr.Zero;
            m_RightHandAnchor = IntPtr.Zero;
        }

        public bool TryCreateNativeProvider(Feature features, out IntPtr provider)
        {
            if (!IsSupported)
            {
                Debug.LogWarning("Hand tracking provider is not supported");
                provider = IntPtr.Zero;
                return false;
            }

            if (CurrentProvider != IntPtr.Zero)
            {
                CurrentProvider = IntPtr.Zero;
                m_LeftHandAnchor = IntPtr.Zero;
                m_RightHandAnchor = IntPtr.Zero;
            }

            var handTrackingConfiguration = NativeApi.HandTracking.ar_hand_tracking_configuration_create();
            provider = NativeApi.HandTracking.ar_hand_tracking_provider_create(handTrackingConfiguration);
            CurrentProvider = provider;
            if (provider == IntPtr.Zero)
            {
                Debug.LogWarning("Failed to create hand tracking provider.");
                return false;
            }

            return true;
        }

        public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
        {
            // All joints except palm are supported
            for (var i = 0; i < handJointsInLayout.Length; i++)
            {
                handJointsInLayout[i] = (XRHandJointID)i != XRHandJointID.Palm;
            }
        }

        /// <inheritdoc/>
        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(
            XRHandSubsystem.UpdateType updateType,
            ref Pose leftHandRootPose,
            NativeArray<XRHandJoint> leftHandJoints,
            ref Pose rightHandRootPose,
            NativeArray<XRHandJoint> rightHandJoints)
        {
            if (CurrentProvider == IntPtr.Zero)
                return XRHandSubsystem.UpdateSuccessFlags.None;

            if (m_LeftHandAnchor == IntPtr.Zero)
                m_LeftHandAnchor = NativeApi.HandTracking.ar_hand_anchor_create();

            if (m_RightHandAnchor == IntPtr.Zero)
                m_RightHandAnchor = NativeApi.HandTracking.ar_hand_anchor_create();

            var success = NativeApi.HandTracking.ar_hand_tracking_provider_get_latest_anchors(CurrentProvider, m_LeftHandAnchor, m_RightHandAnchor);

            // get_latest_anchors will return false if we poll too quickly--in that case, return the last valid result
            if (!success)
                return m_LastSuccessFlags;

            m_LastSuccessFlags = XRHandSubsystem.UpdateSuccessFlags.None;
            GetHandData(ref leftHandRootPose, ref m_LastSuccessFlags, leftHandJoints, m_LeftHandAnchor, Handedness.Left);
            GetHandData(ref rightHandRootPose, ref m_LastSuccessFlags, rightHandJoints, m_RightHandAnchor, Handedness.Right);
            return m_LastSuccessFlags;
        }

        void GetHandData(ref Pose rootPose, ref XRHandSubsystem.UpdateSuccessFlags successFlags, NativeArray<XRHandJoint> jointArray, IntPtr handAnchor, Handedness handedness)
        {
            var isTracked = NativeApi.Anchor.ar_trackable_anchor_is_tracked(handAnchor);
            if (!isTracked)
                return;

            var worldTransform = NativeApi.Anchor.ar_anchor_get_origin_from_anchor_transform(handAnchor);
            var convertedMatrix = NativeApi_Types.UnityVisionOS_impl_simd_float4x4_to_float_array(worldTransform);
            var worldMatrix = Marshal.PtrToStructure<FloatArrayToMatrix4x4>(convertedMatrix);
            var wristPosition = worldMatrix.GetPosition();
            var wristRotation = worldMatrix.GetRotation();

            rootPose = new Pose(wristPosition, AlignRotation(wristRotation, handedness));
            successFlags |= handedness == Handedness.Left
                ? XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose
                : XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;

            var handSkeleton = NativeApi.HandTracking.ar_hand_anchor_get_hand_skeleton(handAnchor);
            for (var jointID = XRHandJointID.BeginMarker; jointID < XRHandJointID.EndMarker; jointID++)
            {
                var index = jointID.ToIndex();
                var pose = Pose.identity;
                XRHandJointTrackingState trackingState;
                if (jointID == XRHandJointID.Palm)
                {
                    trackingState = XRHandJointTrackingState.WillNeverBeValid;
                    jointArray[index] = CreateJoint(handedness, trackingState, jointID, pose);
                    continue;
                }

                var jointName = GetJointNameForJointID(jointID);
                var joint = NativeApi.HandSkeleton.ar_hand_skeleton_get_joint_named(handSkeleton, jointName);
                var jointIsTracked = NativeApi.SkeletonJoint.ar_skeleton_joint_is_tracked(joint);
                trackingState = jointIsTracked ? XRHandJointTrackingState.Pose : XRHandJointTrackingState.None;

                if (jointIsTracked)
                {
                    var jointTransformPtr = NativeApi.SkeletonJoint.ar_skeleton_joint_get_anchor_from_joint_transform(joint);
                    convertedMatrix = NativeApi_Types.UnityVisionOS_impl_simd_float4x4_to_float_array(jointTransformPtr);
                    var jointMatrix = Marshal.PtrToStructure<FloatArrayToMatrix4x4>(convertedMatrix);
                    var jointPosition = wristPosition + wristRotation * jointMatrix.GetPosition();
                    var jointRotation = wristRotation * jointMatrix.GetRotation();
                    pose = new Pose(jointPosition, AlignRotation(jointRotation, handedness));

                    var appleRotations = VisionOSHandExtensions.GetVisionOSRotations(handedness);
                    appleRotations[index] = jointRotation;

                    successFlags |= handedness == Handedness.Left
                        ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints
                        : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
                }
                else
                {
                    var appleRotations = VisionOSHandExtensions.GetVisionOSRotations(handedness);
                    appleRotations[index] = null;
                }

                jointArray[index] = CreateJoint(handedness, trackingState, jointID, pose);
            }
        }

        static Quaternion AlignRotation(Quaternion rotation, Handedness handedness)
        {
            return handedness == Handedness.Left
                ? rotation * k_LeftHandAligment
                : rotation * k_RightHandAlignment;
        }

        static XRHandJoint CreateJoint(Handedness handedness, XRHandJointTrackingState trackingState, XRHandJointID id, Pose pose)
        {
#if INCLUDE_UNITY_XR_HANDS_1_1
            return XRHandProviderUtility.CreateJoint(handedness, trackingState, id, pose);
#else
            return XRHandProviderUtility.CreateJoint(trackingState, id, pose);
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = handSubsystemId,
                providerType = typeof(VisionOSHandProvider)
            };

            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        static AR_Skeleton_Joint_Name GetJointNameForJointID(XRHandJointID jointID)
        {
            switch (jointID)
            {
                case XRHandJointID.Invalid:
                    throw new ArgumentException("Cannot map invalid joint ID to Joint Name");
                case XRHandJointID.Wrist:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_wrist;
                case XRHandJointID.Palm:
                    throw new ArgumentException("VisionOS does not support the palm joint");
                case XRHandJointID.ThumbMetacarpal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_thumb_knuckle;
                case XRHandJointID.ThumbProximal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_thumb_intermediate_base;
                case XRHandJointID.ThumbDistal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_thumb_intermediate_tip;
                case XRHandJointID.ThumbTip:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_thumb_tip;
                case XRHandJointID.IndexMetacarpal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_index_finger_metacarpal;
                case XRHandJointID.IndexProximal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_index_finger_knuckle;
                case XRHandJointID.IndexIntermediate:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_index_finger_intermediate_base;
                case XRHandJointID.IndexDistal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_index_finger_intermediate_tip;
                case XRHandJointID.IndexTip:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_index_finger_tip;
                case XRHandJointID.MiddleMetacarpal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_middle_finger_metacarpal;
                case XRHandJointID.MiddleProximal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_middle_finger_knuckle;
                case XRHandJointID.MiddleIntermediate:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_middle_finger_intermediate_base;
                case XRHandJointID.MiddleDistal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_middle_finger_intermediate_tip;
                case XRHandJointID.MiddleTip:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_middle_finger_tip;
                case XRHandJointID.RingMetacarpal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_ring_finger_metacarpal;
                case XRHandJointID.RingProximal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_ring_finger_knuckle;
                case XRHandJointID.RingIntermediate:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_ring_finger_intermediate_base;
                case XRHandJointID.RingDistal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_ring_finger_intermediate_tip;
                case XRHandJointID.RingTip:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_ring_finger_tip;
                case XRHandJointID.LittleMetacarpal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_little_finger_metacarpal;
                case XRHandJointID.LittleProximal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_little_finger_knuckle;
                case XRHandJointID.LittleIntermediate:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_little_finger_intermediate_base;
                case XRHandJointID.LittleDistal:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_little_finger_intermediate_tip;
                case XRHandJointID.LittleTip:
                    return AR_Skeleton_Joint_Name.ar_hand_skeleton_joint_name_little_finger_tip;
                case XRHandJointID.EndMarker:
                    throw new ArgumentException("EndMarker is not a valid joint ID");
                default:
                    throw new ArgumentOutOfRangeException(nameof(jointID), jointID, null);
            }
        }
    }
}
#endif
