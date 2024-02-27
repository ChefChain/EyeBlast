using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    [CustomEditor(typeof(VolumeCamera))]
    class VolumeCameraEditor : Editor
    {
        const int k_MinInspectorWidth = 212;
        static readonly Color k_BoundsHandleColor = new Color(1, 1, 1, 0.7f);

        SerializedProperty m_IsUniformScaleProperty;
        SerializedProperty m_DimensionsProperty;
        SerializedProperty m_CullingMaskProperty;
        SerializedProperty m_ConfigurationProperty;
        SerializedProperty m_OpenWindowOnLoadProperty;

        SerializedProperty m_OnWindowOpenedProperty;
        SerializedProperty m_OnWindowClosedProperty;
        SerializedProperty m_OnWindowResizedProperty;
        SerializedProperty m_OnWindowFocusedProperty;

        SerializedProperty m_ShowVolCamEvtsFoldoutProperty;

        BoxBoundsHandle m_BoundsHandle = new();
        GUIContent m_DimensionsContent;
        Vector3 m_InitialDimension, m_PreviousDimension;
        Editor m_VolumeCameraConfigEditor;

        void OnEnable()
        {
            m_IsUniformScaleProperty = serializedObject.FindProperty("m_IsUniformScale");
            m_DimensionsProperty = serializedObject.FindProperty("m_Dimensions");
            m_CullingMaskProperty = serializedObject.FindProperty("CullingMask");
            m_ConfigurationProperty = serializedObject.FindProperty("m_OutputConfiguration");
            m_OpenWindowOnLoadProperty = serializedObject.FindProperty("OpenWindowOnLoad");

            m_OnWindowOpenedProperty = serializedObject.FindProperty("OnWindowOpened");
            m_OnWindowClosedProperty = serializedObject.FindProperty("OnWindowClosed");
            m_OnWindowResizedProperty = serializedObject.FindProperty("OnWindowResized");
            m_OnWindowFocusedProperty = serializedObject.FindProperty("OnWindowFocused");

            m_ShowVolCamEvtsFoldoutProperty = serializedObject.FindProperty("m_ShowVolumeCameraEventsFoldout");

            m_InitialDimension = m_DimensionsProperty.vector3Value;

            m_BoundsHandle.wireframeColor = k_BoundsHandleColor;
            m_BoundsHandle.handleColor = k_BoundsHandleColor;

            m_DimensionsContent = new GUIContent(m_DimensionsProperty.displayName, m_DimensionsProperty.tooltip);
        }

        void OnDisable()
        {
            if (m_VolumeCameraConfigEditor != null)
            {
                DestroyImmediate(m_VolumeCameraConfigEditor);
                m_VolumeCameraConfigEditor = null;
            }
        }

        void OnSceneGUI()
        {
            var volumeCamera = (VolumeCamera)target;
            if (volumeCamera == null || volumeCamera.WindowMode != VolumeCamera.PolySpatialVolumeCameraMode.Bounded)
                return;

            var initialDimensions = m_DimensionsProperty.vector3Value == Vector3.zero ? Vector3.one : m_DimensionsProperty.vector3Value;
            m_BoundsHandle.size = initialDimensions;
            m_BoundsHandle.center = Vector3.zero;

            Handles.matrix = volumeCamera.transform.localToWorldMatrix;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                m_BoundsHandle.DrawHandle();

                if (check.changed)
                {
                    if (m_IsUniformScaleProperty.boolValue)
                    {
                        // If uniform scale is enabled, we need to make sure that the user is only able to scale the volume uniformly.
                        var scale = 1f;
                        var deltaSize = m_BoundsHandle.size - initialDimensions;
                        if (deltaSize.x != 0 && initialDimensions.x != 0)
                            scale = m_BoundsHandle.size.x / initialDimensions.x;
                        else if (deltaSize.y != 0 && initialDimensions.y != 0)
                            scale = m_BoundsHandle.size.y / initialDimensions.y;
                        else if (deltaSize.z != 0 && initialDimensions.z != 0)
                            scale = m_BoundsHandle.size.z / initialDimensions.z;

                        m_BoundsHandle.size = initialDimensions * scale;
                    }

                    Undo.RecordObject(target, "Changed Volume Dimensions");
                    m_DimensionsProperty.vector3Value = m_BoundsHandle.size;
                    serializedObject.ApplyModifiedProperties();
                }
            }
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

                EditorGUILayout.PropertyField(m_CullingMaskProperty, EditorGUIBridge.TextContent("Culling Mask"));

                var isUniformScale = m_IsUniformScaleProperty.boolValue;
                var wasUniformScale = isUniformScale;
                var axisModified = -1;
                var toggleContent = EditorGUIUtility.TrTextContent("", (isUniformScale ? "Disable" : "Enable") + " constrained proportions");
                var position = EditorGUILayout.GetControlRect(true);

                var isUnboundedCameraMode = m_ConfigurationProperty.objectReferenceValue is VolumeCameraWindowConfiguration volumeConfiguration && volumeConfiguration != null &&
                                          volumeConfiguration.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded;
                Vector3 dimensions;
                using (new EditorGUI.DisabledScope(isUnboundedCameraMode))
                {
                    dimensions = PolySpatialEditorGUIUtils.LinkedVector3Field(position, m_DimensionsContent,
                        toggleContent, m_DimensionsProperty.vector3Value, ref isUniformScale, m_InitialDimension, 0,
                        ref axisModified, m_DimensionsProperty, m_IsUniformScaleProperty);
                }

                if (wasUniformScale != isUniformScale && isUniformScale)
                    m_InitialDimension = dimensions != Vector3.zero ? dimensions : Vector3.one;

                EditorGUILayout.PropertyField(m_OpenWindowOnLoadProperty, EditorGUIBridge.TextContent("Open Window On Load"));

                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.ObjectField(m_ConfigurationProperty, typeof(VolumeCameraWindowConfiguration),
                        EditorGUIBridge.TextContent("Volume Window Configuration"));

                    if (m_ConfigurationProperty.objectReferenceValue != null)
                    {
                        var config = (VolumeCameraWindowConfiguration) m_ConfigurationProperty.objectReferenceValue;
                        CreateCachedEditor(config, null, ref m_VolumeCameraConfigEditor);
                        if (m_VolumeCameraConfigEditor is VolumeCameraWindowConfigurationEditor configEditor)
                            configEditor.ShowValidationMessage = false;
                        using (new EditorGUI.DisabledScope(true))
                        using (new EditorGUI.IndentLevelScope())
                        {
                            m_VolumeCameraConfigEditor.OnInspectorGUI();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This volume camera does not have a configuration assigned. " +
                                                "The initial configuration specified in XR Plug-in Management settings for the platform will be used.",
                            MessageType.Info);
                    }
                }

                EditorGUILayout.Separator();

                m_ShowVolCamEvtsFoldoutProperty.boolValue = EditorGUILayout.Foldout(m_ShowVolCamEvtsFoldoutProperty.boolValue, "Events", true);
                if (m_ShowVolCamEvtsFoldoutProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(m_OnWindowOpenedProperty);
                    EditorGUILayout.PropertyField(m_OnWindowClosedProperty);
                    EditorGUILayout.PropertyField(m_OnWindowResizedProperty);
                    EditorGUILayout.PropertyField(m_OnWindowFocusedProperty);
                }

                if (changed.changed)
                {
                    m_DimensionsProperty.vector3Value = dimensions;
                    serializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying && PolySpatialRuntime.Enabled)
                    {
                        var volumeCamera = (VolumeCamera)target;
                        volumeCamera.UpdateConfiguration();
                    }
                }
            }
        }
    }
}
