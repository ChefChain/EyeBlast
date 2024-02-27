using System;
using System.Collections.Generic;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.UIElements;
using Connection = Unity.PolySpatial.PolySpatialUserSettings.Connection;
using ConnectionCandidate = Unity.PolySpatial.PolySpatialUserSettings.ConnectionCandidate;

namespace UnityEditor.PolySpatial.PlayToDevice
{
    class ConnectionListEntryController
    {
        const string k_ReadyIconTooltip = "The PlayToDevice app is ready for connection";
        const string k_ConnectedIconTooltip = "The PlayToDevice app connected";
        const string k_LostIconTooltip = "The PlayToDevice app has lost connection";
        const string k_RemoveIconTooltip = "Remove this connection entry from the list";

        const string k_VersionFormat = "v{0}";

        class Content
        {
            internal readonly Texture2D ReadyIcon;
            internal readonly Texture2D ConnectedIcon;
            internal readonly Texture2D LostIcon;
            internal readonly Texture2D RemoveIcon;

            internal Content()
            {
                ReadyIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_completed_task" : "completed_task").image as Texture2D;
                ConnectedIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_linked" : "linked").image as Texture2D;
                LostIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_console.erroricon" : "console.erroricon").image as Texture2D;
                RemoveIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_toolbar minus" : "toolbar minus").image as Texture2D;
            }
        }

        static Lazy<Content> s_Content = new Lazy<Content>(() => new Content());

        static bool IsEmpty(List<ConnectionCandidate> connectionCandidates)
        {
            // The element at index 0 is always null and is reserved for the list header
            // When the list is empty, one more null entry is added (at index 1) for the "List is Empty" message
            return connectionCandidates.Count == 2 && connectionCandidates[1] == null;
        }

        VisualElement m_HeaderParentElement;
        Label m_HeaderStatus;
        Label m_HeaderNameLabel;
        Label m_HeaderIPLabel;
        Label m_HeaderVersionLabel;
        Label m_HeaderPortLabel;
        Label m_HeaderConnectLabel;

        VisualElement m_ConnectionParentElement;
        Label m_Status;
        Label m_NameLabel;
        Label m_IPLabel;
        Label m_VersionLabel;
        Label m_PortLabel;
        Toggle m_ConnectToggle;
        Label m_RemoveLabel;

        VisualElement m_EmptyMessageParentElement;

        internal void SetVisualElement(VisualElement visualElement)
        {
            m_HeaderParentElement = visualElement.Q<VisualElement>("HeaderParentElement");
            m_HeaderStatus = m_HeaderParentElement.Q<Label>("StatusLabel");
            m_HeaderNameLabel = m_HeaderParentElement.Q<Label>("NameLabel");
            m_HeaderIPLabel = m_HeaderParentElement.Q<Label>("IPLabel");
            m_HeaderVersionLabel = m_HeaderParentElement.Q<Label>("VersionLabel");
            m_HeaderPortLabel = m_HeaderParentElement.Q<Label>("PortLabel");
            m_HeaderConnectLabel = m_HeaderParentElement.Q<Label>("ConnectLabel");

            m_ConnectionParentElement = visualElement.Q<VisualElement>("ConnectionParentElement");
            m_Status = m_ConnectionParentElement.Q<Label>("StatusLabel");
            m_NameLabel = m_ConnectionParentElement.Q<Label>("NameLabel");
            m_IPLabel = m_ConnectionParentElement.Q<Label>("IPLabel");
            m_VersionLabel = m_ConnectionParentElement.Q<Label>("VersionLabel");
            m_PortLabel = m_ConnectionParentElement.Q<Label>("PortLabel");
            m_ConnectToggle = m_ConnectionParentElement.Q<Toggle>("ConnectToggle");
            m_RemoveLabel = m_ConnectionParentElement.Q<Label>("RemoveLabel");
            m_RemoveLabel.tooltip = k_RemoveIconTooltip;
            m_RemoveLabel.style.backgroundImage = s_Content.Value.RemoveIcon;

            m_EmptyMessageParentElement = visualElement.Q<VisualElement>("EmptyMessageParentElement");
        }

        internal void BindData(PlayToDeviceWindow playToDeviceWindow, ListView listView, int index)
        {
            if (index == 0)
            {
                BindHeaderData(playToDeviceWindow);
            }
            else
            {
                var connectionCandidates = listView.itemsSource as List<ConnectionCandidate>;
                if (connectionCandidates == null)
                    return;

                if (IsEmpty(connectionCandidates))
                    BindEmptyMessageData();
                else
                    BindConnectionData(playToDeviceWindow, listView, connectionCandidates, index);
            }
        }

        void BindHeaderData(PlayToDeviceWindow playToDeviceWindow)
        {
            m_HeaderParentElement.style.display = DisplayStyle.Flex;
            m_EmptyMessageParentElement.style.display = DisplayStyle.None;
            m_ConnectionParentElement.style.display = DisplayStyle.None;

            var currentSortOption = PolySpatialEditorUserSettings.instance.ConnectionsSortOption;
            m_HeaderStatus.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.Status
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.Status;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderStatus.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.Status
                ? FontStyle.Bold
                : FontStyle.Normal;

            m_HeaderNameLabel.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.Name
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.Name;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderNameLabel.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.Name
                ? FontStyle.Bold
                : FontStyle.Normal;

            m_HeaderIPLabel.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.IP
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.IP;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderIPLabel.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.IP
                ? FontStyle.Bold
                : FontStyle.Normal;

            m_HeaderVersionLabel.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.PlayToDeviceHostVersion
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.PlayToDeviceHostVersion;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderVersionLabel.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.PlayToDeviceHostVersion
                ? FontStyle.Bold
                : FontStyle.Normal;

            m_HeaderPortLabel.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.ServerPort
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.ServerPort;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderPortLabel.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.ServerPort
                ? FontStyle.Bold
                : FontStyle.Normal;

            m_HeaderConnectLabel.RegisterCallback<MouseUpEvent>( _ =>
            {
                PolySpatialEditorUserSettings.instance.ConnectionsSortOption = currentSortOption == ConnectionsSortOption.IsSelected
                    ? ConnectionsSortOption.None
                    : ConnectionsSortOption.IsSelected;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_HeaderConnectLabel.style.unityFontStyleAndWeight = currentSortOption == ConnectionsSortOption.IsSelected
                ? FontStyle.Bold
                : FontStyle.Normal;
        }

        void BindEmptyMessageData()
        {
            m_HeaderParentElement.style.display = DisplayStyle.None;
            m_EmptyMessageParentElement.style.display = DisplayStyle.Flex;
            m_ConnectionParentElement.style.display = DisplayStyle.None;
        }

        void BindConnectionData(PlayToDeviceWindow playToDeviceWindow, ListView listView, List<ConnectionCandidate> connectionCandidates, int index)
        {
            m_HeaderParentElement.style.display = DisplayStyle.None;
            m_EmptyMessageParentElement.style.display = DisplayStyle.None;
            m_ConnectionParentElement.style.display = DisplayStyle.Flex;

            var connectionCandidate = connectionCandidates[index];
            switch (connectionCandidate.Status)
            {
                case ConnectionDiscoveryStatus.Ready:
                    m_Status.tooltip = k_ReadyIconTooltip;
                    m_Status.style.backgroundImage = s_Content.Value.ReadyIcon;
                    break;
                case ConnectionDiscoveryStatus.Connected:
                    m_Status.tooltip = k_ConnectedIconTooltip;
                    m_Status.style.backgroundImage = s_Content.Value.ConnectedIcon;
                    break;
                case ConnectionDiscoveryStatus.Lost:
                    m_Status.tooltip = k_LostIconTooltip;
                    m_Status.style.backgroundImage = s_Content.Value.LostIcon;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            m_NameLabel.text = connectionCandidate.Name;
            m_VersionLabel.text = string.IsNullOrEmpty(connectionCandidate.PlayToDeviceHostVersion)
                ? string.Empty
                : string.Format(k_VersionFormat, connectionCandidate.PlayToDeviceHostVersion);
            m_IPLabel.text = connectionCandidate.IP;
            m_PortLabel.text = connectionCandidate.ServerPort.ToString();
            m_ConnectToggle.value = connectionCandidate.IsSelected;
            m_ConnectToggle.RegisterValueChangedCallback(evt =>
            {
                connectionCandidate.IsSelected = evt.newValue;
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });
            m_RemoveLabel.RegisterCallback<PointerUpEvent>( _ =>
            {
                PolySpatialUserSettings.instance.ConnectionCandidates.Remove(new Connection(connectionCandidate.IP, connectionCandidate.ServerPort));
                EditorApplication.delayCall += playToDeviceWindow.Refresh;
            });

            SetEnabled(string.IsNullOrEmpty(connectionCandidate.PlayToDeviceHostVersion)
                       || PolySpatialSettings.instance.PackageVersion == connectionCandidate.PlayToDeviceHostVersion);
            if (m_RemoveLabel.enabledSelf && connectionCandidate.Status != ConnectionDiscoveryStatus.Lost)
                m_RemoveLabel.SetEnabled(false);
        }

        void SetEnabled(bool value)
        {
            m_NameLabel.SetEnabled(value);
            m_IPLabel.SetEnabled(value);
            m_VersionLabel.SetEnabled(value);
            m_PortLabel.SetEnabled(value);
            m_ConnectToggle.SetEnabled(value);
        }
    }
}
