using System;
using UnityEngine;
using UnityEngine.XR.Management;
#if UNITY_HAS_POLYSPATIAL
using Unity.PolySpatial;
using UnityEditor.PolySpatial;
using UnityEditor.PolySpatial.Internals;
#endif

namespace UnityEditor.XR.VisionOS
{
    /// <summary>
    /// Holds settings that are used to configure the Apple visionOS XR Plug-in.
    /// </summary>
    [Serializable]
    [XRConfigurationData("Apple visionOS", "UnityEditor.XR.VisionOS.VisionOSSettings")]
    public class VisionOSSettings : ScriptableObject
    {
        const string k_SettingsKey = "UnityEditor.XR.VisionOS.VisionOSSettings";

        const string k_HandTrackingUsageTooltip = "Provide a brief description of what hand tracking will be used for." +
            "This will be shown to users in a dialog asking them to allow authorization when hand tracking is requested." +
            "This description will be added to the Info.plist file of the generated visionOS Player Xcode project. If your " +
            "application does not use hand tracking, you can safely leave this field blank.";

        const string k_WorldSensingUsageTooltip = "Provide a brief description of what world sensing (planes, meshes, images)" +
            "will be used for. This will be shown to users in a dialog asking them to allow authorization when world sensing" +
            " is requested. This description will be added to the Info.plist file of the generated visionOS Player Xcode project. If your " +
            "application does not use world sensing, you can safely leave this field blank.";

        /// <summary>
        /// Which mode the app will use: MR or VR.
        /// </summary>
        public enum AppMode
        {
            /// <summary>
            /// Virtual Reality - Full Immersive Space
            /// </summary>
            [InspectorName("Virtual Reality - Fully Immersive Space")]
            VR,

            /// <summary>
            /// Mixed Reality - Volume or Immersive Space
            /// </summary>
            [InspectorName("Mixed Reality - Volume or Immersive Space")]
            MR,

            /// <summary>
            /// Windowed - 2D Window
            /// </summary>
            [InspectorName("Windowed - 2D Window")]
            Windowed
        }

        [SerializeField, Tooltip("Initial mode of the app.")]
        AppMode m_AppMode = AppMode.VR;

        [SerializeField, Tooltip(k_HandTrackingUsageTooltip)]
        string m_HandsTrackingUsageDescription;

        [SerializeField, Tooltip(k_WorldSensingUsageTooltip)]
        string m_WorldSensingUsageDescription;

        /// <summary>
        /// App mode.
        /// </summary>
        public AppMode appMode
        {
            get => m_AppMode;
            set => m_AppMode = value;
        }

        /// <summary>
        /// Hands tracking usage description (added to Info.plist).
        /// </summary>
        public string handsTrackingUsageDescription
        {
            get => m_HandsTrackingUsageDescription;
            set => m_HandsTrackingUsageDescription = value;
        }

        /// <summary>
        /// World sensing usage description (added to Info.plist).
        /// </summary>
        public string worldSensingUsageDescription
        {
            get => m_WorldSensingUsageDescription;
            set => m_WorldSensingUsageDescription = value;
        }

        /// <summary>
        /// Gets the currently selected settings, or creates default settings if no <see cref="VisionOSSettings"/> have been set in Player Settings.
        /// </summary>
        /// <returns>The visionOS settings to use for the current Player build.</returns>
        public static VisionOSSettings GetOrCreateSettings()
        {
            var settings = currentSettings;
            if (settings != null)
                return settings;

            return CreateInstance<VisionOSSettings>();
        }

        /// <summary>
        /// Get or set the <see cref="VisionOSSettings"/> to use for the Player build.
        /// </summary>
        public static VisionOSSettings currentSettings
        {
            get => EditorBuildSettings.TryGetConfigObject(k_SettingsKey, out VisionOSSettings settings) ? settings : null;

            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(k_SettingsKey);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(k_SettingsKey, value, true);
                }
            }
        }

        internal static bool TrySelect()
        {
            var settings = currentSettings;
            if (settings == null)
                return false;

            Selection.activeObject = settings;
            return true;
        }

        internal static SerializedObject GetSerializedSettings() => new(GetOrCreateSettings());
    }
}
