using System.Collections.Generic;
using Unity.PolySpatial;
using UnityEditor.PolySpatial.Internals.InternalBridge;
using UnityEditor.PolySpatial.Internals;
using Object = UnityEngine.Object;

namespace UnityEditor.PolySpatial
{
    class PolySpatialSettingsProvider : SettingsProvider
    {
        internal const string SettingsPath = "Project/PolySpatial";

        [SettingsProvider]
        static SettingsProvider CreatePolySpatialProjectSettingsProvider()
        {
            return new PolySpatialSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        /// <summary>
        /// Settings constructor called when we need a new settings instance.
        /// </summary>
        /// <param name="path">Path of the settings window within the settings UI.</param>
        /// <param name="scopes">The scopes within which the settings are valid.</param>
        /// <param name="keywords">Keywords for search.</param>
        PolySpatialSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        Editor m_PolySpatialSettingsEditor;

        /// <inheritdoc/>
        public override void OnDeactivate()
        {
            if (m_PolySpatialSettingsEditor != null)
            {
                Object.DestroyImmediate(m_PolySpatialSettingsEditor);
                m_PolySpatialSettingsEditor = null;
            }
        }

        /// <inheritdoc/>
        public override void OnGUI(string searchContext)
        {
            Editor.CreateCachedEditor(PolySpatialSettings.instance, null, ref m_PolySpatialSettingsEditor);
            using (EditorGUIBridge.CreateSettingsWindowGUIScope())
            {
                m_PolySpatialSettingsEditor.OnInspectorGUI();
            }

            PolySpatialLoggingUI.DrawLoggingSettings();
        }
    }
}
