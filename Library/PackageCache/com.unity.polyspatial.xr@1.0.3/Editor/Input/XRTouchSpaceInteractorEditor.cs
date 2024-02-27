#if HAS_XR_INTERACTION_TOOLKIT
using Unity.PolySpatial.XR.Input;

namespace UnityEditor.PolySpatial.XR.Input
{
    [CustomEditor(typeof(XRTouchSpaceInteractor))]
    public class XRTouchSpaceInteractorEditor : Editor
    {
        SerializedProperty m_SpatialPointerProperty;

        void OnEnable()
        {
            m_SpatialPointerProperty = serializedObject.FindProperty("m_SpatialPointer");
        }

        public override void OnInspectorGUI()
        {
            // I am hiding everything but the SpatialPointer field as I don't
            // see why someone would want to edit the others in this
            // use case as this is essentially like an input passthrough
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_SpatialPointerProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif