using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that draws the PolySpatial UI validation in the Inspector window.
    /// </summary>
    [InitializeOnLoad]
    static class InspectorValidationUI
    {
        const string k_ObjectMessage = "This Game Object has configurations that are not supported by the target platform:\n";
        const string k_FixObjectButtonLabel = "Fix This Object";
        const string k_FixButtonLabel = "Fix";
        const string k_EditButtonLabel = "Edit";
        const string k_OpenValidationButtonLabel = "Open Project Validation";
        const string k_ProjectValidationSettingsPath = "Project/XR Plug-in Management/Project Validation";
        const string k_FixComponentProgressBarTitle = "Fix Component Issue";

        static readonly Dictionary<int,string> k_ObjectMessages = new Dictionary<int, string>();

        class Styles
        {
            internal readonly GUIContent PreviousButtonContent;
            internal readonly GUIContent NextButtonContent;
            internal readonly GUIContent FixObjectButtonContent = new(k_FixObjectButtonLabel, "Fix all issues with automatic fixes available for this object.");
            internal readonly GUIContent DisabledFixObjectButtonContent = new(k_FixObjectButtonLabel, "There are no issues with automatic fixes available for this object.");

            internal Styles()
            {
                PreviousButtonContent = EditorGUIUtility.IconContent("tab_prev");
                NextButtonContent = EditorGUIUtility.IconContent("tab_next");
            }
        }

        static Styles s_Styles;
        static Styles styles => s_Styles ??= new Styles();

        class ValidationElement : IMGUIContainer
        {
            readonly Editor m_Editor;

            internal ValidationElement(Editor editor)
            {
                m_Editor = editor;
                onGUIHandler = OnGUIHandler;
            }

            void OnGUIHandler()
            {
                OnInspectorGUI(m_Editor);
            }
        }

        class InspectorValidationDecorator : IEditorElementDecoratorProxy
        {
            public VisualElement OnCreateFooter(Editor editor)
            {
                if (Application.isPlaying || GetSingleTargetComponent(editor) == null)
                    return null;

                return new ValidationElement(editor);
            }
        }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<BuildValidationRule> k_ComponentIssues = new();
        static readonly List<BuildValidationRule> k_ObjectIssues = new();
        static readonly List<BuildValidationRule> s_FixAllList = new();

        static InspectorValidationUI()
        {
            Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
            PolySpatialSceneValidator.OnValidateRules += OnRulesValidated;
            EditorElementBridge.AddDecorator(new InspectorValidationDecorator());
        }

        static void OnHeaderGUI(Editor editor)
        {
            if (Application.isPlaying || editor.targets.Length > 1)
                return;

            var instanceID = editor.target.GetInstanceID();
            if (!PolySpatialSceneValidator.IsValidGameObject(instanceID))
            {
                k_ObjectIssues.Clear();
                if (PolySpatialSceneValidator.TryGetObjectRules(instanceID, k_ObjectIssues))
                    ObjectRulesHeaderGUI(instanceID, k_ObjectIssues);

                ObjectFailureHeaderGUI(instanceID);
            }
        }

        static void OnRulesValidated()
        {
            k_ObjectMessages.Clear();
        }

        static void ObjectFailureHeaderGUI(int instanceID)
        {
            IssueNavigationHeaderGUI(instanceID);
            using (new EditorGUILayout.HorizontalScope())
            {
                SceneValidationUIUtils.DrawCapabilitiesSelectionDropdown();

                GUILayout.FlexibleSpace();

                k_ComponentIssues.Clear();
                PolySpatialSceneValidator.GetGameObjectComponentRulesFailures(instanceID, k_ComponentIssues);
                var hasAutoFixIssues = k_ComponentIssues.Any(issue => issue.FixItAutomatic) || k_ObjectIssues.Any(issue => issue.FixItAutomatic);

                using (new EditorGUI.DisabledScope(!hasAutoFixIssues))
                {
                    var buttonContent = hasAutoFixIssues ? styles.FixObjectButtonContent : styles.DisabledFixObjectButtonContent;
                    if (GUILayout.Button(buttonContent))
                        EditorApplication.delayCall += () => PolySpatialSceneValidator.AutoFixGameObject(instanceID);
                }
            }
        }

        static void ObjectRulesHeaderGUI(int instanceID, List<BuildValidationRule> objectRules)
        {
            if (!k_ObjectMessages.TryGetValue(instanceID, out var message))
            {
                message = k_ObjectMessage;
                foreach (var rule in objectRules)
                {
                    message += $"\n• {rule.Message}";
                }

                k_ObjectMessages.Add(instanceID,message);
            }

            PolySpatialEditorGUIUtils.DrawMessageBox(message, MessageType.Warning);
        }

        static void IssueNavigationHeaderGUI(int instanceID)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(styles.PreviousButtonContent, GUILayout.ExpandWidth(false)))
                    Selection.activeInstanceID = PolySpatialSceneValidator.GetPreviousFailureObjectID(instanceID);

                if (GUILayout.Button(k_OpenValidationButtonLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath);

                if (GUILayout.Button(styles.NextButtonContent, GUILayout.ExpandWidth(false)))
                    Selection.activeInstanceID = PolySpatialSceneValidator.GetNextFailureObjectID(instanceID);
            }
        }

        static void OnInspectorGUI(Editor editor)
        {
            if (Application.isPlaying || CapabilityProfileSelection.Selected.Count == 0)
                return;

            var component = GetSingleTargetComponent(editor);
            if (component == null)
                return;

            if (editor is not DefaultEditorWrapper)
                DrawInspectorTypeMessages(component);

            DrawInspectorIssues(editor);
        }

        static Component GetSingleTargetComponent(Editor editor)
        {
            if (editor.targets.Length > 1 ||
                editor.target is not Component component)
                return null;

            return component;
        }

        static void DrawInspectorTypeMessages(Component component)
        {
            if (!CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile))
                return;

            var messages = PolySpatialSceneValidator.GetTypeMessages(component.GetType());
            if (messages == null || messages.Length == 0)
                return;

            foreach (var message in messages)
            {
                EditorGUILayout.Space(2f);
                PolySpatialEditorGUIUtils.DrawMessageBox(message.Message, message.MessageType, message.Link.LinkTitle, message.Link.LinkUrl);
            }
        }

        internal static void DrawInspectorTypeMessages(Editor editor)
        {
            var component = GetSingleTargetComponent(editor);
            if (component == null)
                return;

            DrawInspectorTypeMessages(component);
        }

        static void DrawInspectorIssues(Editor editor)
        {
            k_ComponentIssues.Clear();
            PolySpatialSceneValidator.GetComponentRulesFailures(editor.target.GetInstanceID(), k_ComponentIssues);
            if (k_ComponentIssues.Count == 0)
                return;

            foreach (var issue in k_ComponentIssues)
            {
                EditorGUILayout.Space(2f);
                DrawInspectorIssue(issue);
            }
        }

        static void DrawInspectorIssue(BuildValidationRule issue)
        {
            var messageType = issue.Error ? MessageType.Error : MessageType.Warning;
            if (issue.FixIt == null)
            {
                PolySpatialEditorGUIUtils.DrawMessageBox(issue.Message, messageType, issue.HelpText, issue.HelpLink);
            }
            else
            {
                var fixButtonLabel = issue.FixItAutomatic ? k_FixButtonLabel : k_EditButtonLabel;
                if (PolySpatialEditorGUIUtils.DrawFixMeBox(issue.Message, messageType, fixButtonLabel, issue.FixItMessage, issue.HelpText, issue.HelpLink))
                {
                    s_FixAllList.Clear();
                    s_FixAllList.Add(issue);
                    BuildValidator.FixIssues(s_FixAllList, k_FixComponentProgressBarTitle);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
