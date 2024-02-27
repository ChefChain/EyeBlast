using UnityEditorInternal;

namespace UnityEditor.PolySpatial.Internals.InternalBridge
{
    internal static class EditorBridge
    {
        public static void AddSortingLayer()
        {
            InternalEditorUtility.AddSortingLayer();
        }
    }
}
