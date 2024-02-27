using UnityEngine;

namespace UnityEditor.PolySpatial
{
    /// <summary>
    /// The sort options to display the connection candidates
    /// </summary>
    enum ConnectionsSortOption
    {
        None,
        Status,
        Name,
        PlayToDeviceHostVersion,
        IP,
        ServerPort,
        IsSelected
    }

    /// <summary>
    /// The state of the Play to Device connectio
    /// </summary>
    enum PlayToDeviceConfiguration
    {
        Disabled,
        Enabled
    }

    /// <summary>
    /// Class that holds the PolySpatial user settings values per project.
    /// This class is serialized in the project UserSettings folder.
    /// </summary>
    [FilePath("UserSettings/PolySpatialEditorUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class PolySpatialEditorUserSettings : ScriptableSingleton<PolySpatialEditorUserSettings>
    {
        [SerializeField]
        ConnectionsSortOption m_ConnectionsSortOption;

        internal ConnectionsSortOption ConnectionsSortOption
        {
            get => m_ConnectionsSortOption;
            set
            {
                m_ConnectionsSortOption = value;
                Save(true);
            }
        }
    }
}
