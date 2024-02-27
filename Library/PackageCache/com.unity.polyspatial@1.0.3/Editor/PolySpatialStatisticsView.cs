using System;
using System.Collections.Generic;
using System.IO;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityEditor.PolySpatial.Internals
{
    static class RectExtensions
    {
        internal static Rect Inset(this Rect rect, float deltaX, float deltaY)
        {
            return new Rect(rect.xMin + deltaX, rect.yMin + deltaY, rect.width - (2.0f * deltaX),
                rect.height - (2.0f * deltaY));
        }
    }

    internal static class GraphicsUtilities
    {
        internal static void BeginRendering(Rect bounds, Color clearColor, Material material)
        {
            GUI.BeginClip(bounds);
            GL.PushMatrix();
            GL.Clear(true, false, clearColor);
            material.SetPass(0);
        }

        internal static void EndRendering()
        {
            GL.PopMatrix();
            GUI.EndClip();
        }

        internal static void DrawRect(Rect r, Color c)
        {
            GL.Begin(GL.QUADS);
            GL.Color(c);
            GL.Vertex3(r.xMin, r.yMin, 0);
            GL.Vertex3(r.xMax, r.yMin, 0);
            GL.Vertex3(r.xMax, r.yMax, 0);
            GL.Vertex3(r.xMin, r.yMax, 0);
            GL.End();
        }

        internal static void DrawLine(Color c, Vector3 start, Vector3 end)
        {
            GL.Begin(GL.LINES);
            GL.Color(c);
            GL.Vertex(start);
            GL.Vertex(end);
            GL.End();
        }

        internal static void DrawLines(Color c, Vector3[] points)
        {
            if (points.Length < 2)
                return;


            GL.Begin(GL.LINES);
            GL.Color(c);
            var start = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                var end = points[i];
                GL.Vertex(start);
                GL.Vertex(end);
                start = end;
            }

            GL.End();
        }
    }

    internal class statsView : EditorWindow
    {
        bool isPlaying = false;

#if POLYSPATIAL_INTERNAL
        [MenuItem("Window/PolySpatial/Statistics")]
#endif
        private static void ShowWindow()
        {
            var window = GetWindow<statsView>();
            window.titleContent = new GUIContent("PolySpatial Statistics");
            window.Show();
        }

        // Not sure why the rendering needs this, but according to the samples
        // I found it's necessary to use and set the material when drawing to GL.
        private Material material;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        private void EditorApplicationOnplayModeStateChanged(PlayModeStateChange obj)
        {
            isPlaying = false;
            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    isPlaying = true;
                    break;
            }
        }

        private void OnDisable()
        {
            material = null;
            EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;
        }


        float MapValueInRangeToTargetRange(int v, int valueRange, float targetRange)
        {
            return MapValueInRangeToTargetRange((float)v, (float)valueRange, targetRange);
        }

        float MapValueInRangeToTargetRange(float v, float valueRange, float targetRange)
        {
            return (v / valueRange) * targetRange;
        }

        private const int k_GraphWidth = 100;
        private const int k_GraphHeight = 50;

        private const int k_ChartWidth = 100;
        private const int k_ChartExpandoWidth = 20;
        private const int k_ChartHeight = 100;

        class AssetDataDisplay
        {
            public bool foldoutStatus;
            public Vector2 scrollStatus;
        }
        Dictionary<string, AssetDataDisplay> m_AssetFoldoutStatus = new();

        private void OnGUI()
        {
            if (!PolySpatialSettings.instance.EnableStatistics)
            {
                EditorGUILayout.HelpBox("Statistics feature flag must be enabled for the active build target to view statistics.", MessageType.Warning);
                return;
            }

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("Statistics are only available at runtime.", MessageType.Info);
                return;
            }

            var stats = PolySpatialStatistics.Instance;

            if (material == null)
                material = new Material(Shader.Find("Hidden/Internal-Colored"));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(
                new GUIContent($"Change Processing Time (ms): {stats.ChangeHandlingTimeLastFrame * 1000}"));
            var boundsRect = GUILayoutUtility.GetRect(k_GraphWidth, k_GraphWidth, k_GraphHeight, k_GraphHeight,
                GUILayout.ExpandWidth(false));
            if (material && Event.current.type == EventType.Repaint)
            {
                RenderGraph(boundsRect, stats.RegisteredGameObjectsOverTime,
                    stats.MinRegisteredGameObjects, stats.MaxRegisteredGameObjects);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                new GUIContent($"Tracked Game Objects {stats.RegisteredGameObjects}"));
            boundsRect = GUILayoutUtility.GetRect(k_GraphWidth, k_GraphWidth, k_GraphHeight, k_GraphHeight,
                GUILayout.ExpandWidth(false));
            if (material && Event.current.type == EventType.Repaint)
            {
                RenderGraph(boundsRect, stats.RegisteredGameObjectsOverTime,
                    stats.MinRegisteredGameObjects, stats.MaxRegisteredGameObjects);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                new GUIContent($"Transforms Last Frame: {stats.TransformsUpdateLastFrame}"));
            boundsRect = GUILayoutUtility.GetRect(k_GraphWidth, k_GraphWidth, k_GraphHeight, k_GraphHeight,
                GUILayout.ExpandWidth(false));
            if (material && Event.current.type == EventType.Repaint)
            {
                RenderGraph(boundsRect, stats.UpdatedTransformCounts,
                    stats.MinUpdateTransformCount, stats.MaxUpdateTransformCount);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Registered Assets", EditorStyles.largeLabel);
            boundsRect = GUILayoutUtility.GetRect(k_ChartWidth,
                k_ChartWidth + (k_ChartExpandoWidth * stats.TotalRegisteredAssetsByType.Count), k_ChartHeight,
                k_ChartHeight, GUILayout.ExpandWidth(false));
            if (material && Event.current.type == EventType.Repaint)
            {
                RenderChart(stats.TotalRegisteredAssetsByType, boundsRect);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            RenderAssetTypeInfo(stats);
            EditorGUILayout.Space();
        }

        void RenderAssetTypeInfo(PolySpatialStatistics.Impl stats)
        {
            EditorGUILayout.LabelField("Assets by Type", EditorStyles.largeLabel);

            foreach (var kvp in stats.TotalRegisteredAssetsByType)
            {
                string typeName = kvp.Key;
                if (!m_AssetFoldoutStatus.TryGetValue(typeName, out var assetDataDisplay))
                {
                    assetDataDisplay = new AssetDataDisplay();
                    m_AssetFoldoutStatus[typeName] = assetDataDisplay;
                }

                assetDataDisplay.foldoutStatus = EditorGUILayout.Foldout(assetDataDisplay.foldoutStatus, $"{typeName} Assets: {stats.TotalRegisteredAssetsByType[typeName].Count}");
                if (assetDataDisplay.foldoutStatus)
                {
                    assetDataDisplay.scrollStatus = EditorGUILayout.BeginScrollView(assetDataDisplay.scrollStatus);
                    EditorGUI.indentLevel++;
                    foreach (var akvp in stats.TotalRegisteredAssetsByType[typeName])
                    {
                        var assetId = akvp.Key;
                        var refCount = akvp.Value.referenceCount;
                        var path = akvp.Value.name;
                        path = Path.GetFileName(path);
                        EditorGUILayout.LabelField($"Asset: {path}:{assetId.ToShortString()}:{refCount}");
                        var tex = PolySpatialCore.LocalAssetManager.GetRegisteredResource<Texture2D>(assetId);
                        if (tex != null)
                        {
                            var rect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100));
                            EditorGUI.DrawPreviewTexture(rect, tex);
                            EditorGUILayout.Space();
                        }
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.Space();
            }
        }

        internal void DrawGraphArea(Rect boundsRect)
        {
            GraphicsUtilities.DrawRect(boundsRect, Color.black);
            GraphicsUtilities.DrawLines(Color.white, new Vector3[]
            {
                new Vector3(boundsRect.xMin, boundsRect.yMin, 0),
                new Vector3(boundsRect.xMin, boundsRect.yMax, 0),
                new Vector3(boundsRect.xMax, boundsRect.yMax, 0),
                new Vector3(boundsRect.xMax, boundsRect.yMin, 0),
                new Vector3(boundsRect.xMin, boundsRect.yMin, 0),
            });
        }

        private const int k_BarWidth = 20;
        private const int k_BarPadding = 10;

        private static Color[] k_BarColor = new[]
        {
            Color.white,
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.gray
        };

        private void RenderChart(Dictionary<string, Dictionary<PolySpatialAssetID, PolySpatialStatistics.Impl.AssetInformation>> assetsByType, Rect boundsRect)
        {
            int maxBarHeight = 0;

            foreach (var assets in assetsByType.Values)
            {
                maxBarHeight = Math.Max(maxBarHeight, assets.Count);
            }

            GraphicsUtilities.BeginRendering(boundsRect, Color.black, material);
            try
            {
                boundsRect.x = 1;
                boundsRect.y = 1;
                DrawGraphArea(boundsRect);
                var renderArea = boundsRect.Inset(2, 2);
                var minX = renderArea.xMin + k_BarPadding;
                var renderHeight = renderArea.height - 4;
                int colorIndex = 0;

                foreach (var kvp in assetsByType)
                {
                    var barHeight = renderHeight * ((float)kvp.Value.Count / (float)maxBarHeight);
                    var barStart = renderArea.yMin + 2 + (renderHeight - barHeight);
                    var bar = new Rect(minX, barStart, k_BarWidth, barHeight);
                    GraphicsUtilities.DrawRect(bar, k_BarColor[colorIndex]);
                    colorIndex = (colorIndex + 1) % k_BarColor.Length;
                    minX += (k_BarPadding + k_BarWidth);
                }
            }
            finally
            {
                GraphicsUtilities.EndRendering();
            }
        }

        private const int k_RenderAreaInset = 10;

        private void RenderGraph(Rect boundsRect, RingBuffer<int> itemsToGraph, int minRange, int maxRange)
        {
            GraphicsUtilities.BeginRendering(boundsRect, Color.black, material);
            try
            {
                boundsRect.x = 1;
                boundsRect.y = 1;
                DrawGraphArea(boundsRect);

                var renderArea = boundsRect.Inset(2, 2);

                var start = Vector3.zero;
                var end = Vector3.zero;

                var range = maxRange - minRange;
                if (range > 0)
                {
                    List<Vector3> points = new List<Vector3>();

                    var segmentLength = renderArea.width / itemsToGraph.Size;
                    var minX = renderArea.width - (segmentLength * itemsToGraph.Count);
                    var renderHeight = renderArea.height - (2 * k_RenderAreaInset);
                    var minY = k_RenderAreaInset;
                    foreach (var curValue in itemsToGraph)
                    {
                        end.x = minX;
                        minX += segmentLength;
                        end.y = minY + renderHeight -
                                MapValueInRangeToTargetRange(curValue, range, renderHeight);
                        points.Add(end);
                    }

                    GraphicsUtilities.DrawLines(Color.red, points.ToArray());
                }
            }
            finally
            {
                GraphicsUtilities.EndRendering();
            }
        }

        private void Update()
        {
            Repaint();
        }
    }
}
