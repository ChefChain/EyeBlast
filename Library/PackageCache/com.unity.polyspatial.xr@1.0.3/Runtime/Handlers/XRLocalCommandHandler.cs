using System;
using Unity.PolySpatial.Internals;
using UnityEditor;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Command handler running on the host, mainly for initializing AR/XR data when a client connects to _this_ host.
    /// Since some data can be generated on the platform such as ARPlane data, Hand data, etc. we need to send this
    /// to the client when they connect to the host.
    /// </summary>
    internal class XRLocalCommandHandler : IPolySpatialCommandHandler, IDisposable
    {
        internal XRLocalCommandHandler()
        {
        }

        public void Initialize()
        {
            //PolySpatialCore.MulticastHandler.AddHandler(this);
        }

        public void Dispose()
        {
            //PolySpatialCore.MulticastHandler.RemoveHandler(this);
        }

        public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
        {
        }
    }
}
