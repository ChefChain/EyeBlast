#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Class that holds the PolySpatial user settings values per project.
    /// This class is serialized in the project UserSettings folder.
    /// </summary>
    [FilePath("UserSettings/PolySpatialUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class PolySpatialUserSettings : ScriptableSingleton<PolySpatialUserSettings>
    {
        /// <summary>
        /// The IP address and port of a connection.
        /// </summary>
        [Serializable]
        internal struct Connection
        {
            public static bool operator==(Connection lhs, Connection rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator!=(Connection lhs, Connection rhs)
            {
                return !(lhs == rhs);
            }

            [SerializeField]
            string m_IP;

            /// <summary> IP address of the candidate </summary>
            internal string IP => m_IP;

            [SerializeField]
            int m_ServerPort;

            /// <summary> App host server port to connect to </summary>
            internal int ServerPort => m_ServerPort;

            internal Connection(string ip, int serverPort)
            {
                m_IP = ip;
                m_ServerPort = serverPort;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return m_IP.GetHashCode() ^ m_ServerPort.GetHashCode();
            }

            /// <inheritdoc/>
            public override bool Equals(object other)
            {
                if (!(other is Connection))
                    return false;

                return Equals((Connection)other);
            }

            /// <summary>
            /// Returns <see langword="true"/> if the given connection is exactly equal to this connection.
            /// </summary>
            /// <param name="other">The other connection to compare.</param>
            /// <returns>Returns <see langword="true"/> if the given connection is exactly equal to this connection. Otherwise returns <see langword="false"/>.</returns>
            public bool Equals(Connection other)
            {
                return IP == other.IP && ServerPort == other.ServerPort;
            }
        }

        /// <summary>
        /// Connection candidate data
        /// </summary>
        [Serializable]
        internal class ConnectionCandidate
        {
            /// <summary> IP address of the candidate </summary>
            public string IP;
            /// <summary> Hostname of the candidate if found, IP address otherwise</summary>
            public string Name;
            /// <summary> App host server port to connect to </summary>
            public int ServerPort;
            /// <summary> Current status of the App host </summary>
            public ConnectionDiscoveryStatus Status;
            /// <summary> The PolySpatial package version of the play to device App host </summary>
            public string PlayToDeviceHostVersion;
            /// <summary> Whether a candidate has been selected for connection </summary>
            public bool IsSelected;
            /// <summary> Last time a ping was received from the App host </summary>
            public DateTime LastContact;
        }

        /// <summary>
        /// A serializable dictionary of connections indexed by IP address and port.
        /// </summary>
        [Serializable]
        internal class ConnectionDictionary : Dictionary<Connection, ConnectionCandidate>, ISerializationCallbackReceiver
        {
            [SerializeField]
            List<Connection> m_Connections = new List<Connection>();

            [SerializeField]
            List<ConnectionCandidate> m_ConnectionsInfo = new List<ConnectionCandidate>();

            void ISerializationCallbackReceiver.OnBeforeSerialize()
            {
                // Save the keys and values in separate list
                m_Connections.Clear();
                m_Connections.AddRange(Keys);

                m_ConnectionsInfo.Clear();
                m_ConnectionsInfo.AddRange(Values);
            }

            void ISerializationCallbackReceiver.OnAfterDeserialize()
            {
                // Deserialize the connection back into this dictionary
                Clear();
                for (var i = 0; i < m_Connections.Count; i++)
                    this[m_Connections[i]] = m_ConnectionsInfo[i];
            }
        }

        [SerializeField]
        bool m_ConnectToPlayToDevice;

        internal bool ConnectToPlayToDevice
        {
            get => m_ConnectToPlayToDevice;
            set
            {
                m_ConnectToPlayToDevice = value;
                Save(true);
            }
        }

        [SerializeField]
        uint m_ConnectionTimeout = PolySpatialSettings.DefaultConnectionTimeout;

        internal uint ConnectionTimeout
        {
            get => m_ConnectionTimeout;
            set
            {
                m_ConnectionTimeout = value;
                Save(true);
            }
        }

        [SerializeField]
        ConnectionDictionary m_ConnectionCandidates = new ConnectionDictionary();

        internal ConnectionDictionary ConnectionCandidates => m_ConnectionCandidates;

        void OnDisable()
        {
            // Ensure that external changes to connection candidate are saved
            Save(true);
        }
    }
}
#endif
