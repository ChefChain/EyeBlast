using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshRenderer))]
    class MeshRendererEditor : DefaultEditorWrapper
    {
        const string k_DefaultEditorTypeName = "UnityEditor.MeshRendererEditor, UnityEditor";

        SerializedProperty m_Materials;

        void OnEnable()
        {
            m_Materials = serializedObject.FindProperty(nameof(m_Materials));

            Initialize(k_DefaultEditorTypeName);
        }

        protected override void DrawSyncedProperties()
        {
            EditorGUILayout.PropertyField(m_Materials);
        }
    }
}
