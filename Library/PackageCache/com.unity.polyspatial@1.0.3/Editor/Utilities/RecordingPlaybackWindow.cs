using System;
using System.IO;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.PolySpatial.Utilities
{
    class RecordingPlaybackWindow : EditorWindow
    {
        const string k_WindowTitle = "Recording and Playback";
        const string k_MenuItemName = "Window/PolySpatial/" + k_WindowTitle;
        const string k_RecordingBaseDir = "Library/PolySpatialRecordings";

        class WindowGUIScope : GUI.Scope
        {
            float m_LabelWidth;
            public WindowGUIScope()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(6f);
                GUILayout.BeginVertical();
                GUILayout.Space(6f);
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();
            }
        }

        // what scene to return to after we exit playback, if not null or empty
        [SerializeField]
        SceneSetup[] m_ScenesForExitPlayback;

        [SerializeField]
        Vector2 m_ScrollPosition;

        PolySpatialEditorPlaybackManager m_PlaybackManager;
        ReorderableList m_RecordingsList;

        [NonSerialized]
        string[] m_AvailableRecordings;

        [SerializeField]
        int m_TargetFrameRate = 0;

        // we use this to prevent the UI to display wrong information when the user is switching from edit to play mode
        bool m_IsInPlayMode;

        string[] AvailableRecordings()
        {
            if (m_AvailableRecordings != null)
                return m_AvailableRecordings;

            if (Directory.Exists(k_RecordingBaseDir))
                m_AvailableRecordings = Directory.GetFiles(k_RecordingBaseDir, "*.qrec");
            else
                m_AvailableRecordings = Array.Empty<string>();

            return m_AvailableRecordings;
        }

        [MenuItem(k_MenuItemName)]
        static void Init()
        {
            var instance = GetWindow<RecordingPlaybackWindow>(k_WindowTitle);
            instance.Show();
        }

        void OnEnable()
        {
            // Force refreshing the list
            m_AvailableRecordings = null;
            m_RecordingsList = new ReorderableList(AvailableRecordings(), typeof(string), false, false, false, true);
            m_RecordingsList.drawElementCallback += DrawRecordingItem;
            m_RecordingsList.onRemoveCallback += OnRemoveRecording;
            m_RecordingsList.onSelectCallback += OnSelectRecording;
            m_RecordingsList.index = Array.IndexOf(m_AvailableRecordings, PolySpatialSettings.SessionRecordingFilePath);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnDestroy()
        {
            PolySpatialSettings.EraseSessionRecordingMode();
        }

        void DrawRecordingItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= m_AvailableRecordings.Length)
                    return;

            var recordingPath = m_AvailableRecordings[index];
            var recordingName = Path.GetFileNameWithoutExtension(recordingPath);
            EditorGUI.LabelField(rect, recordingName);
        }

        void OnRemoveRecording(ReorderableList list)
        {
            if (list.index < 0 || list.index > m_AvailableRecordings.Length || m_AvailableRecordings[list.index] == null
                || !EditorUtility.DisplayDialog("Delete Recording", "Are you sure you want to delete this recording? This action cannot be undone.", "Delete", "Cancel"))
                return;

            var recordingPath = m_AvailableRecordings[list.index];
            File.Delete(recordingPath);
            RefreshRecordingList();
        }

        void OnSelectRecording(ReorderableList list)
        {
            if (list.index < 0 || list.index > m_AvailableRecordings.Length || m_AvailableRecordings[list.index] == null)
                return;

            PolySpatialSettings.SessionRecordingFilePath = m_AvailableRecordings[list.index];
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    m_IsInPlayMode = true;
                    if (PolySpatialRuntime.Enabled && PolySpatialCore.Instance.ActiveRecordingMode == PolySpatialSettings.RecordingMode.Playback)
                        m_PlaybackManager = FindFirstObjectByType<PolySpatialEditorPlaybackManager>();

                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                    m_IsInPlayMode = false;
                    PolySpatialSettings.EraseSessionRecordingMode();
                    RefreshRecordingList();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                {
                    if (m_ScenesForExitPlayback != null && m_ScenesForExitPlayback.Length > 0)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(m_ScenesForExitPlayback);
                        m_ScenesForExitPlayback = Array.Empty<SceneSetup>();
                    }

                    break;
                }
            }
        }

        void OnGUI()
        {
            using (new WindowGUIScope())
            {
                if (!EditorApplication.isPlaying)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Record"))
                            EditorApplication.delayCall += Record;

                        // can only play if there's a path selected
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(PolySpatialSettings.SessionRecordingFilePath)))
                        {
                            if (GUILayout.Button("Play"))
                                EditorApplication.delayCall += Play;
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Recording Framerate", GUILayout.Width(150));
                        m_TargetFrameRate = EditorGUILayout.IntField(m_TargetFrameRate);

                        if (m_TargetFrameRate < 0)
                            m_TargetFrameRate = 0;
                    }

                    using (new EditorGUI.DisabledScope(m_TargetFrameRate <= 0))
                    {
                        EditorGUIUtility.labelWidth = position.width - 35;
                        PolySpatialSettings.SessionLimitFramerateWhenRecording =
                            EditorGUILayout.Toggle("Limit Framerate While Recording", PolySpatialSettings.SessionLimitFramerateWhenRecording);
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Stop Recording"))
                        {
                            EditorApplication.delayCall += () => { EditorApplication.ExitPlaymode(); };
                        }

                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUILayout.Button("Play");
                        }
                    }
                }

                DrawInfoBox();
                EditorGUILayout.Space();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    m_ScrollPosition = scrollView.scrollPosition;
                    m_RecordingsList.DoLayoutList();
                }
            }
        }

        void DrawInfoBox()
        {
            if (m_IsInPlayMode)
            {
                if (!PolySpatialRuntime.Enabled)
                    return;

                switch (PolySpatialCore.Instance.ActiveRecordingMode)
                {
                    case PolySpatialSettings.RecordingMode.Record:
                        EditorGUILayout.HelpBox("Recording...", MessageType.Info);
                        break;
                    case PolySpatialSettings.RecordingMode.Playback when m_PlaybackManager != null && m_PlaybackManager.enabled:
                        EditorGUILayout.HelpBox("Playing...", MessageType.Info);
                        break;
                    case PolySpatialSettings.RecordingMode.Playback:
                        EditorGUILayout.HelpBox("Finished playback.", MessageType.Info);
                        break;
                }
            }
            else
            {
                // Only showing this message if there are no recordings available
                var msg = AvailableRecordings().Length == 0 ? "Press Record to enter play mode and start recording. " : "";
                EditorGUILayout.HelpBox($"{msg}Framerate is set to record at: {GetFpsMessage()}", MessageType.Info);
            }
        }

        private string GetFpsMessage()
        {
            if (m_TargetFrameRate == 0 || !PolySpatialSettings.SessionLimitFramerateWhenRecording)
                return "Unlimited";

            return m_TargetFrameRate + " fps";
        }

        void Play()
        {
            var mgr = FindFirstObjectByType<PolySpatialEditorPlaybackManager>();
            if (mgr == null && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // save the path of every scene currently loaded, in order to restore them when we exit playmode
            m_ScenesForExitPlayback = EditorSceneManager.GetSceneManagerSetup();

            PolySpatialSettings.SessionRecordingMode = PolySpatialSettings.RecordingMode.Playback;

            if (mgr == null)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("PlaybackManager");
                mgr = go.AddComponent<PolySpatialEditorPlaybackManager>();
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            mgr.m_FilePath = PolySpatialSettings.SessionRecordingFilePath;

            EditorApplication.EnterPlaymode();
        }

        static string GenerateRecordingPath()
        {
           var recordingFile = Application.productName + "-" +
                               #if UNITY_EDITOR
                               EditorSceneManager.GetActiveScene().name + "-" +
                               #else
                               SceneManager.GetActiveScene().name + " - " +
                               #endif
                               DateTime.Now.ToString("yyyy-M-d-HHmmss") + ".qrec";
           return Path.Combine(k_RecordingBaseDir, recordingFile);
        }

        void Record()
        {
            if (!Directory.Exists(k_RecordingBaseDir))
                Directory.CreateDirectory(k_RecordingBaseDir);

            var recordingPath = GenerateRecordingPath();

            PolySpatialSettings.SessionRecordingMode = PolySpatialSettings.RecordingMode.Record;
            PolySpatialSettings.SessionRecordingFilePath = recordingPath;

            // set framerate before starting PlayMode
            if (PolySpatialSettings.SessionLimitFramerateWhenRecording && m_TargetFrameRate > 0)
                PolySpatialSettings.SessionRecordingFrameRate = m_TargetFrameRate;
            else
                PolySpatialSettings.SessionRecordingFrameRate = 0;

            EditorUtility.SetDirty(PolySpatialSettings.instance);
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);

            Debug.Log($"Recording to {recordingPath}");
            EditorApplication.EnterPlaymode();
        }

        void RefreshRecordingList()
        {
            m_AvailableRecordings = null;
            m_RecordingsList.list = AvailableRecordings();

            var recordingPath = PolySpatialSettings.SessionRecordingFilePath;
            if (string.IsNullOrEmpty(recordingPath))
            {
                m_RecordingsList.index = -1;
                return;
            }

            m_RecordingsList.index = Array.IndexOf(m_AvailableRecordings, recordingPath);
            if (m_RecordingsList.index == -1 )
                PolySpatialSettings.EraseSessionRecordingFilePath();
        }
    }
}
