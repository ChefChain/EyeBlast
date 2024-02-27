using System;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEngine;

namespace UnityEditor.PolySpatial.Utilities
{
    /// <summary>
    /// Utility class for Editor GUI.
    /// </summary>
    static class PolySpatialEditorGUIUtils
    {
        static readonly int k_FoldoutHash = "Foldout".GetHashCode();
        static readonly float[] k_Vector3Floats = { 0, 0, 0 };
        static readonly Lazy<GUIContent[]> k_XYZLabels = new(() => new[]
            { EditorGUIBridge.TextContent("X"), EditorGUIBridge.TextContent("Y"), EditorGUIBridge.TextContent("Z") });

        class Styles
        {
            internal Styles()
            {
                m_IconInfo = EditorGUIUtility.IconContent("console.infoicon").image as Texture2D;
                m_IconWarn = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                m_IconFail = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;

                LinkButton = new GUIStyle("FloatFieldLinkButton") {alignment = TextAnchor.MiddleCenter};
            }

            readonly Texture2D m_IconWarn;
            readonly Texture2D m_IconInfo;
            readonly Texture2D m_IconFail;

            internal readonly GUIStyle LinkButton;

            /// <summary>
            /// Gets the icon that describes the <see cref="MessageType"/>
            /// </summary>
            /// <param name="messageType">The <see cref="MessageType"/> to obtain the icon from</param>
            /// <returns>a <see cref="Texture2D"/> with the icon for the <see cref="MessageType"/></returns>
            internal Texture2D GetMessageTypeIcon(MessageType messageType)
            {
                switch (messageType)
                {
                    case MessageType.None:
                        return null;
                    case MessageType.Info:
                        return m_IconInfo;
                    case MessageType.Warning:
                        return m_IconWarn;
                    case MessageType.Error:
                        return m_IconFail;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
                }
            }
        }

        static Styles s_Styles;

        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        /// <summary>
        /// Draw a help box with the Fix button, an optional link and returns whether the user clicked in the button.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="buttonTooltip">The button tooltip.</param>
        /// <param name="helpText">The link title that the user will click on</param>
        /// <param name="helpLink">The link url</param>
        /// <returns>Returns <see langword="true"/> when the user clicks the Fix button. Otherwise, returns <see langword="false"/>.</returns>
        internal static bool DrawFixMeBox(string message, MessageType messageType, string buttonLabel, string buttonTooltip, string helpText, string helpLink)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(styles.GetMessageTypeIcon(messageType), GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label(message, EditorGUIUtils.Styles.WordWrapMiniLabel);
            if (!string.IsNullOrEmpty(helpText) && !string.IsNullOrEmpty(helpLink))
                EditorGUIUtils.DrawLink(new GUIContent(helpText), helpLink);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var buttonContent = EditorGUIUtility.TrTextContent(buttonLabel, buttonTooltip);
            var clicked = GUILayout.Button(buttonContent, GUILayout.MinWidth(60));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            return clicked;
        }

        /// <summary>
        /// Draws a message box with the given message and type and adds an optional link at the end if one is passed.
        /// </summary>
        /// <param name="message">The message text, supports rich text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="linkTittle">The link title that the user will click on</param>
        /// <param name="linkURL">The link url</param>
        internal static void DrawMessageBox(string message, MessageType messageType, string linkTittle = "", string linkURL = "")
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(styles.GetMessageTypeIcon(messageType), GUILayout.ExpandWidth(false));

            GUILayout.BeginVertical();
            GUILayout.Label(message, EditorGUIUtils.Styles.WordWrapMiniLabel);
            if (!string.IsNullOrEmpty(linkTittle) && !string.IsNullOrEmpty(linkURL))
                EditorGUIUtils.DrawLink(new GUIContent(linkTittle), linkURL);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        internal static Vector3 LinkedVector3Field(Rect position, GUIContent label, GUIContent toggleContent, Vector3 value, ref bool isUniformScale,
            Vector3 initialScale, uint mixedValues, ref int axisModified, SerializedProperty vectorProperty = null,
            SerializedProperty isUniformScaleProperty = null)
        {
            var fullLabelRect = position;

            EditorGUI.BeginChangeCheck();

            if (isUniformScaleProperty != null)
                EditorGUIBridge.BeginPropertyInternal(fullLabelRect, label, isUniformScaleProperty);

            var scalePropertyId = -1;
            if (vectorProperty != null)
            {
                label = EditorGUIBridge.BeginPropertyInternal(position, label, vectorProperty);
                scalePropertyId = GUIUtility.keyboardControl;
            }

            var copiedProperty = vectorProperty?.Copy();
            var toggle = EditorStyles.toggle.CalcSize(GUIContent.none);
            var id = GUIUtility.GetControlID(k_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, id, label, 3, toggle.x + EditorGUIBridge.DefaultSpacing, false);
            var toggleRect = position;
            toggleRect.width = toggle.x;
            var toggleOffset = toggleRect.width + EditorGUIBridge.DefaultSpacing;
            toggleRect.x -= toggleOffset;
            toggle.x -= toggleOffset;

            // In case we have a background overlay, make sure constrain proportions toggle won't be affected
            var currentColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            var previousProportionalScale = isUniformScale;
            isUniformScale = GUI.Toggle(toggleRect, isUniformScale, toggleContent, styles.LinkButton);

            if (isUniformScaleProperty != null && previousProportionalScale != isUniformScale)
                isUniformScaleProperty.boolValue = isUniformScale;

            GUI.backgroundColor = currentColor;

            position.x += toggle.x + EditorGUIBridge.DefaultSpacing;
            position.width -= toggle.x + EditorGUIBridge.DefaultSpacing;
            position.height = EditorGUIBridge.SingleLineHeight;

            if (isUniformScaleProperty != null)
                EditorGUIBridge.EndProperty();

            if (vectorProperty != null)
            {
                // Note: due to how both the scale + constrainScale property drawn and handled in a custom fashion, the lastcontrolId never correspond
                // to the scaleProperty. Also s_PendingPropertyKeyboardHandling is nullifed by the constrainScale property.
                // Make it work for now but I feel this whole system is super brittle.
                // This will be hopefully fixed up when we use uitk to create these editors.

                var lastId = EditorGUIBridge.LastControlID;
                EditorGUIBridge.LastControlID = scalePropertyId;
                EditorGUIBridge.EndProperty();
                EditorGUIBridge.LastControlID = lastId;
            }

            var newValue = LinkedVector3Field(position, value, isUniformScale, mixedValues, initialScale, ref axisModified, copiedProperty);
            return newValue;
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        static Vector3 LinkedVector3Field(Rect position, Vector3 value, bool isUniformScale, uint mixedValues, Vector3 initialValue, ref int axisModified,
            SerializedProperty property = null)
        {
            var valueAfterChangeCheck = value;
            k_Vector3Floats[0] = value.x;
            k_Vector3Floats[1] = value.y;
            k_Vector3Floats[2] = value.z;
            position.height = EditorGUIBridge.SingleLineHeight;
            const float kPrefixWidthOffset = 3.65f;

            var labels = k_XYZLabels.Value;
            LockingMultiFloatFieldInternal(position, isUniformScale, mixedValues, labels, k_Vector3Floats,
                new[] { initialValue.x, initialValue.y, initialValue.z }, property,
                EditorGUIBridge.CalcPrefixLabelWidth(labels[0]) + kPrefixWidthOffset);

            if (EditorGUI.EndChangeCheck())
            {
                valueAfterChangeCheck.x = k_Vector3Floats[0];
                valueAfterChangeCheck.y = k_Vector3Floats[1];
                valueAfterChangeCheck.z = k_Vector3Floats[2];
            }

            return isUniformScale ? DoScaleProportions(valueAfterChangeCheck, value, initialValue, ref axisModified) : valueAfterChangeCheck;
        }

        static void LockingMultiFloatFieldInternal(Rect position, bool locked, uint mixedValues, GUIContent[] subLabels, float[] values,
            float[] initialValues = null, SerializedProperty property = null, float prefixLabelWidth = -1)
        {
            var eCount = values.Length;
            var w = (position.width - (eCount - 1) * EditorGUIBridge.SpacingSubLabel) / eCount;
            var nr = new Rect(position) { width = w };
            var t = EditorGUIUtility.labelWidth;
            var l = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var guiEnabledState = GUI.enabled;
            var hasMixedValues = mixedValues != 0;

            initialValues ??= values;

            var mixedValueState = EditorGUI.showMixedValue;

            for (int i = 0; i < initialValues.Length; i++)
            {
                if (property != null)
                    property.Next(true);

                EditorGUIUtility.labelWidth = prefixLabelWidth > 0 ? prefixLabelWidth : EditorGUIBridge.CalcPrefixLabelWidth(subLabels[i]);

                if (guiEnabledState)
                {
                    if (locked)
                    {
                        // If initial field value is 0, it must be locked not to break proportions
                        GUI.enabled = !Mathf.Approximately(initialValues[i], 0) && property != null;
                    }
                    else
                    {
                        GUI.enabled = true;
                    }
                }

                if (property != null)
                {
                    EditorGUI.PropertyField(nr, property, subLabels[i]);
                    values[i] = property.floatValue;
                }
                else
                {
                    values[i] = EditorGUI.FloatField(nr, subLabels[i], values[i]);
                }

                if (hasMixedValues)
                    EditorGUI.showMixedValue = false;

                nr.x += w + EditorGUIBridge.SpacingSubLabel;
            }

            GUI.enabled = guiEnabledState;
            EditorGUI.showMixedValue = mixedValueState;
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
        }

        // The following methods are modified/adapted versions of methods from trunk scripts: EditorGUI & ConstrainProportionTransformScale
        // The "linked-scaling" functionality in trunk is hardcoded to work only with rect/transform "Scale" property.
        // The comments inside of these methods are copied from the original scripts.
        static Vector3 DoScaleProportions(Vector3 value, Vector3 previousValue, Vector3 initialValue, ref int axisModified)
        {
            float ratio = 1;
            var ratioChanged = false;

            if (previousValue != value)
            {
                // Check which axis was modified and set locked fields and ratio
                //AxisModified values [-1;2] : [none, x, y, z]
                // X axis
                ratio = SetRatio(value.x, previousValue.x, initialValue.x);
                axisModified = ratio != 1 || !Mathf.Approximately(value.x, previousValue.x) ? 0 : -1;
                // Y axis
                if (axisModified == -1)
                {
                    ratio = SetRatio(value.y, previousValue.y, initialValue.y);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.y, previousValue.y) ? 1 : -1;
                }

                // Z axis
                if (axisModified == -1)
                {
                    ratio = SetRatio(value.z, previousValue.z, initialValue.z);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.z, previousValue.z) ? 2 : -1;
                }

                ratioChanged = true;
            }

            return ratioChanged ? GetVector3WithRatio(initialValue, ratio) : value;
        }

        static float SetRatio(float value, float previousValue, float initialValue)
        {
            return Mathf.Approximately(value, previousValue) ? 1 : Mathf.Approximately(initialValue, 0) ? 0 : value / initialValue;
        }

        static Vector3 GetVector3WithRatio(Vector3 vector, float ratio)
        {
            //If there are any fields with the same values, use already precalculated values.
            var xValue = vector.x * ratio;
            var yValue = vector.y * ratio;

            return new Vector3(
                xValue,
                Mathf.Approximately(vector.y, vector.x) ? xValue : yValue,
                Mathf.Approximately(vector.z, vector.x) ? xValue : Mathf.Approximately(vector.z, vector.y) ? yValue : vector.z * ratio
            );
        }
    }
}
