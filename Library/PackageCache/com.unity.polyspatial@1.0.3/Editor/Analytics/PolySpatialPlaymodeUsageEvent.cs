#if ENABLE_CLOUD_SERVICES_ANALYTICS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;

#if ENABLE_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// Editor event used to send editor usage <see cref="PolySpatialAnalytics"/> data.
    /// Only accepts <see cref="PolySpatialPlaymodeUsageEvent.Payload"/> parameters.
    /// </summary>
    class PolySpatialPlaymodeUsageEvent : PolySpatialEditorAnalyticsEvent<PolySpatialPlaymodeUsageEvent.Payload>
    {
        const string k_EventName = "polyspatial_playmode_usage";
        const int k_EventVersion = 2;

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
        {
            internal const string EnteredPlaymodeState = "EnteredPlaymode";
            internal const string NotInstalledState = "NotInstalled";
            internal const string ActivatedState = "Activated";
            internal const string DeactivatedState = "Deactivated";
            internal const string MRMode = "MR";
            internal const string VRMode = "VR";
            internal const string WindowedMode = "Windowed";
            internal const string UndefinedMode = "Undefined";

            [SerializeField]
            internal string PlaymodeState;

            [SerializeField]
            internal string ActiveBuildTarget;

            [SerializeField]
            internal string PolySpatialRuntimeState;

            [SerializeField]
            internal int BoundedVolumes;

            [SerializeField]
            internal int UnboundedVolumes;

            [SerializeField]
            internal string XRManagementState;

            [SerializeField]
            internal string[] ActiveXRLoaders;

            [SerializeField]
            internal string ConfiguredMode;

            [SerializeField]
            internal List<AppNetworkPayload> AppNetworkConnections;
        }

        [Serializable]
        internal struct AppNetworkPayload
        {
            internal const string UnknownAppName = "Unknown";
            internal const string UnityPlayToDeviceName = "UnityPlayToDevice";

            [SerializeField]
            internal bool IsConnected;

            [SerializeField]
            internal string AppName;
        }

        static List<AppNetworkPayload> GetLocalAppNetworkConnections()
        {
            var localAppNetwork = new List<AppNetworkPayload>();
            if (!PolySpatialRuntime.Enabled)
                return localAppNetwork;

            var localAppNetworkConnections = PolySpatialCore.LocalAppNetworkConnections;
            var selectedCandidate = PolySpatialUserSettings.instance.ConnectionCandidates.Values.FirstOrDefault(c => c.IsSelected);
            var playToDeviceIP = selectedCandidate != null && IPAddress.TryParse(selectedCandidate.IP, out var ipAddress) ? ipAddress : null;

            // Will be marked true if one or more connections are valid
            var connected = false;

            if (localAppNetworkConnections != null)
            {
                foreach (var localAppNetworkConnection in localAppNetworkConnections)
                {
                    if (localAppNetworkConnection != null && localAppNetworkConnection.Connected)
                    {
                        connected = true;

                        localAppNetwork.Add(new AppNetworkPayload()
                        {
                            IsConnected = true,
                            AppName = localAppNetworkConnection.Address.Equals(playToDeviceIP)
                                ? AppNetworkPayload.UnityPlayToDeviceName
                                : AppNetworkPayload.UnknownAppName
                        });
                    }
                }
            }

            if (!connected && PolySpatialUserSettings.instance.ConnectToPlayToDevice && playToDeviceIP != null)
            {
                localAppNetwork.Add(new AppNetworkPayload()
                {
                    IsConnected = false,
                    AppName = AppNetworkPayload.UnityPlayToDeviceName
                });
            }

            return localAppNetwork;
        }

        internal PolySpatialPlaymodeUsageEvent() : base(k_EventName, k_EventVersion)
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnPlayModeChanged(PlayModeStateChange newState)
        {
            if (newState != PlayModeStateChange.EnteredPlayMode)
                return;

            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var payload = new Payload()
            {
                PlaymodeState = Payload.EnteredPlaymodeState,
                ActiveBuildTarget = activeBuildTarget.ToString(),
                PolySpatialRuntimeState = Payload.DeactivatedState,
                XRManagementState = Payload.NotInstalledState,
                ActiveXRLoaders = new string[0],
                ConfiguredMode = Payload.UndefinedMode
            };

            if (PolySpatialRuntime.Enabled)
            {
                payload.PolySpatialRuntimeState = Payload.ActivatedState;
                if (PolySpatialRuntime.Enabled && PolySpatialCore.UnitySimulation != null)
                {
                    var volumeCamera = PolySpatialCore.UnitySimulation.Camera;
                    if (volumeCamera != null)
                    {
                        switch (volumeCamera.WindowMode)
                        {
                            case VolumeCamera.PolySpatialVolumeCameraMode.Bounded:
                                payload.BoundedVolumes++;
                                break;
                            case VolumeCamera.PolySpatialVolumeCameraMode.Unbounded:
                                payload.UnboundedVolumes++;
                                break;
                        }
                    }
                }
            }

#if ENABLE_XR_MANAGEMENT
            var group = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            var isXRManagementActive = generalSettings != null && generalSettings.InitManagerOnStart;
            if (isXRManagementActive)
            {
                payload.XRManagementState = Payload.ActivatedState;
                if (generalSettings.Manager != null)
                {
                    payload.ActiveXRLoaders = generalSettings.Manager.activeLoaders
                        .Where(l => l != null)
                        .Select(l => l.GetType().Name)
                        .ToArray();
                }
            }
            else
            {
                payload.XRManagementState = Payload.DeactivatedState;
            }
#endif

            if (activeBuildTarget == BuildTarget.VisionOS)
            {
                if (payload.PolySpatialRuntimeState == Payload.ActivatedState)
                    payload.ConfiguredMode = Payload.MRMode;
                else if (payload.XRManagementState == Payload.ActivatedState)
                    payload.ConfiguredMode = Payload.VRMode;
                else
                    payload.ConfiguredMode = Payload.WindowedMode;
            }

            payload.AppNetworkConnections = GetLocalAppNetworkConnections();

            Send(payload);
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
