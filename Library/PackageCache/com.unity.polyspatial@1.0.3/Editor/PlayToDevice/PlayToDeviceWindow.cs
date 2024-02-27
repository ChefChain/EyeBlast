using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Networking;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Connection = Unity.PolySpatial.PolySpatialUserSettings.Connection;
using ConnectionCandidate = Unity.PolySpatial.PolySpatialUserSettings.ConnectionCandidate;

namespace UnityEditor.PolySpatial.PlayToDevice
{
    class PlayToDeviceWindow : EditorWindow
    {
        const string k_InfoHelpBoxTextFormat = "Refer to <a href=\"{0}\">this post</a> or the <a href=\"{1}\">package documentation</a> for more info about the Play to Device for PolySpatial.";

        const string k_DiscussionsURL = "https://discussions.unity.com/t/play-to-device/309359";
        const string k_PlayToDeviceDocsURL = "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/PlayToDevice.html";

        const string k_MismatchedVersionHelpBoxTextFormat = "The device(s) name {0} have an app version that is not compatible with the current PolySpatial version " +
                                                     "(v{1}).";

        const string k_ConnectOnPlayToggleTooltip = "When enabled, your content will be synced to the Play to Device Host each time you enter Play mode. The Play To Device Host must be installed and running within the visionOS or your Vision Pro device.";
        const string k_ConnectionTimeoutFieldTooltip = "How long (in seconds) to try connecting to a remote host before timing out.";

        const string k_DirectConnectionName = "<Direct Connection>";
        const string k_InvalidNameHelpBoxText = "Invalid Name";
        const string k_InvalidIPHelpBoxText = "Invalid IP Address";
        const string k_DuplicateConnectionHelpBoxText = "Device IP address and port conflicts with already available connection.";
        const string k_InvalidPortHelpBoxText = "Invalid Port";

        const string k_PlayToDeviceWindowTitle = "Play To Device";
        const string k_PlayToDeviceWindowMenuPath = "Window/PolySpatial/" + k_PlayToDeviceWindowTitle;
        const string k_PlayToDeviceWindowIconPath = "Packages/com.unity.polySpatial/Assets/Textures/Icons/ARVR@4x.png";
        const string k_PlayToDeviceWindowTreeAssetPath = "Packages/com.unity.polyspatial/Editor/PlayToDevice/PlayToDeviceWindow.uxml";
        const string m_ConnectionListEntryTreeAssetPath = "Packages/com.unity.polyspatial/Editor/PlayToDevice/ConnectionListEntry.uxml";

        const string K_InfoFoldoutName = "InfoFoldout";
        const string k_InfoHelpBox = "InfoHelpBox";
        const string k_ConnectOnPlayDropdown = "ConnectOnPlayDropdown";
        const string k_ConnectionTimeoutField = "ConnectionTimeoutField";
        const string k_AvailableConnectionsFoldoutName = "AvailableConnectionsFoldout";
        const string k_ConnectionList = "ConnectionList";
        const string k_MismatchVersionHelpBox = "MismatchVersionHelpBox";

        const string K_AdvancedSettingsFoldoutName = "AdvancedSettingsFoldout";
        const string k_HostNameField = "HostNameField";
        const string k_InvalidHostNameHelpBox = "InvalidHostNameHelpBox";
        const string k_HostIPField = "HostIPField";
        const string k_InvalidIPHelpBox = "InvalidIPHelpBox";
        const string k_HostPortField = "HostPortField";
        const string k_InvalidPortHelpBox = "InvalidPortHelpBox";
        const string K_AddConnectionButton = "AddConnectionButton";
        const string K_DuplicateConnectionHelpBox = "DuplicateConnectionHelpBox";

        [MenuItem(k_PlayToDeviceWindowMenuPath)]
        static void LoadPlayToDeviceWindow()
        {
            var window = GetWindow<PlayToDeviceWindow>();
            window.titleContent = new GUIContent(k_PlayToDeviceWindowTitle, AssetDatabase.LoadAssetAtPath<Texture2D>(k_PlayToDeviceWindowIconPath));
        }

        static bool IsValidHostName(string name)
        {
            return !string.IsNullOrEmpty(name);
        }

        static bool IsValidIPAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            var octets = ipAddress.Split('.');
            if (octets.Length != 4)
                return false;

            return octets.All(o => byte.TryParse(o, out _));
        }

        static bool IsValidPort(int port)
        {
            return port > 1 && port < 65535;
        }

        static int CompareConnectionsByStatus(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.Status == b.Status)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return b.Status.CompareTo(a.Status);
        }

        static int CompareConnectionsByName(ConnectionCandidate a, ConnectionCandidate b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }

        static int CompareConnectionsByAppVersion(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.PlayToDeviceHostVersion == b.PlayToDeviceHostVersion)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return string.Compare(b.PlayToDeviceHostVersion, a.PlayToDeviceHostVersion, StringComparison.Ordinal);
        }

        static int CompareConnectionsByIP(ConnectionCandidate a, ConnectionCandidate b)
        {
            return string.Compare(a.IP, b.IP, StringComparison.Ordinal);
        }

        static int CompareConnectionsByPort(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.ServerPort == b.ServerPort)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return a.ServerPort.CompareTo(b.ServerPort);
        }

        static int CompareConnectionsByIsSelected(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.IsSelected == b.IsSelected)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return b.IsSelected.CompareTo(a.IsSelected);
        }

        static void SortConnections(List<ConnectionCandidate> connectionCandidates, ConnectionsSortOption sortOption)
        {
            switch (sortOption)
            {
                case ConnectionsSortOption.Status:
                    connectionCandidates.Sort(CompareConnectionsByStatus);
                    break;
                case ConnectionsSortOption.Name:
                    connectionCandidates.Sort(CompareConnectionsByName);
                    break;
                case ConnectionsSortOption.PlayToDeviceHostVersion:
                    connectionCandidates.Sort(CompareConnectionsByAppVersion);
                    break;
                case ConnectionsSortOption.IP:
                    connectionCandidates.Sort(CompareConnectionsByIP);
                    break;
                case ConnectionsSortOption.ServerPort:
                    connectionCandidates.Sort(CompareConnectionsByPort);
                    break;
                case ConnectionsSortOption.IsSelected:
                    connectionCandidates.Sort(CompareConnectionsByIsSelected);
                    break;
            }
        }

        [SerializeField]
        VisualTreeAsset m_PlayToDeviceWindowTreeAsset;

        [SerializeField]
        VisualTreeAsset m_ConnectionListEntryTreeAsset;

        [SerializeField]
        string m_HostName = k_DirectConnectionName;

        [SerializeField]
        string m_HostIPAddress = PolySpatialSettings.DefaultServerAddress;

        [SerializeField]
        int m_HostPort = PolySpatialSettings.DefaultServerPort;

        [NonSerialized]
        List<ConnectionCandidate> m_ConnectionCandidates = new List<ConnectionCandidate>();

        ListView m_ConnectionCandidatesListView;
        HelpBox m_MismatchedVersionsHelpBox;
        HelpBox m_DuplicateConnectionHelpBox;
        Button m_AddConnectionButton;

        SavedBool m_InfoFoldoutState;
        SavedBool m_AvailableConnectionsFoldoutState;
        SavedBool m_AdvancedSettingsFoldoutState;
        string m_MismatchedVersionNames;

        void OnEnable()
        {
            minSize = new Vector2(380, 400);
            m_InfoFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.InfoFoldoutState", false);
            m_AvailableConnectionsFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.AvailableConnectionsFoldoutState", true);
            m_AdvancedSettingsFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.AdvancedSettingsFoldoutState", false);

            if (m_PlayToDeviceWindowTreeAsset == null)
                m_PlayToDeviceWindowTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_PlayToDeviceWindowTreeAssetPath);

            if (m_ConnectionListEntryTreeAsset == null)
                m_ConnectionListEntryTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_ConnectionListEntryTreeAssetPath);

            Refresh();
            ConnectionDiscoveryManager.instance.OnConnectionsChanges += Refresh;
            if (!ConnectionDiscoveryManager.instance.IsListening)
                ConnectionDiscoveryManager.instance.StartListening();
        }

        void OnDisable()
        {
            ConnectionDiscoveryManager.instance.OnConnectionsChanges -= Refresh;
            if (ConnectionDiscoveryManager.instance.IsListening)
                ConnectionDiscoveryManager.instance.StopListening();
        }

        internal void Refresh()
        {
            m_ConnectionCandidates.Clear();
            m_ConnectionCandidates.AddRange(PolySpatialUserSettings.instance.ConnectionCandidates.Values);
            SortConnections(m_ConnectionCandidates, PolySpatialEditorUserSettings.instance.ConnectionsSortOption);

            // Reserves the first list element for the header
            m_ConnectionCandidates.Insert(0, null);

            // Reserves the second list element for the empty list message
            if (PolySpatialUserSettings.instance.ConnectionCandidates.Values.Count == 0)
                m_ConnectionCandidates.Add(null);

            if (rootVisualElement.childCount != 0)
                RefreshViews();
        }

        void RefreshViews()
        {
            m_ConnectionCandidatesListView.Rebuild();
            m_AddConnectionButton.SetEnabled(IsValidHostName(m_HostName)
                                             && IsValidIPAddress(m_HostIPAddress)
                                             && IsValidPort(m_HostPort)
                                             && !PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(new Connection(m_HostIPAddress, m_HostPort)));

            m_MismatchedVersionNames = GetMisMatchedConnectionNames();
            if (string.IsNullOrEmpty(m_MismatchedVersionNames))
            {
                m_MismatchedVersionsHelpBox.style.display = DisplayStyle.None;
            }
            else
            {
                m_MismatchedVersionsHelpBox.text = string.Format(k_MismatchedVersionHelpBoxTextFormat, m_MismatchedVersionNames,
                    PolySpatialSettings.instance.PackageVersion);
                m_MismatchedVersionsHelpBox.style.display = DisplayStyle.Flex;
            }

            m_DuplicateConnectionHelpBox.style.display = m_ConnectionCandidates.Any(
                c => c != null && c.IP == m_HostIPAddress && c.ServerPort == m_HostPort)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        string GetMisMatchedConnectionNames()
        {
            return string.Join(", ",
                m_ConnectionCandidates
                    .Where(c =>
                        c != null
                        && !string.IsNullOrEmpty(c.PlayToDeviceHostVersion)
                        && c.PlayToDeviceHostVersion != PolySpatialSettings.instance.PackageVersion)
                    .Select(c => $"\'{c.Name}\'")
                    .Distinct());
        }

        void CreateGUI()
        {
            VisualElement uxmlElements = m_PlayToDeviceWindowTreeAsset.Instantiate();

            uxmlElements.Q<HelpBox>(k_InfoHelpBox).text = string.Format(k_InfoHelpBoxTextFormat, k_DiscussionsURL, k_PlayToDeviceDocsURL);

            var infoFoldout = uxmlElements.Q<Foldout>(K_InfoFoldoutName);
            infoFoldout.value = m_InfoFoldoutState.Value;
            infoFoldout.RegisterValueChangedCallback(evt => m_InfoFoldoutState.Value = evt.newValue);

            var connectOnPlayToggle = uxmlElements.Q<DropdownField>(k_ConnectOnPlayDropdown);
            connectOnPlayToggle.index = PolySpatialUserSettings.instance.ConnectToPlayToDevice ? 1 : 0;
            connectOnPlayToggle.tooltip = k_ConnectOnPlayToggleTooltip;
            connectOnPlayToggle.RegisterValueChangedCallback(evt =>
            {
                var isEnabled = evt.newValue == PlayToDeviceConfiguration.Enabled.ToString();
                PolySpatialUserSettings.instance.ConnectToPlayToDevice = isEnabled;
            });

            var availableConnectionsFoldout = uxmlElements.Q<Foldout>(k_AvailableConnectionsFoldoutName);
            availableConnectionsFoldout.value = m_AvailableConnectionsFoldoutState.Value;
            availableConnectionsFoldout.RegisterValueChangedCallback(evt => m_AvailableConnectionsFoldoutState.Value = evt.newValue);

            m_ConnectionCandidatesListView = uxmlElements.Q<ListView>(k_ConnectionList);
            m_ConnectionCandidatesListView.makeItem = () =>
            {
                var newListEntry = m_ConnectionListEntryTreeAsset.Instantiate();
                var newListEntryController= new ConnectionListEntryController();
                newListEntry.userData = newListEntryController;
                newListEntryController.SetVisualElement(newListEntry);
                return newListEntry;
            };
            m_ConnectionCandidatesListView.bindItem = (item, index) =>
            {
                (item.userData as ConnectionListEntryController)?.BindData(this, m_ConnectionCandidatesListView, index);
            };
            m_ConnectionCandidatesListView.itemsSource = m_ConnectionCandidates;

            m_MismatchedVersionsHelpBox = uxmlElements.Q<HelpBox>(k_MismatchVersionHelpBox);

            var advancedSettingsFoldout = uxmlElements.Q<Foldout>(K_AdvancedSettingsFoldoutName);
            advancedSettingsFoldout.value = m_AdvancedSettingsFoldoutState.Value;
            advancedSettingsFoldout.RegisterValueChangedCallback(evt => m_AdvancedSettingsFoldoutState.Value = evt.newValue);

            var connectionTimeoutField = uxmlElements.Q<UnsignedIntegerField>(k_ConnectionTimeoutField);
            connectionTimeoutField.value = PolySpatialUserSettings.instance.ConnectionTimeout;
            connectionTimeoutField.tooltip = k_ConnectionTimeoutFieldTooltip;
            connectionTimeoutField.RegisterValueChangedCallback(evt => PolySpatialUserSettings.instance.ConnectionTimeout = evt.newValue);

            var invalidHostNameHelpBox = uxmlElements.Q<HelpBox>(k_InvalidHostNameHelpBox);
            invalidHostNameHelpBox.text = k_InvalidNameHelpBoxText;
            invalidHostNameHelpBox.style.display = IsValidHostName(m_HostName) ? DisplayStyle.None : DisplayStyle.Flex;

            var hostNameField = uxmlElements.Q<TextField>(k_HostNameField);
            hostNameField.value = m_HostName;
            hostNameField.RegisterValueChangedCallback(evt =>
            {
                m_HostName = evt.newValue;
                invalidHostNameHelpBox.style.display = IsValidHostName(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
            });

            var invalidIPHelpBox = uxmlElements.Q<HelpBox>(k_InvalidIPHelpBox);
            invalidIPHelpBox.text = k_InvalidIPHelpBoxText;
            invalidIPHelpBox.style.display = IsValidIPAddress(m_HostIPAddress) ? DisplayStyle.None : DisplayStyle.Flex;

            m_DuplicateConnectionHelpBox = uxmlElements.Q<HelpBox>(K_DuplicateConnectionHelpBox);
            m_DuplicateConnectionHelpBox.text = k_DuplicateConnectionHelpBoxText;

            var hostIPField = uxmlElements.Q<TextField>(k_HostIPField);
            hostIPField.value = m_HostIPAddress;
            hostIPField.RegisterValueChangedCallback(evt =>
            {
                m_HostIPAddress = evt.newValue;
                invalidIPHelpBox.style.display = IsValidIPAddress(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
                m_DuplicateConnectionHelpBox.style.display = m_ConnectionCandidates.Any(c =>
                    c != null && c.IP == m_HostIPAddress && c.ServerPort == m_HostPort)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });

            var invalidPortHelpBox = uxmlElements.Q<HelpBox>(k_InvalidPortHelpBox);
            invalidPortHelpBox.text = k_InvalidPortHelpBoxText;
            invalidPortHelpBox.style.display = IsValidPort(m_HostPort) ? DisplayStyle.None : DisplayStyle.Flex;

            var hostPortField = uxmlElements.Q<IntegerField>(k_HostPortField);
            hostPortField.value = m_HostPort;
            hostPortField.RegisterValueChangedCallback(evt =>
            {
                m_HostPort = evt.newValue;
                invalidPortHelpBox.style.display = IsValidPort(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
            });

            m_AddConnectionButton = uxmlElements.Q<Button>(K_AddConnectionButton);
            hostNameField.RegisterValueChangedCallback(evt =>
            {
                m_AddConnectionButton.SetEnabled(IsValidHostName(evt.newValue)
                                                 && IsValidIPAddress(m_HostIPAddress)
                                                 && IsValidPort(m_HostPort)
                                                 && !PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(new PolySpatialUserSettings.Connection(m_HostIPAddress, m_HostPort)));
            });
            hostIPField.RegisterValueChangedCallback(evt =>
            {
                m_AddConnectionButton.SetEnabled(IsValidHostName(m_HostName)
                                                 && IsValidIPAddress(evt.newValue)
                                                 && IsValidPort(m_HostPort)
                                                 && !PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(new PolySpatialUserSettings.Connection(evt.newValue, m_HostPort)));
            });
            hostPortField.RegisterValueChangedCallback(evt =>
            {
                m_AddConnectionButton.SetEnabled(IsValidHostName(m_HostName)
                                                 && IsValidIPAddress(m_HostIPAddress)
                                                 && IsValidPort(evt.newValue)
                                                 && !PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(new PolySpatialUserSettings.Connection(m_HostIPAddress, evt.newValue)));
            });
            m_AddConnectionButton.clicked += () =>
            {
                var newConnection = new Connection(m_HostIPAddress, m_HostPort);
                if (PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(newConnection))
                    return;

                var newConnectionCandidate = new ConnectionCandidate()
                {
                    IP = m_HostIPAddress,
                    Name = m_HostName,
                    ServerPort = m_HostPort,
                    Status = ConnectionDiscoveryStatus.Lost,
                    PlayToDeviceHostVersion = string.Empty,
                    LastContact = DateTime.Now
                };

                PolySpatialUserSettings.instance.ConnectionCandidates.Add(newConnection, newConnectionCandidate);
                Refresh();
            };

            RefreshViews();
            rootVisualElement.Add(uxmlElements);
        }
    }
}
