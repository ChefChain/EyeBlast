using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.PolySpatial.Networking;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable NotAccessedField.Local
#pragma warning disable CS0414 // Field is assigned but its value is never used
#pragma warning disable CS0162 // Unreachable code detected

namespace Unity.PolySpatial
{
    public partial class PolySpatialSettings
    {
        [Flags]
        internal enum PolySpatialRuntimeMode
        {
            Auto = 0,
            ForceLinked = 1 << 0,
            ForceEnabled = 1 << 1,
        }

        // Always present fields in the serialized object, even if not used in non-INTERNAL builds.
        [SerializeField]
        PolySpatialRuntimeMode m_RuntimeMode;

        [SerializeField]
        bool m_EnableTransformVerification;

        [SerializeField]
        bool m_EnableProgressiveMipStreaming;

        [SerializeField]
        long m_MaxMipByteSizePerCycle = k_DefaultMaxMipByteSizePerCycle;

        [SerializeField]
        ulong m_RuntimeFlags = 0;

        [Tooltip("PolySpatial creates a mirrored version of your scene on the target runtime. When previewing in the Unity Editor, " +
                 "this option will enable or disable creating these preview clones within Unity.  Changing this setting does not affect behavior of a build.")]
        [SerializeField]
        bool m_EnableInEditorPreview = true;

        [SerializeField]
        [Tooltip("When enabled, GameObjects created to support PolySpatial preview will be hidden in the Scene view.")]
        bool m_HidePolySpatialPreviewObjectsInScene = true;

        [SerializeField]
        [Tooltip("Always links the PolySpatial runtime when making a build.")]
        bool m_AlwaysLinkPolySpatialRuntime;

        [SerializeField]
        NetworkingMode m_PolySpatialNetworkingMode;

        [SerializeField]
        [Tooltip("How long (in seconds) to try connecting to a remote host before timing out.")]
        uint m_ConnectionTimeout = DefaultConnectionTimeout;

        [SerializeField]
        bool m_EnableHostCameraControl;

        [SerializeField]
        bool m_EnableClipping;

        [SerializeField]
        List<string> m_SerializedServerAddresses = new() { DefaultServerAddress };

        [SerializeField]
        List<string> m_SerializedIgnoredScenePaths;

        [SerializeField]
        private bool m_MockBackend = false;

        [Conditional("POLYSPATIAL_INTERNAL")]
        internal static void RuntimeOverrideFromEnvironment(PolySpatialSettings inst)
        {
            // Environment overrides command line variables

            var netmode =
                Environment.GetEnvironmentVariable("POLYSPATIAL_NETMODE") ??
                FindCmdArg("-qnetmode");
            var server =
                Environment.GetEnvironmentVariable("POLYSPATIAL_HOST") ??
                FindCmdArg("-qhost");
            var launchscene =
                Environment.GetEnvironmentVariable("POLYSPATIAL_SCENE") ??
                FindCmdArg("-qscene");

            if (!string.IsNullOrEmpty(netmode))
            {
                if (Enum.TryParse(netmode, true, out NetworkingMode mode))
                {
                    inst.m_PolySpatialNetworkingMode = mode;
                }
                else
                {
                    Debug.LogWarning($"Invalid POLYSPATIAL_NETMODE value: {netmode}");
                }
            }

            if (!string.IsNullOrEmpty(server))
            {
                inst.m_SerializedServerAddresses = new List<string> { server };
            }

            inst.m_DemoLaunchSceneName = launchscene;
        }

        internal uint ConnectionTimeout => m_ConnectionTimeout;

        internal static ulong RuntimeFlags
        {
            get => instance.m_RuntimeFlags;
            set
            {
                instance.m_RuntimeFlags = value;
            }
        }

        // TODO make RuntimeMode const-Auto in non-POLYSPATIAL_INTERNAL
        internal static PolySpatialRuntimeMode RuntimeMode => instance.m_RuntimeMode;

        internal static bool RuntimeModeAuto => RuntimeMode == PolySpatialRuntimeMode.Auto;

        internal static bool RuntimeModeForceEnabled => RuntimeMode.HasFlag(PolySpatialRuntimeMode.ForceEnabled);

        internal static bool RuntimeModeForceLinked => RuntimeMode.HasFlag(PolySpatialRuntimeMode.ForceLinked);

        // In non-INTERNAL builds, all of these turn into constants (see the #else block) to ensure code is
        // properly stripped in dependent assemblies.
#if POLYSPATIAL_INTERNAL

        internal static bool EnableTransformVerification => instance.m_EnableTransformVerification;

        internal static bool EnableProgressiveMipStreaming => instance.m_EnableProgressiveMipStreaming;

        internal static long MaxMipByteSizePerCycle => instance.m_MaxMipByteSizePerCycle;

        internal static bool EnableInEditorPreview => instance.m_EnableInEditorPreview;

        internal static bool EnableHostCameraControl
        {
            get => instance.m_EnableHostCameraControl;
            set => instance.m_EnableHostCameraControl = value;
        }

        internal static bool EnableClipping => instance.m_EnableClipping;

        internal static bool MockBackend => instance.m_MockBackend;

        internal static bool HidePolySpatialPreviewObjectsInScene => instance.m_HidePolySpatialPreviewObjectsInScene;

        static NetworkingMode InternalNetworkingMode => instance.m_PolySpatialNetworkingMode;
#else
        internal const bool EnableTransformVerification = false;
        internal const bool EnableProgressiveMipStreaming = false;
        internal const long MaxMipByteSizePerCycle = k_DefaultMaxMipByteSizePerCycle;
        internal const bool EnableInEditorPreview = true;
        internal const bool EnableHostCameraControl = false;
        internal const bool EnableClipping = false;
        internal const bool MockBackend = false;
        internal const bool HidePolySpatialPreviewObjectsInScene = true;

        const NetworkingMode InternalNetworkingMode = NetworkingMode.Local;
#endif

        readonly Lazy<List<SocketAddress>> m_ServerAddresses = new(() =>
        {
            var results = new List<SocketAddress>();

#if POLYSPATIAL_INTERNAL
            if (instance.m_PolySpatialNetworkingMode == NetworkingMode.LocalAndClient)
            {
                foreach (var address in instance.m_SerializedServerAddresses)
                {
                    if (SocketAddress.ParseAddress(address, DefaultServerPort, out var socketAddress))
                    {
                        results.Add(socketAddress);
                    }
                }
            }
#endif

#if UNITY_EDITOR
            if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
            {

                foreach (var candidate in PolySpatialUserSettings.instance.ConnectionCandidates.Values)
                {
                    // If there is no candidate already added with the same IP and Port, and the IP parses correctly, add this candidate.
                    if (candidate.IsSelected && results.All(s => !(s.Host == candidate.IP && s.Port == candidate.ServerPort))
                                             && SocketAddress.ParseAddress(candidate.IP, candidate.ServerPort, out var socketAddress))
                    {
                        results.Add(socketAddress);
                    }
                }
            }
#endif

            if (results.Count == 0)
            {
                results.Add(new SocketAddress() { Host = DefaultServerAddress, Port = DefaultServerPort });
            }

            return results;
        });

        readonly Lazy<HashSet<string>> m_IgnoredScenePaths = new(() => new HashSet<string>(
#if POLYSPATIAL_INTERNAL
        instance.m_SerializedIgnoredScenePaths
#endif
        ));

        // Hack; non-serialized value to store the launch scene that comes from
        // the command line or environment.  Used for sample bootstrapping.
        string m_DemoLaunchSceneName = null;

#if POLYSPATIAL_INTERNAL
        public static string DemoLaunchSceneName => instance.m_DemoLaunchSceneName;
#endif

        internal static string FindCmdArg(string arg)
        {
            var cmdargs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i].ToLowerInvariant() == arg)
                {
                    if (i + 1 < cmdargs.Length)
                        return cmdargs[i + 1];
                    return null;
                }
            }

            return null;
        }
    }
}
