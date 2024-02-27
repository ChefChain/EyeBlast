using Unity.PolySpatial;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    [CustomEditor(typeof(VolumeCameraWindowConfiguration))]
    class VolumeCameraWindowConfigurationEditor : Editor
    {
        const int k_MinInspectorWidth = 212;
        const string k_ResourcesFolderMessage = "This asset is not inside of a <b>Resources</b> folder. For this <b>VolumeCameraConfiguration</b> asset to be usable at runtime, it must be moved under a <b>Resources</b> folder.";

        SerializedProperty m_ModeProperty;
        SerializedProperty m_DimensionsProperty;
        SerializedProperty m_IsUniformScaleProperty;

        GUIContent m_DimensionsContent;
        Vector3 m_InitialDimension, m_PreviousDimension;

        internal bool ShowValidationMessage { get; set; } = true;

        static bool IsInResourcesFolder(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path) && path.Contains("/Resources/");
        }

        void OnEnable()
        {
            m_ModeProperty = serializedObject.FindProperty("m_Mode");
            m_DimensionsProperty = serializedObject.FindProperty("m_OutputDimensions");
            m_IsUniformScaleProperty = serializedObject.FindProperty("m_IsUniformScale");

            m_InitialDimension = m_DimensionsProperty.vector3Value;
            m_DimensionsContent = new GUIContent($"{m_DimensionsProperty.displayName} (meters)", m_DimensionsProperty.tooltip);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                // Adjust label to avoid content spilling over
                if (!EditorGUIUtility.wideMode)
                {
                    EditorGUIUtility.wideMode = true;
                    EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - k_MinInspectorWidth;
                }

                EditorGUILayout.PropertyField(m_ModeProperty, EditorGUIBridge.TextContent("Mode"));

                var isUniformScale = m_IsUniformScaleProperty.boolValue;
                var wasUniformScale = isUniformScale;
                var axisModified = -1;
                var toggleContent = EditorGUIUtility.TrTextContent("", (isUniformScale ? "Disable" : "Enable") + " constrained proportions");
                var position = EditorGUILayout.GetControlRect(true);

                var targetObjectMode = ((VolumeCameraWindowConfiguration)target).Mode;

                Vector3 dimensions;

                using (new EditorGUI.DisabledScope(targetObjectMode != VolumeCamera.PolySpatialVolumeCameraMode.Bounded))
                {
                    dimensions = PolySpatialEditorGUIUtils.LinkedVector3Field(position, m_DimensionsContent,
                        toggleContent, m_DimensionsProperty.vector3Value, ref isUniformScale, m_InitialDimension, 0,
                        ref axisModified, m_DimensionsProperty, m_IsUniformScaleProperty);
                }

                if (wasUniformScale != isUniformScale && isUniformScale)
                    m_InitialDimension = dimensions != Vector3.zero ? dimensions : Vector3.one;

                if (changed.changed)
                {
                    m_DimensionsProperty.vector3Value = dimensions;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (ShowValidationMessage && !IsInResourcesFolder(target))
            {
                PolySpatialEditorGUIUtils.DrawMessageBox(k_ResourcesFolderMessage, MessageType.Warning);
            }
        }
    }
}
