using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that draws the PolySpatial UI validation in the Hierarchy window.
    /// </summary>
    [InitializeOnLoad]
    class HierarchyValidationUI
    {
        const float k_WarnIconLeftPadding = -28f;
        const float k_WarnIconWidth = 18f;

        class Styles
        {
            internal GUIContent WarningIcon;
            internal GUIContent WarningIconInactive;

            internal Styles()
            {
                WarningIcon = EditorGUIUtility.IconContent("console.warnicon.sml@2x");
                WarningIconInactive = EditorGUIUtility.IconContent("console.warnicon.inactive.sml@2x");
            }
        }

        static Styles s_Styles;

        static Styles styles => s_Styles ??= new Styles();

        static HierarchyValidationUI()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            if (PolySpatialSceneValidator.IsValidGameObject(instanceID))
            {
                if (PolySpatialSceneValidator.IsRootObjectWithFailureChildren(instanceID))
                    DrawRootIssueIcon(selectionRect, instanceID);
                return;
            }

            DrawIssueIcon(selectionRect, styles.WarningIcon);
        }

        static void DrawRootIssueIcon(Rect selectionRect, int instanceID)
        {
            var iconRect = DrawIssueIcon(selectionRect, styles.WarningIconInactive);

            var currentEvt = Event.current;
            if (currentEvt.type == EventType.MouseDown && currentEvt.button == 0 && iconRect.Contains(currentEvt.mousePosition))
            {
                currentEvt.Use();
                Selection.activeInstanceID = PolySpatialSceneValidator.GetNextFailureObjectID(instanceID);
            }
        }

        static Rect DrawIssueIcon(Rect selectionRect, GUIContent content)
        {
            var iconRect = new Rect(selectionRect) {x = selectionRect.x + k_WarnIconLeftPadding, width = k_WarnIconWidth};
            GUI.Label(iconRect, content);
            return iconRect;
        }
    }
}
