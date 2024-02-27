using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial.Internals;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    class AssetFinder : EditorWindow
    {
#if POLYSPATIAL_INTERNAL
        [MenuItem("Window/PolySpatial/Asset Finder")]
#endif
        private static void ShowWindow()
        {
            var window = GetWindow<AssetFinder>();
            window.titleContent = new GUIContent("PolySpatial Asset Finder");
            window.Show();
        }

        Dictionary<string, PolySpatialAssetID> shortNameToAssetId = new Dictionary<string, PolySpatialAssetID>();
        List<string> shortNames = new List<string>();

        bool shouldRefreshAssets = false;
        string searchText = "";
        ReorderableList assetList;
        int selectedShortName = -1;

        void OnEnable()
        {
            shortNames.Clear();
            shortNameToAssetId.Clear();

            assetList = new ReorderableList(shortNames, typeof(string));
            assetList.multiSelect = false;
            assetList.displayRemove = false;
            assetList.displayAdd = false;
            assetList.draggable = false;
            assetList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, $"Assets", EditorStyles.boldLabel); };
            assetList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (PolySpatialCore.LocalAssetManager == null)
                    return;

                var shortName = shortNames[index];
                var assetId = shortNameToAssetId[shortName];
                var asset = PolySpatialCore.LocalAssetManager.GetRegisteredResource(assetId);
                EditorGUI.LabelField(rect, $"{shortName}:{asset}", EditorStyles.label);
            };
            assetList.onSelectCallback = list =>
            {
                if (selectedShortName >= 0 && !list.IsSelected(selectedShortName))
                {
                    selectedShortName = -1;
                }

                var indices = list.selectedIndices;
                if (indices.Count == 0)
                {
                    return;
                }

                selectedShortName = indices[0];
            };

            PopulateAssetCache();
        }


        void OnInspectorUpdate()
        {
            PopulateAssetCache();
        }

        void PopulateAssetCache()
        {
            if (PolySpatialCore.LocalAssetManager == null)
                return;

            shortNameToAssetId.Clear();
            shortNames.Clear();
            foreach (var kvp in PolySpatialCore.LocalAssetManager.m_AssetIdToAsset)
            {
                shortNameToAssetId.Add(kvp.Key.ToShortString(), kvp.Key);
            }

            shortNames = shortNameToAssetId.Keys.ToList();
            assetList.list = shortNames;
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            {
                shouldRefreshAssets = GUILayout.Button($"Refresh");
                EditorGUILayout.Space();
                searchText = EditorGUILayout.TextField("Find:", searchText, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Search"))
                {
                    if (shortNames.Contains(searchText))
                    {
                        assetList.Select(shortNames.IndexOf(searchText));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        void DrawAssetList()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var clientRect =
                    EditorGUILayout.GetControlRect(false, 200, GUILayout.ExpandWidth(true));
                assetList.DoList(clientRect);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawPreview()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (selectedShortName >= 0 && selectedShortName < shortNames.Count)
                {
                    var shortName = shortNames[selectedShortName];
                    var assetId = shortNameToAssetId[shortName];
                    var obj = PolySpatialCore.LocalAssetManager.GetRegisteredResource(assetId);
                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.LabelField($"{obj}");
                    EditorGUILayout.Space();

                    if (Event.current.type == EventType.Repaint)
                    {
                        if (obj is Texture)
                        {
                            var rect = EditorGUILayout.GetControlRect(false, 100, GUILayout.ExpandHeight(true));
                            var tex = obj as Texture;
                            EditorGUI.DrawPreviewTexture(rect, tex);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        private void OnGUI()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                EditorGUILayout.HelpBox(new GUIContent("Only available in play mode."));
                return;
            }

            if (shouldRefreshAssets)
            {
                shouldRefreshAssets = false;
                PopulateAssetCache();
            }

            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorGUILayout.Space();
            DrawAssetList();
            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.EndVertical();
        }
    }
}
