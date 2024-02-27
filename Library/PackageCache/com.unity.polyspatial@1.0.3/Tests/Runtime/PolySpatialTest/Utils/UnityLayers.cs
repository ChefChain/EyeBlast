#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    internal static class UnityLayers
    {
        private static MethodInfo AddSortingLayer_Method;

        /// <summary>
        /// Adds a sorting layer to the Tag Manager using script. This is done through
        /// Scriptable Object/Property manipulation as there is no way to directly
        /// add a sorting layer any other way.
        /// </summary>
        /// <param name="tagManager">Instance of the TagManager asset that is to be modified.</param>
        /// <param name="layerName">The name of the sorting layer to add.</param>
        /// <returns>True if the layer was successfully added.</returns>
        internal static bool AddSortingLayer(SerializedObject tagManager, string layerName)
        {
            // There need to be a better way to do this, but there isn't anything like it on the C# side
            bool assigned = false;

            if (SortingLayer.NameToID(layerName) > 0)
                return true;

            if (AddSortingLayer_Method == null)
            {
                var tagManagerType = tagManager.targetObject.GetType();
                AddSortingLayer_Method = tagManagerType.GetMethod("AddSortingLayer", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (AddSortingLayer_Method == null)
                return assigned;
            AddSortingLayer_Method.Invoke(tagManager.targetObject, null);
            tagManager.Update();

            var sortingLayers = tagManager.FindProperty("m_SortingLayers");
            int idx = sortingLayers.arraySize - 1;
            if (idx <= 0)
                return assigned;

            SerializedProperty sortingLayer = sortingLayers.GetArrayElementAtIndex(idx);
            SerializedProperty name = sortingLayer.FindPropertyRelative("name");
            if (name == null)
                return assigned;

            name.stringValue = layerName;

            tagManager.ApplyModifiedProperties();
            assigned = true;

            return assigned;
        }

    }
}
#endif
