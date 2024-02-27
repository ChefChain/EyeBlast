using System;
using System.Collections.Generic;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.PolySpatial
{
    /// <summary>
    /// Simple stats window to get visibility into what the UnitySceneGraph is up to.
    /// </summary>
    internal class PolySpatialUSGPlatformStatistics : EditorWindow
    {

        Dictionary<string, Vector2> m_AssetScrollPosition = new();
        Vector2 m_WindowScrollPos = Vector2.zero;

#if POLYSPATIAL_INTERNAL
        [MenuItem("Window/PolySpatial/USG Platform Statistics")]
#endif
        private static void ShowWindow()
        {
            var window = GetWindow<PolySpatialUSGPlatformStatistics>();
            window.titleContent = new GUIContent("USG Platform Statistics");
            window.Show();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Content only visible while playing.", MessageType.Info);
                return;
            }

            if (!PolySpatialSettings.instance.EnableStatistics)
            {
                EditorGUILayout.HelpBox("Statistics must be enabled in PolySpatial Settings.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            m_WindowScrollPos = EditorGUILayout.BeginScrollView(m_WindowScrollPos);
            {
                EditorGUILayout.LabelField("Assets by Type", EditorStyles.largeLabel);
                EditorGUILayout.Space();
                foreach (var kvp in m_TrackedAssetInformation)
                {
                    Vector2 scrollPos;
                    if (!m_AssetScrollPosition.TryGetValue(kvp.Key.Name, out scrollPos))
                    {
                        scrollPos = Vector2.zero;
                        m_AssetScrollPosition[kvp.Key.Name] = scrollPos;
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"{kvp.Key.Name}: {kvp.Value.Count}");
                    EditorGUILayout.Space();
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    m_AssetScrollPosition[kvp.Key.Name] = scrollPos;
                    if (kvp.Key.Name.Contains("Texture"))
                    {
                        foreach (var asset in kvp.Value)
                        {
                            if (asset is Texture2D tex)
                            {
                                EditorGUILayout.LabelField(asset.name);
                                var rect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100));
                                EditorGUI.DrawPreviewTexture(rect, tex);
                                EditorGUILayout.Space();
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        Dictionary<Type, List<UnityObject>> m_TrackedAssetInformation = new();

        void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                m_TrackedAssetInformation.Clear();
                var pam = UnitySceneGraphAssetManager.Shared;

                foreach (var obj in pam.RegisteredAssets)
                {
                    var t = obj.GetType();

                    if (!m_TrackedAssetInformation.TryGetValue(t, out var assets))
                    {
                        assets = new List<UnityObject>();
                        m_TrackedAssetInformation.Add(t, assets);
                    }

                    assets.Add(obj);
                }

                Repaint();
            }
        }
    }
}
