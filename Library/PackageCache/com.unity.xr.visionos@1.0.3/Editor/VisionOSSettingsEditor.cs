using UnityEngine;

#if !UNITY_HAS_POLYSPATIAL_VISIONOS
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace UnityEditor.XR.VisionOS
{
    [CustomEditor(typeof(VisionOSSettings))]
    class VisionOSSettingsEditor : Editor
    {
        const string k_WorldSensingUsageWarning = "World sensing usage description is required if world sensing features " +
            "(images, planes, or meshes) will be used. If this field is blank, the app will not be allowed to request " +
            "world sensing authorization, and these features will not work. If your app does not use world sensing, " +
            "you can safely leave this field blank.";

#if !UNITY_HAS_POLYSPATIAL_VISIONOS
        static readonly string[] k_PolySpatialPackages = { "com.unity.polyspatial.visionos", "com.unity.polyspatial.xr" };
        AddAndRemoveRequest m_InstallRequest;
#endif

        SerializedProperty m_AppModeProperty;
        SerializedProperty m_HandsTrackingUsageDescriptionProperty;
        SerializedProperty m_WorldSensingUsageDescriptionProperty;

        void OnEnable()
        {
            m_AppModeProperty = serializedObject.FindProperty("m_AppMode");
            m_HandsTrackingUsageDescriptionProperty = serializedObject.FindProperty("m_HandsTrackingUsageDescription");
            m_WorldSensingUsageDescriptionProperty = serializedObject.FindProperty("m_WorldSensingUsageDescription");
        }

        public override void OnInspectorGUI()
        {
            var isLoaderEnabled = VisionOSBuildProcessor.IsLoaderEnabled();

            serializedObject.Update();
            EditorGUIUtility.labelWidth = 200;

            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

#if !UNITY_HAS_POLYSPATIAL_VISIONOS
            var previousAppMode = (VisionOSSettings.AppMode)m_AppModeProperty.intValue;
            if (m_InstallRequest != null && m_InstallRequest.IsCompleted)
                m_InstallRequest = null;
#endif

            EditorGUILayout.PropertyField(m_AppModeProperty);
            var appMode = (VisionOSSettings.AppMode)m_AppModeProperty.intValue;

#if !UNITY_HAS_POLYSPATIAL_VISIONOS
            // ChangeCheckScope can fire when first viewing the inspector, so just compare previous to current state
            if (appMode == VisionOSSettings.AppMode.MR && previousAppMode != appMode)
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorUtility.DisplayDialog("Install PolySpatial",
                            "Mixed Reality apps require PolySpatial packages. Click Yes to install PolySpatial. Clicking No will revert this setting to its previous value.",
                            "Yes", "No"))
                    {
                        m_InstallRequest = Client.AddAndRemove(k_PolySpatialPackages);
                    }
                    else
                    {
                        m_AppModeProperty.intValue = (int)previousAppMode;
                        m_AppModeProperty.serializedObject.ApplyModifiedProperties();
                    }
                };
            }
#endif

            // Usage descriptions are only needed when loader is enabled (it will be disabled under Windowed mode regardless)
            using (new EditorGUI.DisabledScope(!isLoaderEnabled || appMode == VisionOSSettings.AppMode.Windowed))
            {
                // TODO: Enable/disable hand tracking
                EditorGUILayout.PropertyField(m_HandsTrackingUsageDescriptionProperty);
                if (isLoaderEnabled && string.IsNullOrEmpty(m_HandsTrackingUsageDescriptionProperty.stringValue))
                    EditorGUILayout.HelpBox(VisionOSBuildProcessor.HandTrackingUsageWarning, MessageType.Error);

                EditorGUILayout.PropertyField(m_WorldSensingUsageDescriptionProperty);
                if (isLoaderEnabled && string.IsNullOrEmpty(m_WorldSensingUsageDescriptionProperty.stringValue))
                    EditorGUILayout.HelpBox(k_WorldSensingUsageWarning, MessageType.Warning);
            }

            switch (appMode)
            {
                case VisionOSSettings.AppMode.VR:
                    switch (PlayerSettings.VisionOS.sdkVersion)
                    {
                        case VisionOSSdkVersion.Device:
                            EditorGUILayout.HelpBox("When building for visionOS Device SDK, Single-Pass Instanced rendering will be used.", MessageType.Info);
                            break;
                        case VisionOSSdkVersion.Simulator:
                            EditorGUILayout.HelpBox("When building for visionOS Simulator SDK, Multi-Pass rendering will be used.", MessageType.Info);
                            break;
                    }

                    break;
                case VisionOSSettings.AppMode.MR:
#if UNITY_HAS_POLYSPATIAL_VISIONOS
                    EditorGUILayout.HelpBox("The initial window configuration at app launch is determined by the default volume settings, found in Project " +
                        "Settings > PolySpatial Settings.", MessageType.Info);
#else
                    if (m_InstallRequest != null && !m_InstallRequest.IsCompleted)
                    {
                        EditorGUILayout.HelpBox("Installing PolySpatial packages...", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Mixed Reality on visionOS requires PolySpatial and the PolySpatial visionOS packages.", MessageType.Error);
                        if (GUILayout.Button("Install Packages"))
                            m_InstallRequest = Client.AddAndRemove(k_PolySpatialPackages);
                    }
#endif
                    break;
                case VisionOSSettings.AppMode.Windowed:
                    if (isLoaderEnabled)
                    {
                        EditorGUILayout.HelpBox("The Apple visionOS XR loader is not supported when building a visionOS Windowed application. It will be " +
                            "disabled prior to builds and then re-enabled afterward. You may need to manually re-enable the loader in XR Plugin Management " +
                            "settings if the build fails.", MessageType.Warning);
                    }

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
