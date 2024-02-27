using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using Unity.XR.CoreUtils;
using UnityEditor.PolySpatial.Utilities;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PolySpatial
{
    [CustomEditor(typeof(PolySpatialSettings))]
    class PolySpatialSettingsEditor : Editor
    {
        static readonly List<string> s_TrackerTypeNames;
        static readonly List<string> s_TrackerTooltips;

        SerializedProperty m_DefaultVolumeCameraWindowConfigurationProperty;
        SerializedProperty m_AutoCreateVolumeCameraProperty;
        GUIContent m_AutoCreateVolumeCameraContent;
        SerializedProperty m_ColliderSyncLayerMaskProperty;
        SerializedProperty m_ParticleModeProperty;
        SerializedProperty m_TrackLightAndReflectionProbesProperty;

        SerializedProperty m_TransmitDebugInfoProperty;

        SerializedProperty m_DisableTrackingMaskProperty;
        SerializedProperty m_DisabledTrackersProperty;
        ReorderableList m_DisabledTrackersList;

        SerializedProperty m_SelectedValidationTypeProperty;
        SerializedProperty m_ShowWarningsForShaderGraphsInPackagesProperty;

        SerializedProperty m_EnableFallbackShaderConversionProperty;
        SerializedProperty m_MaterialSwapSetsProperty;

        SerializedProperty m_EnableRuntimeValidationProperty;

        static PolySpatialSettingsEditor()
        {
            List<(string, string)> trackers = new();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IUnityObjectTracker>())
            {
                // Some can't be disabled
                if (type == typeof(GameObjectTracker))
                    continue;

                if (!PolySpatialUnityTracker.IsValidTrackerType(type))
                    continue;

                var label = type.Name;
                if (type.IsDefined(typeof(TooltipAttribute), false))
                {
                    label = type.GetAttribute<TooltipAttribute>(inherit: false).tooltip;
                }
                else
                {
                    // for now, if there is no Tooltip, then it can't be disabled
                    continue;
                }

                trackers.Add((type.FullName, label));
            }

            // sort trackers by label
            trackers.Sort((a, b) => string.Compare(a.Item2, b.Item2, StringComparison.Ordinal));

            s_TrackerTypeNames = trackers.Select(x => x.Item1).ToList();
            s_TrackerTooltips = trackers.Select(x => x.Item2).ToList();
        }

        void OnEnable()
        {
            m_DefaultVolumeCameraWindowConfigurationProperty = serializedObject.FindProperty("m_DefaultVolumeCameraWindowConfiguration");
            m_AutoCreateVolumeCameraProperty = serializedObject.FindProperty("m_AutoCreateVolumeCamera");
            m_AutoCreateVolumeCameraContent = new GUIContent("Auto-Create Volume Camera", m_AutoCreateVolumeCameraProperty.tooltip);
            m_ColliderSyncLayerMaskProperty = serializedObject.FindProperty("m_ColliderSyncLayerMask");
            m_ParticleModeProperty = serializedObject.FindProperty("m_ParticleMode");
            m_TrackLightAndReflectionProbesProperty = serializedObject.FindProperty("m_TrackLightAndReflectionProbes");

            m_TransmitDebugInfoProperty = serializedObject.FindProperty("m_TransmitDebugInfo");

            m_DisableTrackingMaskProperty = serializedObject.FindProperty("m_DisableTrackingMask");
            m_DisabledTrackersProperty = serializedObject.FindProperty("m_DisabledTrackers");
            m_DisabledTrackersList = new ReorderableList(serializedObject, m_DisabledTrackersProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawDisabledTrackersHeader,
                drawElementCallback = DrawDisabledTrackersItem,
                onAddCallback = OnAddDisabledTrackers
            };

            m_SelectedValidationTypeProperty = serializedObject.FindProperty("m_SelectedValidationType");
            m_ShowWarningsForShaderGraphsInPackagesProperty = serializedObject.FindProperty("m_ShowWarningsForShaderGraphsInPackages");
            m_EnableFallbackShaderConversionProperty = serializedObject.FindProperty("m_EnableFallbackShaderConversion");

            m_EnableRuntimeValidationProperty = serializedObject.FindProperty("m_EnableRuntimeValidation");
            m_MaterialSwapSetsProperty = serializedObject.FindProperty("m_MaterialSwapSets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DefaultVolumeCameraWindowConfigurationProperty, new GUIContent("Default Volume Camera Window Config"));
            if (m_DefaultVolumeCameraWindowConfigurationProperty.objectReferenceValue == null)
            {
                m_DefaultVolumeCameraWindowConfigurationProperty.objectReferenceValue =
                    Resources.Load<VolumeCameraWindowConfiguration>("Default Unbounded Configuration");
            }
            EditorGUILayout.PropertyField(m_AutoCreateVolumeCameraProperty, m_AutoCreateVolumeCameraContent);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ParticleModeProperty, new GUIContent("Particle System Mode"));
            EditorGUILayout.PropertyField(m_ColliderSyncLayerMaskProperty, new GUIContent("Collider Objects Layer Mask"));
            EditorGUILayout.PropertyField(m_DisableTrackingMaskProperty, new GUIContent("Ignored Objects Layer Mask"));

            EditorGUILayout.PropertyField(m_TrackLightAndReflectionProbesProperty, new GUIContent("Light and Reflection Probes"));
            EditorGUILayout.PropertyField(m_EnableFallbackShaderConversionProperty, new GUIContent("Fallback Shader Conversion"));
            EditorGUILayout.PropertyField(m_MaterialSwapSetsProperty);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_TransmitDebugInfoProperty, new GUIContent("Extra Debug Information"));
            EditorGUILayout.PropertyField(m_EnableRuntimeValidationProperty, new GUIContent("Runtime Validation"));

            EditorGUILayout.Space();

            m_DisabledTrackersList.DoLayoutList();

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_SelectedValidationTypeProperty,
                    new GUIContent("PolySpatial Project Validation"));

                if (check.changed)
                {
                    EditorUtility.RequestScriptReload();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ShowWarningsForShaderGraphsInPackagesProperty,
                new GUIContent("Warnings for ShaderGraphs in Packages"));

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
        }

        void DrawDisabledTrackersHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Disabled Features");
        }

        void DrawDisabledTrackersItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_DisabledTrackersProperty.GetArrayElementAtIndex(index);
            var typeName = element.stringValue;
            var knownIndex = s_TrackerTypeNames.IndexOf(typeName);
            var label = knownIndex != -1
                ? s_TrackerTooltips[knownIndex]
                : typeName + " (Invalid)";

            EditorGUI.LabelField(rect, label);
        }

        void OnAddDisabledTrackers(ReorderableList list)
        {
            var disabledTrackers = PolySpatialSettings.instance.DisabledTrackers;
            var menu = new GenericMenu();

            for (var i = 0; i < s_TrackerTypeNames.Count; i++)
            {
                var typeName = s_TrackerTypeNames[i];
                var label = s_TrackerTooltips[i];

                if (disabledTrackers.Contains(typeName))
                    menu.AddDisabledItem(new GUIContent(label));
                else
                    menu.AddItem(new GUIContent(label), false, () => AddDisabledTracker(list, typeName));
            }
            menu.ShowAsContext();
        }

        void AddDisabledTracker(ReorderableList list, string trackerTypeName)
        {
            list.serializedProperty.arraySize++;
            var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newElement.stringValue = trackerTypeName;

            list.serializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
        }
    }
}
