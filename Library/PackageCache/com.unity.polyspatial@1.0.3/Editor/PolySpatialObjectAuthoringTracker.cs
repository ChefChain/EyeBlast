using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.PolySpatial.Internals
{
    static class PolySpatialObjectAuthoringTracker
    {
        static bool s_IsTracking;
        static readonly List<UnityObject> s_ChangedList = new();
        static readonly List<int> s_DestroyedList = new();
        static readonly Dictionary<Type, Action<List<UnityObject>, List<int>>> s_TypeEventMap = new();

        static readonly ObjectDispatcherProxy s_Dispatcher = new(data =>
        {
            s_ChangedList.AddRange(data.changed);

            // Passing NativeArray to List.AddRange triggers code path that makes GC
            for (var i = 0; i < data.destroyedID.Length; ++i)
                s_DestroyedList.Add(data.destroyedID[i]);
        });

        internal static bool IsTracking(Type type) => s_TypeEventMap.ContainsKey(type);

        internal static void AddListener(Type type, Action<List<UnityObject>, List<int>> listener)
        {
            if (!s_TypeEventMap.TryGetValue(type, out var instanceChangedEvent) && s_IsTracking)
                s_Dispatcher.EnableTypeTracking(ObjectDispatcherProxy.TypeTrackingFlags.SceneObjects, type);

            instanceChangedEvent += listener;
            s_TypeEventMap[type] = instanceChangedEvent;
        }

        internal static void RemoveListener(Type type, Action<List<UnityObject>, List<int>> listener)
        {
            if (!s_TypeEventMap.TryGetValue(type, out var instanceChangedEvent))
                return;

            instanceChangedEvent -= listener;
            if (instanceChangedEvent == null)
            {
                s_TypeEventMap.Remove(type);
                if (s_IsTracking)
                    s_Dispatcher.DisableTypeTracking(type);
            }
            else
            {
                // Delegate is a value type and we need to assign it back
                s_TypeEventMap[type] = instanceChangedEvent;
            }
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                StartTracking();
        }

        static void StartTracking()
        {
            s_IsTracking = true;
            foreach (var type in s_TypeEventMap.Keys)
                s_Dispatcher.EnableTypeTracking(ObjectDispatcherProxy.TypeTrackingFlags.SceneObjects, type);

            EditorApplication.update += TrackChanges;
            Undo.postprocessModifications += OnPostProcessModifications;
        }

        static void StopTracking()
        {
            EditorApplication.update -= TrackChanges;
            Undo.postprocessModifications -= OnPostProcessModifications;
            foreach (var type in s_TypeEventMap.Keys)
                s_Dispatcher.DisableTypeTracking(type);

            s_IsTracking = false;
        }

        static UndoPropertyModification[] OnPostProcessModifications(UndoPropertyModification[] modifications)
        {
            TrackChanges();
            return modifications;
        }

        static void OnPlayModeChanged(PlayModeStateChange newState)
        {
            switch (newState)
            {
                case PlayModeStateChange.EnteredEditMode:
                    StartTracking();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    StopTracking();
                    break;
            }
        }

        static void TrackChanges()
        {
            foreach (var pair in s_TypeEventMap)
            {
                ClearLists();
                GetObjectChanges(pair.Key);
                if (s_ChangedList.Count != 0 || s_DestroyedList.Count != 0)
                    pair.Value?.Invoke(s_ChangedList, s_DestroyedList);
            }
        }

        static void ClearLists()
        {
            s_ChangedList.Clear();
            s_DestroyedList.Clear();
        }

        static void GetObjectChanges(Type type)
        {
            s_Dispatcher.DispatchTypeChangesAndClear(type);
        }
    }
}
