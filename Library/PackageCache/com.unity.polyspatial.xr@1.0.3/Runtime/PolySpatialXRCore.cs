#if POLYSPATIAL_INTERNAL
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.PolySpatial.XR.Internals;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals.Subsystems;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// The PolySpatialXRCore subsystem is responsible for initializing the PolySpatialXRCore
    /// Add all the handlers for the commands that are supported by the PolySpatialXRCore, and
    /// add all the trackers necessary for tracking changes from XR components and events.
    /// </summary>
    internal class PolySpatialXRCore : PolySpatialSubsystemBase
    {
        const string k_SubsystemId = "PolySpatialXRCore_Subsystem";

        private List<IPolySpatialCommandHandler> m_CommandHandlers = new List<IPolySpatialCommandHandler>();
        private List<IPolySpatialHostCommandHandler> m_HostCommandHandlers = new List<IPolySpatialHostCommandHandler>();

        public PolySpatialXRCore() : base(k_SubsystemId)
        {
        }

        /// <summary>
        /// Initializes the host handlers depending on the current networking mode
        /// </summary>
        internal override void Initialize()
        {
            AddLocalCommandHandlers();

            if(PolySpatialCore.CurrentNetworkingMode == PolySpatialSettings.NetworkingMode.LocalAndClient)
            {
                AddHostCommandHandlers();
            }
        }

        /// <summary>
        /// Add the handlers on the Host
        /// </summary>
        void AddLocalCommandHandlers()
        {
            var xrhandler = new XRLocalCommandHandler();
            xrhandler.Initialize();
            m_CommandHandlers.Add(xrhandler);
        }

        /// <summary>
        /// Add the handlers that will handle commands coming from a host over the network
        /// </summary>
        void AddHostCommandHandlers()
        {
            var xrhosthandler = new XRHostCommandHandler();
            xrhosthandler.Initialize();
            m_HostCommandHandlers.Add(xrhosthandler);
        }

        /// <summary>
        /// Cleanup all the handlers
        /// </summary>
        public override void Dispose()
        {
            foreach (var handler in m_CommandHandlers)
            {
                (handler as IDisposable)?.Dispose();
            }

            foreach (var handler in m_HostCommandHandlers)
            {
                (handler as IDisposable)?.Dispose();
            }
        }
    }
}
#endif
