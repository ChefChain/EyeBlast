using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.PolySpatial.Networking;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.PolySpatial
{
    /// <summary>
    /// Class containing the PolySpatial settings asset.
    /// </summary>
    public partial class PolySpatialSettings : ScriptableObject
    {
        internal enum PolySpatialTextureCompressionFormat
        {
            Unknown = 0,
            ETC = 1,
            ETC2 = 2,
            ASTC = 3,
            PVRTC = 4,
            DXTC = 5,
            BPTC = 6,
            DXTC_RGTC = 7
        }

        internal enum NetworkingMode
        {
            Local,
            LocalAndClient,
        }

        internal enum RecordingMode
        {
            None,
            Record,
            Playback
        }

        internal enum ParticleReplicationMode
        {
            ReplicateProperties,
            BakeToMesh,
#if POLYSPATIAL_INTERNAL
            ExperimentalBakeToTexture
#endif
        }

        [Serializable]
        internal struct ProjectionHalfAngles
        {
            public ProjectionHalfAngles(float left, float right, float top, float bottom)
            {
                this.left = left;
                this.right = right;
                this.top = top;
                this.bottom = bottom;
            }

            public float left;
            public float right;
            public float top;
            public float bottom;

            public static ProjectionHalfAngles Default => new ProjectionHalfAngles(-1.0f, 1.0f, 1.0f, -1.0f);
        }

        [Serializable]
        internal struct DisplayProviderParameters
        {
            public int framebufferWidth;
            public int framebufferHeight;
            public Pose leftEyePose;
            public Pose rightEyePose;
            public ProjectionHalfAngles leftProjectionHalfAngles;
            public ProjectionHalfAngles rightProjectionHalfAngles;
        }

        const int k_DefaultMaxMipByteSizePerCycle = 128000;

        static PolySpatialSettings s_Instance;

        /// <summary>
        /// Gets a reference for an instance of the PolySpatial settings asset in the project.
        /// </summary>
        public static PolySpatialSettings instance {
            get {
                InitializeInstance();
                return s_Instance;
            }
        }

        internal static PolySpatialSettings GetInstanceIfExists()
        {
            return s_Instance;
        }

        static void InitializeInstance()
        {
            if (s_Instance != null)
                return;

            InitializeInstanceForEditor();
            InitializeInstanceForRuntime();
        }

        #if UNITY_EDITOR
        [Conditional("DEADBEEF_SHOULD_NOT_BE_DEFINED")]
        #endif
        static void InitializeInstanceForRuntime()
        {
            s_Instance = Resources.Load<PolySpatialSettings>("PolySpatialSettings");
            if (s_Instance == null)
            {
                Debug.LogWarning("PolySpatialSettings not found in Resources folder. Using default settings.");
                s_Instance = CreateInstance<PolySpatialSettings>();
            }

            RuntimeOverrideFromEnvironment(s_Instance);
        }

        void Awake()
        {
            if (m_DefaultVolumeCameraWindowConfiguration == null)
                m_DefaultVolumeCameraWindowConfiguration = Resources.Load<VolumeCameraWindowConfiguration>("Default Unbounded Configuration");
        }

        [SerializeField] string m_PackageVersion;

        /// <summary>The version of the PolySpatial package</summary>
        public string PackageVersion => m_PackageVersion;

        [SerializeField]
        [Tooltip("When enabled, PolySpatial will collect information about its tracked objects. You can see this data in the PolySpatial Statistics windows.")]
        bool m_EnableStatistics;

        internal bool EnableStatistics => m_EnableStatistics;

        [SerializeField]
        [Tooltip("Only colliders in these layers are tracked by PolySpatial.")]
        LayerMask m_ColliderSyncLayerMask = 1;

        internal LayerMask ColliderSyncLayerMask
        {
            get => m_ColliderSyncLayerMask;
            set => m_ColliderSyncLayerMask = value;
        }

        [SerializeField]
        [Tooltip("The technique used to translate particle data to Reality Kit. Replicate Properties: Unity Particle System Properties will be mapped to " +
                 "RealityKit Particle System Properties. BakeToMesh: Particle Systems will be baked to a mesh every frame and rendered in RealityKit.")]
        ParticleReplicationMode m_ParticleMode;

        internal ParticleReplicationMode ParticleMode
        {
            get => m_ParticleMode;
            set => m_ParticleMode = value;
        }

        [SerializeField]
        [Tooltip("Whether or not to track light and reflection probes for PolySpatial Lighting node.")]
        bool m_TrackLightAndReflectionProbes = true;

        internal bool TrackLightAndReflectionProbes => m_TrackLightAndReflectionProbes;

        [SerializeField]
        [Tooltip("GameObjects created in these layers will have tracking completely disabled.")]
        LayerMask m_DisableTrackingMask;

        internal LayerMask DisableTrackingMask
        {
            get => m_DisableTrackingMask;
            set => m_DisableTrackingMask = value;
        }

        [SerializeField]
        [Tooltip("PolySpatial tracker type names to disable. When a tracker is disabled, PolySpatial won't track their respective Unity object types.")]
        string[] m_DisabledTrackers;

        internal string[] DisabledTrackers => m_DisabledTrackers;

        // Additional texture formats to be produced by the PolySpatialTextureImporter.
        // This cannot be a simple serialized field on the PolySpatialSettings ScriptableObject,
        // because the TextureImporter may run before the PolySpatialSettings object is imported - so we need to be
        // able to access these even if the PolySpatialSettings object does not exist. So, instead, the static
        // AdditionalTextureFormats property will read and write from a file next to the ScriptableObjects.
        //
        // So, why have a serialized field here at all then? Because it makes the settings UI in the
        // PolySpatialSettingsProvider "just work". So we have a field backed by the static property using
        // ISerializationCallbackReceiver.
        [SerializeField]
        PolySpatialTextureCompressionFormat[] m_AdditionalTextureFormats;

        internal static PolySpatialTextureCompressionFormat[] AdditionalTextureFormats
        {
            get
            {
                PolySpatialTextureCompressionFormat[] additionalFormats = null;
                GetAdditionalTextureFormatsInEditor(ref additionalFormats);
                return additionalFormats;
            }

            #if UNITY_EDITOR
            set
            {
                SetAdditionalTextureFormatsInEditor(value);
            }
            #endif
        }

        [SerializeField]
        [Tooltip("Default Volume Camera Window configuration, if none is specified on a Volume Camera component. If null, unbounded is assumed.")]
        VolumeCameraWindowConfiguration m_DefaultVolumeCameraWindowConfiguration;

        /// <summary>
        /// Default Volume Camera camera configuration, if none is specified on a Volume Camera component. If null, unbounded is assumed.
        /// </summary>
        public VolumeCameraWindowConfiguration DefaultVolumeCameraWindowConfiguration
        {
            get => m_DefaultVolumeCameraWindowConfiguration;
            set => m_DefaultVolumeCameraWindowConfiguration = value;
        }

        [Obsolete("Renamed to DefaultVolumeCameraWindowConfiguration (UnityUpgradable) -> DefaultVolumeCameraWindowConfiguration")]
        public VolumeCameraWindowConfiguration DefaultVolumeCameraConfiguration
        {
            get => m_DefaultVolumeCameraWindowConfiguration;
            set => m_DefaultVolumeCameraWindowConfiguration = value;
        }

        [SerializeField]
        [Tooltip("When enabled, if there is no Volume Camera after scene load, one will be automatically created using the default settings. Disable this to be able to create the initial Volume Camera from script.")]
        bool m_AutoCreateVolumeCamera = true;

        public bool AutoCreateVolumeCamera
        {
            get => m_AutoCreateVolumeCamera;
            set => m_AutoCreateVolumeCamera = value;
        }

        [SerializeField]
        [Tooltip("When enabled, PolySpatial tracks preview object names and display them in the Hierarchy view.")]
        bool m_TransmitDebugInfo;

        internal bool TransmitDebugInfo
        {
            get => m_TransmitDebugInfo;
            set => m_TransmitDebugInfo = value;
        }

        [Tooltip("Convert unsupported shaders at runtime to a best guess.")]
        [SerializeField]
        bool m_EnableFallbackShaderConversion = true;

        /// <summary>
        /// Convert unsupported shaders at runtime to a best guess.
        /// </summary>
        public bool EnableFallbackShaderConversion => m_EnableFallbackShaderConversion;

        [Tooltip("Run tests at runtime to validate setup.")]
        [SerializeField]
        bool m_EnableRuntimeValidation = false;

        /// <summary>
        /// Run tests at runtime to validate setup.
        /// </summary>
        public bool EnableRuntimeValidation => m_EnableRuntimeValidation;

        [Tooltip("The frame count from launch before running the runtime validation.")]
        [SerializeField]
        int m_RuntimeValidationFrameCount = 10;

        /// <summary>
        /// The frame count from launch before running the runtime validation.
        /// </summary>
        public int RuntimeValidationFrameCount => m_RuntimeValidationFrameCount;

        internal NetworkingMode PolySpatialNetworkingMode
        {
            get
            {
#if POLYSPATIAL_FORCE_CLIENT
                // POLYSPATIAL_FORCE_CLIENT used for internal testing+dev when mirroring a project to be the client.
                // See Projects/PolySpatialShell/README.md for project mirroring instructions.
                Logging.Log(Unity.PolySpatial.Internals.LogCategory.Debug,
                    "POLYSPATIAL_FORCE_CLIENT #define set, project forced to always be network client.",
                    LogType.Warning);
                return NetworkingMode.Client;
#endif

                var mode = InternalNetworkingMode;
                AdjustNetworkingModeInEditor(ref mode);
                return mode;
            }

#if POLYSPATIAL_INTERNAL
            set => m_PolySpatialNetworkingMode = value;
#endif
        }

        internal List<SocketAddress> ServerAddresses => m_ServerAddresses.Value;

        internal const int DefaultServerPort = 9876;
        internal const string DefaultServerAddress = "127.0.0.1";
        internal const uint DefaultConnectionTimeout = 5;

        [SerializeField] int m_ConnectionDiscoveryPort = 9877;

        /// <summary>Default port for auto connection discovery</summary>
        internal int ConnectionDiscoveryPort => m_ConnectionDiscoveryPort;

        [SerializeField] float m_ConnectionDiscoverySendInterval = 1.0f;

        /// <summary>Default interval between UDP broadcast for auto connection discovery app host</summary>
        internal float ConnectionDiscoverySendInterval => m_ConnectionDiscoverySendInterval;

        [SerializeField] float m_ConnectionDiscoveryTimeOutDuration = 5.0f;

        /// <summary>The timeout duration in seconds to mark a connection as Lost for auto connection discovery</summary>
        internal float ConnectionDiscoveryTimeOutDuration => m_ConnectionDiscoveryTimeOutDuration;

        internal HashSet<string> IgnoredScenePaths => m_IgnoredScenePaths.Value;

        // TODO -- this should not be in settings, it is platform/device specific

        // Temporarily defaults to the simulator parameters until HW ship.
        [SerializeField]
        DisplayProviderParameters m_DeviceDisplayProviderParameters = new()
        {
            framebufferWidth = 1920,
            framebufferHeight = 1080,
            leftEyePose = new(position: Vector3.zero, rotation: Quaternion.identity),
            rightEyePose = new(position: Vector3.zero, rotation: Quaternion.identity),
            leftProjectionHalfAngles = ProjectionHalfAngles.Default,
            rightProjectionHalfAngles = ProjectionHalfAngles.Default,
        };

        internal DisplayProviderParameters DeviceDisplayProviderParameters => m_DeviceDisplayProviderParameters;

        [SerializeField]
        DisplayProviderParameters m_SimulatorDisplayProviderParameters = new()
        {
            framebufferWidth = 1920,
            framebufferHeight = 1080,
            leftEyePose = new(position: Vector3.zero, rotation: Quaternion.identity),
            rightEyePose = new(position: Vector3.zero, rotation: Quaternion.identity),
            leftProjectionHalfAngles = ProjectionHalfAngles.Default,
            rightProjectionHalfAngles = ProjectionHalfAngles.Default,
        };

        internal DisplayProviderParameters SimulatorDisplayProviderParameters => m_SimulatorDisplayProviderParameters;

        [Tooltip("Define material substitutions that will be used by PolySpatial at runtime. Add any MaterialSwapSet assets you would like to use to this " +
            "list. The material on the left will be replaced with the material on the right as a fallback. For example, you may swap a material which uses a " +
            "custom shader for one which uses URP/Lit.")]
        [SerializeField]
        MaterialSwapSet[] m_MaterialSwapSets = Array.Empty<MaterialSwapSet>();

        internal MaterialSwapSet[] MaterialSwapSets => m_MaterialSwapSets;
    }
}
