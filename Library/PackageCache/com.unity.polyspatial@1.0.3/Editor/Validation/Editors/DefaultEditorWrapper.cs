using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    abstract class DefaultEditorWrapper : Editor
    {
        const string k_DefaultInspectorFoldoutLabel = "Default Inspector";
        const string k_ShowDefaultEditorPrefsKeyFormat = "PolySpatial.Validation.{0}.ShowDefaultEditor";
        const string k_DefaultEditorTypeNotLoadedErrorFormat = "Could not load the default editor type: {0}";

        static readonly Dictionary<string, Type> s_CachedDefaultEditorType = new();

        Editor m_DefaultEditor;
        SavedBool m_ShowDefaultEditor;
        string m_ErrorMessage;

        static Type GetDefaultEditorType(string editorTypeName)
        {
            if (s_CachedDefaultEditorType.TryGetValue(editorTypeName, out var defaultEditorType))
                return defaultEditorType;

            defaultEditorType = Type.GetType(editorTypeName);
            s_CachedDefaultEditorType[editorTypeName] = defaultEditorType;
            return defaultEditorType;
        }

        protected abstract void DrawSyncedProperties();

        protected void Initialize(string editorTypeName)
        {
            var showDefaultEditorPrefsKey = string.Format(k_ShowDefaultEditorPrefsKeyFormat, GetType().Name);
            m_ShowDefaultEditor = new(showDefaultEditorPrefsKey, false);

            var defaultEditorType = GetDefaultEditorType(editorTypeName);
            if (defaultEditorType == null)
            {
                m_ErrorMessage = string.Format(k_DefaultEditorTypeNotLoadedErrorFormat, editorTypeName);
                return;
            }

            m_DefaultEditor = CreateEditor(targets, defaultEditorType);
        }

        protected virtual void OnDisable()
        {
            if (m_DefaultEditor != null)
                DestroyImmediate(m_DefaultEditor);
        }

        public override void OnInspectorGUI()
        {
            if (CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile))
            {
                DrawSyncedPropertiesInternal();
                EditorGUILayout.Space();
                DrawDefaultEditorNested();
            }
            else
            {
                DrawDefaultEditor();
            }
        }

        void DrawSyncedPropertiesInternal()
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DrawSyncedProperties();

                if (changeCheck.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawDefaultEditorNested()
        {
            m_ShowDefaultEditor.Value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowDefaultEditor.Value, k_DefaultInspectorFoldoutLabel);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (!m_ShowDefaultEditor.Value)
                return;

            // EditorGUI.indentLevel does not play nice with FoldoutHeaderGroups, so we use Vertical and Horizontal layout groups to ensure consistent layout.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            DrawDefaultEditor();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                InspectorValidationUI.DrawInspectorTypeMessages(this);
        }

        void DrawDefaultEditor()
        {
            if (m_DefaultEditor == null)
                EditorGUILayout.HelpBox(m_ErrorMessage, MessageType.Error);
            else
                m_DefaultEditor.OnInspectorGUI();
        }
    }
}
