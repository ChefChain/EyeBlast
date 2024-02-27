using System;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals.InternalBridge
{
    /// <summary>
    /// This class acts as a bridge for accessing trunk methods, for the EditorGUI and EditorGuiUtility classes.
    /// </summary>

    static class EditorGUIBridge
    {
        // These methods/properties are related to the "linked scaling" functionality of Transforms, which we're using to apply to other/general vector3 fields.
        internal static int DefaultSpacing => EditorGUI.kDefaultSpacing;
        internal static float SingleLineHeight => EditorGUI.kSingleLineHeight;
        internal static float SpacingSubLabel => EditorGUI.kSpacingSubLabel;
        internal static int LastControlID
        {
            get => EditorGUIUtility.s_LastControlID;
            set => EditorGUIUtility.s_LastControlID = value;
        }
        internal static GUIContent BeginPropertyInternal(Rect totalPosition, GUIContent label, SerializedProperty property) =>
            EditorGUI.BeginPropertyInternal(totalPosition, label, property);
        internal static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns, float labelWidthIndent, bool setWideMode) =>
            EditorGUI.MultiFieldPrefixLabel(totalPosition, id, label, columns, labelWidthIndent, setWideMode);
        internal static void EndProperty() => EditorGUI.EndProperty();
        internal static GUIContent TextContent(string s) => EditorGUIUtility.TextContent(s);
        internal static float CalcPrefixLabelWidth(GUIContent subLabel) => EditorGUI.CalcPrefixLabelWidth(subLabel);
        internal static IDisposable CreateSettingsWindowGUIScope() => new SettingsWindow.GUIScope();
    }
}
