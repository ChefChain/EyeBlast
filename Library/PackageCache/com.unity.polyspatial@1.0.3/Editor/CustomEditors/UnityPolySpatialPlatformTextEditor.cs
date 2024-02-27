using System;
using System.Security.Permissions;
using TMPro;
using Unity.PolySpatial;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.Internals
{
    [CustomEditor(typeof(VisionOSNativeText))]
    public class UnityPolySpatialPlatformTextEditor : Editor
    {
        static string s_Uxml = "EditorUI/psl-platform-text";
        static string s_Uss = "EditorUI/psl-platform-text-style";

        RectIntField m_Insets;
        Vector2Field m_CanvasSize;
        PropertyField m_TmpFontAsset;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement customInspector = new VisualElement();
            var visualTree = Resources.Load(s_Uxml) as VisualTreeAsset;
            visualTree.CloneTree(customInspector);
            customInspector.styleSheets.Add(Resources.Load(s_Uss) as StyleSheet);

            m_Insets = customInspector.Q<RectIntField>(name: "Insets");
            if (m_Insets != null)
                m_Insets.RegisterValueChangedCallback(OnInsetsFieldChanged);

            m_CanvasSize = customInspector.Q<Vector2Field>(name: "CanvasSize");
            if (m_CanvasSize != null)
                m_CanvasSize.RegisterValueChangedCallback(OnCanvasSizeChanged);

            m_TmpFontAsset = customInspector.Q<PropertyField>(name: "TmProFontAsset");
            if (m_TmpFontAsset != null)
                m_TmpFontAsset.RegisterValueChangeCallback(OnTmpFontAssetChanged);

            return customInspector;
        }

        void OnCanvasSizeChanged(ChangeEvent<Vector2> evt)
        {
            if (m_CanvasSize == null)
                return;

            var canvasSize = evt.newValue;

            if (canvasSize.x < 1)
                canvasSize.x = 1;

            if (canvasSize.y < 1)
                canvasSize.y = 1;

            m_CanvasSize.SetValueWithoutNotify(canvasSize);
        }

        void OnInsetsFieldChanged(ChangeEvent<RectInt> evt)
        {
            if (m_Insets == null)
                return;

            var text = target as VisionOSNativeText;
            if (text == null)
                return;

            var insets = evt.newValue;
            var canvasSize = text.CanvasSize;

            // Currently we are bound by the types used in the
            // controls for UIElements. To ease confusion we
            // rename the values to be more clear.
            var width = (int)canvasSize.x;
            var height = (int)canvasSize.y;

            var left = Math.Max(0, insets.x);
            var right = Math.Max(0, insets.width);
            var top = Math.Max(0, insets.y);
            var bottom = Math.Max(0, insets.height);

            left = Math.Max(0, Math.Min(left, width - right - 1));
            top = Math.Max(0, Math.Min(top, height - bottom - 1));
            right = Math.Max(0, Math.Min(right, width - left - 1));
            bottom = Math.Max(0, Math.Min(bottom, height - top - 1));

            insets.x = left;
            insets.y = top;
            insets.width = right;
            insets.height = bottom;

            m_Insets.SetValueWithoutNotify(insets);
        }

        void OnTmpFontAssetChanged(SerializedPropertyChangeEvent evt)
        {
            if (m_TmpFontAsset == null)
                return;

            var text = target as VisionOSNativeText;
            if (text == null)
                return;

            text.UpdateFontAssets();
        }
    }
}
