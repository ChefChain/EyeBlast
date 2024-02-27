using System;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Protocol for handling data providers in visionOS XR plugin
    /// </summary>
    interface IVisionOSProvider
    {
        /// <summary>
        /// The <see cref="AR_Authorization_Type"/> required by this data provider.
        /// </summary>
        AR_Authorization_Type RequiredAuthorizationType { get; }

        /// <summary>
        /// Whether this data provider is supported by the system.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Whether this data provider should be active
        /// </summary>
        bool ShouldBeActive { get; }

        /// <summary>
        /// The current <see cref="IntPtr"/> for the native data provider.
        /// </summary>
        IntPtr CurrentProvider { get; }

        /// <summary>
        /// Create a native data provider with the given <see cref="Feature"/> configuration.
        /// </summary>
        /// <param name="features"><see cref="Feature"/> flags requested by the app.</param>
        /// <param name="provider">An <see cref="IntPtr"/> provided by the ARKit API for this data provider, if one was created. <see cref="IntPtr.Zero"/> otherwise.</param>
        /// <returns><see langword="true"/> if the native provider was created successfully. Otherwise, <see langword="false"/>.</returns>
        bool TryCreateNativeProvider(Feature features, out IntPtr provider);
    }
}
