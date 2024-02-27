using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Connection = Unity.PolySpatial.PolySpatialUserSettings.Connection;
using ConnectionCandidate = Unity.PolySpatial.PolySpatialUserSettings.ConnectionCandidate;

namespace Unity.PolySpatial.Networking
{
    class ConnectionDiscoveryManager : ScriptableSingleton<ConnectionDiscoveryManager>
    {
        const float k_DisconnectionCheckInterval = 0.25f;

        /// <summary>
        /// The current list of connection candidates
        /// </summary>
        internal PolySpatialUserSettings.ConnectionDictionary ConnectionCandidates => PolySpatialUserSettings.instance.ConnectionCandidates;

        /// <summary>
        /// Callback when the list of connection candidates changes
        /// </summary>
        internal Action OnConnectionsChanges;

        UdpClient m_UdpClient;
        Thread m_ListenerThread;

        /// <summary>
        /// Whether there is a current thread listening
        /// </summary>
        internal bool IsListening => m_ListenerThread != null;

        DateTime m_NextDisconnectionCheck = DateTime.Now;
        bool m_InvokeChangedAction;
        readonly object m_InvokeChangedActionLock = new object();

        void Update()
        {
            lock (m_InvokeChangedActionLock)
            {
                if (m_InvokeChangedAction)
                {
                    OnConnectionsChanges?.Invoke();
                    m_InvokeChangedAction = false;
                }
            }

            // Early exit if not currently listening or not yet for the next disconnection check
            if (m_NextDisconnectionCheck > DateTime.Now || !IsListening)
            {
                return;
            }

            // Validate last check time for each candidate and mark it as lost if it has been too long
            foreach (var connection in ConnectionCandidates.Values)
            {
                if (!((DateTime.Now - connection.LastContact).TotalSeconds > PolySpatialSettings.instance.ConnectionDiscoveryTimeOutDuration)) continue;

                if (connection.Status != ConnectionDiscoveryStatus.Lost)
                {
                    lock (m_InvokeChangedActionLock)
                    {
                        m_InvokeChangedAction = true;
                    }
                }
                connection.Status = ConnectionDiscoveryStatus.Lost;
            }

            // Set the time for next disconnection check
            m_NextDisconnectionCheck = DateTime.Now.AddSeconds(k_DisconnectionCheckInterval);
        }

        /// <summary>
        /// Start a new thread to listen for udp pings from App host
        /// </summary>
        internal void StartListening()
        {
            // Make sure to kill any active listening thread if it exist
            if (m_ListenerThread != null)
            {
                StopListening();
            }

            // Unselect mismatched versions that are not direct connections
            foreach (var connection in ConnectionCandidates.Values)
            {
                if (connection.IsSelected && !string.IsNullOrEmpty(connection.PlayToDeviceHostVersion) && connection.PlayToDeviceHostVersion != PolySpatialSettings.instance.PackageVersion)
                    connection.IsSelected = false;
            }

            // Create the udp client
            try
            {
                m_UdpClient = new UdpClient(PolySpatialSettings.instance.ConnectionDiscoveryPort);
            }
            catch (Exception)
            {
                Debug.LogWarning("PolySpatial: Unable to listen to connection discovery. Make sure no other Unity Editor is actively listening.");
                return;
            }

            // Add a new background thread
            m_ListenerThread = new Thread(DiscoveryListener)
            {
                IsBackground = true
            };

            m_ListenerThread.Start();
            EditorApplication.update += Update;
        }

        /// <summary>
        /// Stop active thread listening for udp pings
        /// </summary>
        internal void StopListening()
        {
            if (m_ListenerThread == null)
            {
                Debug.LogWarning($"PolySpatial: Trying to stop when not listening to connection discovery. Make sure no other Unity Editor is actively listening.");
                return;
            }

            EditorApplication.update -= Update;
            m_UdpClient.Close();
            m_ListenerThread.Abort();
            m_ListenerThread = null;
        }

        /// <summary>
        /// Method called by the background thread to listen for udp pings
        /// </summary>
        void DiscoveryListener()
        {
            while (true)
            {
                var hostIP = new IPEndPoint(IPAddress.Any, PolySpatialSettings.instance.ConnectionDiscoveryPort);

                ConnectionDiscoveryData responseData;

                try
                {
                    var receivedResults = m_UdpClient.Receive(ref hostIP);
                    responseData = ConnectionDiscoveryData.FromByteArray(receivedResults);
                }
#if POLYSPATIAL_INTERNAL
                catch (Exception e)
                {
                    Debug.LogWarning($"Content Discovery: Unable to parse UDP data {e}");
                    continue;
                }
#else
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    continue;
                }
#endif

                // Parse out any port info, and just use the ip address
                var ip = hostIP.Address.ToString().Split(":")[0];
                var connection = new Connection(ip, responseData.ServerPort);

                // Check if connection already exists
                if (!ConnectionCandidates.ContainsKey(connection))
                {
                    ConnectionCandidates[connection] = new ConnectionCandidate
                    {
                        IP = hostIP.Address.ToString(),
                        Name = responseData.Hostname,
                        ServerPort = responseData.ServerPort,
                        PlayToDeviceHostVersion = responseData.PlayToDeviceHostVersion
                    };
                    lock (m_InvokeChangedActionLock)
                    {
                        m_InvokeChangedAction = true;
                    }
                }

                // Invoke connection change if the data is different for a connection candidate
                if (ConnectionCandidates[connection].Status != responseData.Status ||
                    ConnectionCandidates[connection].PlayToDeviceHostVersion != responseData.PlayToDeviceHostVersion)
                {
                    ConnectionCandidates[connection].Status = responseData.Status;
                    ConnectionCandidates[connection].PlayToDeviceHostVersion = responseData.PlayToDeviceHostVersion;
                    lock (m_InvokeChangedActionLock)
                    {
                        m_InvokeChangedAction = true;
                    }
                }

                // Make sure to save the last update time
                ConnectionCandidates[connection].LastContact = DateTime.Now;
            }
        }
    }
}
