using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    class PolySpatialLoggingUI
    {
        class Styles
        {
            internal GUIContent CategoriesContent { get; } = new GUIContent("Logging Categories", "Toggle logging categories to change what is output to the console log.");
            internal GUIContent EnabledHeaderContent { get; } = new GUIContent("Enabled");
            internal GUIContent StackTraceHeaderContent { get; } = new GUIContent("StackTrace");
            internal List<LogCategory> LogCategoryList { get; }

            internal Styles()
            {
                LogCategoryList = Enum.GetValues(typeof(LogCategory)).Cast<LogCategory>().ToList();
                LogCategoryList.Sort((a,b) => string.Compare(Enum.GetName(typeof(LogCategory), a), Enum.GetName(typeof(LogCategory), b), StringComparison.Ordinal));
            }
        }

        static bool s_Initialized;
        static Styles s_Styles;
        static SavedBool s_CategoriesVisible;

        static void Initialize()
        {
            s_Initialized = true;
            s_Styles = new Styles();

            s_CategoriesVisible = new SavedBool("PolySpatial.Logging.CategoriesVisible", true);
        }

        static void DrawLoggingCategories()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(" ");
                GUILayout.Label(s_Styles.EnabledHeaderContent, GUILayout.ExpandWidth(false));
                GUILayout.Space(6f);
                GUILayout.Label(s_Styles.StackTraceHeaderContent, GUILayout.ExpandWidth(false));
            }

            foreach (var cat in s_Styles.LogCategoryList)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var catEnabled = Logging.IsCategoryEnabled(cat);
                    var stackEnabled = Logging.IsStackTraceEnabled(cat);
                    var name = Enum.GetName(typeof(LogCategory), cat);

                    EditorGUILayout.PrefixLabel(name);

                    var newCat = EditorGUILayout.Toggle(catEnabled, GUILayout.Width(64));
                    var newStack = EditorGUILayout.Toggle(stackEnabled);

                    if (newStack != stackEnabled)
                    {
                        Logging.SetStackTraceEnabled(cat, newStack);
                    }

                    if (newCat != catEnabled)
                    {
                        Logging.SetCategoryEnabled(cat, newCat);
                    }
                }
            }
        }

        internal static void DrawLoggingSettings()
        {
            if (!s_Initialized)
                Initialize();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                s_CategoriesVisible.Value = EditorGUILayout.Foldout(s_CategoriesVisible.Value, s_Styles.CategoriesContent, true);
                if (!s_CategoriesVisible.Value)
                    return;

                using (new EditorGUI.IndentLevelScope())
                {
                    DrawLoggingCategories();
                }
            }
        }
    }
}
