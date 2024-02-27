#if POLYSPATIAL_INTERNAL
using System;
using System.Collections.Generic;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.AnimatedValues;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// Settings provider for internal properties.
    /// </summary>
    class PolySpatialInternalSettingsProvider : SettingsProvider
    {
        const string k_SettingsPath = PolySpatialSettingsProvider.SettingsPath + "/PolySpatial Internal";

        [SettingsProvider]
        static SettingsProvider CreatePolySpatialProjectSettingsProvider()
        {
            return new PolySpatialInternalSettingsProvider(k_SettingsPath, SettingsScope.Project);
        }

        SerializedObject m_SerializedObject;

        SerializedProperty m_RuntimeModeProperty;

        SerializedProperty m_HidePolySpatialPreviewObjectsInScene;
        SerializedProperty m_PolySpatialNetworkingModeProperty;
        SerializedProperty m_ConnectionTimeoutProperty;
        SerializedProperty m_ServerAddressesProperty;
        SerializedProperty m_EnableHostCameraControlProperty;
        SerializedProperty m_EnableClippingProperty;

        SerializedProperty m_EnableProgressiveMipStreamingProperty;
        SerializedProperty m_MaxMipByteSizePerCycleProperty;
        AnimBool m_EnableProgressiveMipStreamingAnim;

        SerializedProperty m_EnableTransformVerificationProperty;
        SerializedProperty m_RuntimeFlags;
        SerializedProperty m_AdditionalTextureFormatsProperty;

        SerializedProperty m_EnableStatisticsProperty;

        SerializedProperty m_IgnoredScenesListProperty;
        ReorderableList m_IgnoredScenesReorderableList;

        SerializedProperty m_AlwaysLinkPolySpatialRuntimeProperty;
        SerializedProperty m_EnableInEditorPreviewProperty;

        SerializedProperty m_ConnectionDiscoveryPortProperty;
        SerializedProperty m_ConnectionDiscoverySendIntervalProperty;
        SerializedProperty m_ConnectionDiscoveryTimeOutDurationProperty;

        SerializedProperty m_MockBackendProperty;

        /// <summary>
        /// Settings constructor called when we need a new settings instance.
        /// </summary>
        /// <param name="path">Path of the settings window within the settings UI.</param>
        /// <param name="scopes">The scopes within which the settings are valid.</param>
        /// <param name="keywords">Keywords for search.</param>
        PolySpatialInternalSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        /// <inheritdoc/>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = new SerializedObject(PolySpatialSettings.instance);
            m_RuntimeModeProperty = m_SerializedObject.FindProperty("m_RuntimeMode");

            m_EnableTransformVerificationProperty = m_SerializedObject.FindProperty("m_EnableTransformVerification");

            m_PolySpatialNetworkingModeProperty = m_SerializedObject.FindProperty("m_PolySpatialNetworkingMode");
            m_ConnectionTimeoutProperty = m_SerializedObject.FindProperty("m_ConnectionTimeout");
            m_ServerAddressesProperty = m_SerializedObject.FindProperty("m_SerializedServerAddresses");
            m_EnableHostCameraControlProperty = m_SerializedObject.FindProperty("m_EnableHostCameraControl");
            m_EnableClippingProperty = m_SerializedObject.FindProperty("m_EnableClipping");

            m_RuntimeFlags = m_SerializedObject.FindProperty("m_RuntimeFlags");
            m_HidePolySpatialPreviewObjectsInScene = m_SerializedObject.FindProperty("m_HidePolySpatialPreviewObjectsInScene");
            m_AdditionalTextureFormatsProperty = m_SerializedObject.FindProperty("m_AdditionalTextureFormats");

            m_EnableStatisticsProperty = m_SerializedObject.FindProperty("m_EnableStatistics");

            m_IgnoredScenesListProperty = m_SerializedObject.FindProperty("m_SerializedIgnoredScenePaths");
            m_IgnoredScenesReorderableList = new ReorderableList(m_SerializedObject, m_IgnoredScenesListProperty, true, true, true, true)
            {
                drawElementCallback = DrawIgnoredSceneItem,
                drawHeaderCallback = DrawIgnoredSceneHeader
            };

            m_AlwaysLinkPolySpatialRuntimeProperty = m_SerializedObject.FindProperty("m_AlwaysLinkPolySpatialRuntime");
            m_EnableInEditorPreviewProperty = m_SerializedObject.FindProperty("m_EnableInEditorPreview");

            m_EnableProgressiveMipStreamingProperty = m_SerializedObject.FindProperty("m_EnableProgressiveMipStreaming");
            m_MaxMipByteSizePerCycleProperty = m_SerializedObject.FindProperty("m_MaxMipByteSizePerCycle");

            m_ConnectionDiscoveryPortProperty = m_SerializedObject.FindProperty("m_ConnectionDiscoveryPort");
            m_ConnectionDiscoverySendIntervalProperty = m_SerializedObject.FindProperty("m_ConnectionDiscoverySendInterval");
            m_ConnectionDiscoveryTimeOutDurationProperty = m_SerializedObject.FindProperty("m_ConnectionDiscoveryTimeOutDuration");

            m_MockBackendProperty = m_SerializedObject.FindProperty("m_MockBackend");

            m_EnableProgressiveMipStreamingAnim = new AnimBool(m_EnableProgressiveMipStreamingProperty.boolValue);
            m_EnableProgressiveMipStreamingAnim.valueChanged.AddListener(Repaint);
        }

        /// <inheritdoc/>
        public override void OnGUI(string searchContext)
        {
            using (EditorGUIBridge.CreateSettingsWindowGUIScope())
            {
                m_SerializedObject.Update();

                EditorGUILayout.PropertyField(m_RuntimeModeProperty, new GUIContent("Runtime Mode"));

                EditorGUILayout.PropertyField(m_PolySpatialNetworkingModeProperty, new GUIContent("Networking Mode"));
                using (new EditorGUI.IndentLevelScope())
                {
                    switch ((PolySpatialSettings.NetworkingMode)m_PolySpatialNetworkingModeProperty.enumValueFlag)
                    {
                        case PolySpatialSettings.NetworkingMode.LocalAndClient:
                            EditorGUILayout.PropertyField(m_ServerAddressesProperty, new GUIContent("Server Addresses"));
                            break;
                    }

                    EditorGUILayout.PropertyField(m_ConnectionTimeoutProperty,
                        new GUIContent("Connection Timeout",
                            "Set connection timeout in seconds."));
                    EditorGUILayout.PropertyField(m_EnableHostCameraControlProperty,
                        new GUIContent("Enable Host Camera Control",
                            "Host will control camera transform."));
                    EditorGUILayout.PropertyField(m_EnableClippingProperty,
                        new GUIContent("Enable Clipping Buffer",
                            "Clip apps to the bounds of their VolumeRenderer."));
                }

                EditorGUILayout.PropertyField(m_EnableStatisticsProperty);

                EditorGUILayout.PropertyField(m_EnableTransformVerificationProperty);

                EditorGUILayout.PropertyField(m_HidePolySpatialPreviewObjectsInScene, new GUIContent("Hide Play Mode Preview Objects In Scene View"));

                EditorGUILayout.PropertyField(m_EnableProgressiveMipStreamingProperty);
                m_EnableProgressiveMipStreamingAnim.target = m_EnableProgressiveMipStreamingProperty.boolValue;
                using (var group = new EditorGUILayout.FadeGroupScope(m_EnableProgressiveMipStreamingAnim.faded))
                {
                    if (group.visible)
                    {
                        using (new EditorGUI.IndentLevelScope())
                            EditorGUILayout.PropertyField(m_MaxMipByteSizePerCycleProperty);
                    }
                }

                DrawRuntimeFlags();
                EditorGUILayout.PropertyField(m_AdditionalTextureFormatsProperty);
                EditorGUILayout.Space();

                m_IgnoredScenesReorderableList.DoLayoutList();

                EditorGUILayout.PropertyField(m_AlwaysLinkPolySpatialRuntimeProperty);
                EditorGUILayout.PropertyField(m_EnableInEditorPreviewProperty);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_ConnectionDiscoveryPortProperty);
                EditorGUILayout.PropertyField(m_ConnectionDiscoverySendIntervalProperty);
                EditorGUILayout.PropertyField(m_ConnectionDiscoveryTimeOutDurationProperty);

                EditorGUILayout.PropertyField(m_MockBackendProperty);

                m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
            }
        }

        void DrawRuntimeFlags()
        {
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var flags = (PolySpatialRuntimeFlags)m_RuntimeFlags.ulongValue;
                flags = (PolySpatialRuntimeFlags)EditorGUILayout.EnumFlagsField(m_RuntimeFlags.displayName, flags);
                if (changeCheckScope.changed)
                    m_RuntimeFlags.ulongValue = (ulong)flags;
            }
        }

        void DrawIgnoredSceneHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, m_IgnoredScenesListProperty.displayName);
        }

        void DrawIgnoredSceneItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_IgnoredScenesListProperty.GetArrayElementAtIndex(index);
            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(element.stringValue);

            EditorGUI.BeginChangeCheck();

            var newScene = EditorGUI.ObjectField(rect, oldScene, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck())
            {
                var newPath = AssetDatabase.GetAssetPath(newScene);
                element.stringValue = newPath;
            }
        }
    }
}
#endif
