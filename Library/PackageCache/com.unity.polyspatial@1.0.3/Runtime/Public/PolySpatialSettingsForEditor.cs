using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.PolySpatial.Networking;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable NotAccessedField.Local
#pragma warning disable CS0414 // Field is assigned but its value is never used
#pragma warning disable CS0162 // Unreachable code detected

namespace Unity.PolySpatial
{
    /// <summary>
    /// Class containing the PolySpatial settings asset.
    /// </summary>
    public partial class PolySpatialSettings
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        internal enum ValidationOption
        {
            EnabledForVisionOSOnly,
            EnabledForAllPlatforms,
            Disabled
        }
        // fields that are only used in the editor, but are always part of the serialized object

        [Tooltip("Depending on the type of validation selected you can enable project validation only for visionOS, " +
                 "for all platforms (Useful to run validation when developing on Windows, for example) or completely disable PolySpatial project validation.")]
        [SerializeField]
        ValidationOption m_SelectedValidationType = ValidationOption.EnabledForAllPlatforms;

        [Tooltip("Whether or not to show conversion warnings for shader graphs loaded from packages.")]
        [SerializeField]
        bool m_ShowWarningsForShaderGraphsInPackages = true;

        const string k_DefaultAssetPath = "Assets/Resources/PolySpatialSettings.asset";
        const string k_SessionRecordingModeKey = "PolySpatial.Session.Recording.Mode";
        const string k_SessionRecordingFilePathKey = "PolySpatial.Session.Recording.FilePath";
        const string k_SessionRecordingFrameRateKey = "PolySpatial.Session.Recording.FrameRate";
        const string k_SessionRecordingFrameLimit = "PolySpatial.Session.Recording.FrameLimit";

        [Conditional("UNITY_EDITOR")]
        static void InitializeInstanceForEditor()
        {
#if UNITY_EDITOR
            s_Instance = AssetDatabase.LoadAssetAtPath<PolySpatialSettings>(k_DefaultAssetPath);
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<PolySpatialSettings>();

                // Dispatch asset creation on main thread in case instance is created on worker thread
                EditorApplication.delayCall += () =>
                {
                    // Don't overwrite an existing asset if one already exists
                    if (File.Exists(k_DefaultAssetPath))
                    {
                        Debug.LogError($"A new {nameof(PolySpatialSettings)} asset was initialized when its asset already exists. " +
                                       $"Was {nameof(PolySpatialSettings)}.{nameof(instance)} used by an asset importer?");

                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(k_DefaultAssetPath));
                    AssetDatabase.CreateAsset(s_Instance, k_DefaultAssetPath);
                    EditorUtility.SetDirty(s_Instance);
                    AssetDatabase.SaveAssetIfDirty(s_Instance);
                };
            }
#endif
        }

        [Conditional("UNITY_EDITOR")]
        static void GetAdditionalTextureFormatsInEditor(ref PolySpatialTextureCompressionFormat[] additionalTextureFormats)
        {
            var path = Path.ChangeExtension(k_DefaultAssetPath, "textureFormats");
            if (!File.Exists(path))
                return;

            additionalTextureFormats = File.ReadAllLines(path).Select(l => Enum.Parse<PolySpatialTextureCompressionFormat>(l)).ToArray();
        }

        [Conditional("UNITY_EDITOR")]
        static void SetAdditionalTextureFormatsInEditor(PolySpatialTextureCompressionFormat[] value)
        {
            var path = Path.ChangeExtension(k_DefaultAssetPath, "textureFormats");
            if (value != null)
                File.WriteAllLines(path, value.Select(f => f.ToString()).ToArray());
            else
                File.Delete(path);
        }

        [Conditional("UNITY_EDITOR")]
        static void AdjustNetworkingModeInEditor(ref NetworkingMode networkingMode)
        {
#if UNITY_EDITOR
            if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
            {
                networkingMode = NetworkingMode.LocalAndClient;
            }
#endif
        }

#if UNITY_EDITOR
        // what to do when we enter play mode
        internal static RecordingMode SessionRecordingMode
        {
            get => (RecordingMode)SessionState.GetInt(k_SessionRecordingModeKey, (int)RecordingMode.None);
            set => SessionState.SetInt(k_SessionRecordingModeKey, (int)value);
        }

        //The FrameRate to use when recording
        internal static int SessionRecordingFrameRate
        {
            get => SessionState.GetInt(k_SessionRecordingFrameRateKey, 0);
            set => SessionState.SetInt(k_SessionRecordingFrameRateKey, value);
        }

        internal static bool SessionLimitFramerateWhenRecording
        {
            get => SessionState.GetBool(k_SessionRecordingFrameLimit, false);
            set => SessionState.SetBool(k_SessionRecordingFrameLimit, value);
        }

        // the target file path for recording or playback
        internal static string SessionRecordingFilePath
        {
            get => SessionState.GetString(k_SessionRecordingFilePathKey, null);
            set => SessionState.SetString(k_SessionRecordingFilePathKey, value);
        }

        internal static void EraseSessionRecordingMode()
        {
            SessionState.EraseInt(k_SessionRecordingModeKey);
        }

        internal static void EraseSessionRecordingFilePath()
        {
            SessionState.EraseString(k_SessionRecordingFilePathKey);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_AdditionalTextureFormats = AdditionalTextureFormats;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            AdditionalTextureFormats = m_AdditionalTextureFormats;
        }

        internal ValidationOption PolySpatialValidationOption => m_SelectedValidationType;

        public bool ShowWarningsForShaderGraphsInPackages => m_ShowWarningsForShaderGraphsInPackages;
#endif

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        internal static void InitializeInEditor()
        {
            if (Application.isBatchMode)
            {
                LoadPackageVersion();
            }
            else
            {
                EditorApplication.update += LoadPackageVersion;
            }
        }

        internal static void LoadPackageVersion()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.update -= LoadPackageVersion;
            }

            var assembly = typeof(PolySpatialSettings).Assembly;
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);

            if (PolySpatialSettings.instance.m_PackageVersion == packageInfo.version)
                return;

            // Only set PolySpatialSettings dirty if the version is different
            PolySpatialSettings.instance.m_PackageVersion = packageInfo.version;
            EditorUtility.SetDirty(PolySpatialSettings.instance);
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
        }
#endif
    }
}
