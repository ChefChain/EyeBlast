namespace Unity.PolySpatial
{
    /// <summary>
    /// Status for a auto discovery connection
    /// </summary>
    enum ConnectionDiscoveryStatus
    {
        Lost,       // Have not received a ping from host for a time period
        Ready,      // Host is open for connection
        Connected,  // Host is already connected
    }
}
