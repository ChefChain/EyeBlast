using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Host Command handler, this should recieving ARPlane data from a connected host,
    /// as well Hand data, mesh data, etc. and interfacing with the PolySpatial XR Plug-in to therefore provide ARFoundation with this data.
    /// </summary>
    internal class XRHostCommandHandler : IPolySpatialHostCommandHandler, IDisposable
    {
        public void Initialize()
        {
            PolySpatialCore.HostMulticastHandler?.AddHandler(this);
        }

        public void Dispose()
        {
            PolySpatialCore.HostMulticastHandler?.RemoveHandler(this);
        }

        public unsafe void HandleHostCommand(PolySpatialHostCommand cmd, int argCount, void** argValues, int* argSizes)
        {
        }
    }
}
