using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Types that implement <see cref="IVisionOSProvider"/> should register themselves for a unique <see cref="Feature"/>.
    /// This mapping is used by <see cref="VisionOSSessionSubsystem"/> to create native data providers for requested features.
    /// </summary>
    static class VisionOSProviderRegistration
    {
        static readonly Dictionary<Feature, IVisionOSProvider> k_DataProviders = new();

        /// <summary>
        /// Register a <see cref="IVisionOSProvider"/> instance that should be used to satisfy a given <see cref="Feature"/>.
        /// Only one provider may be registered for a given feature. The first provider registered will be used. Subsequent
        /// attempts to register a provider will have no effect, and an error will be logged.
        /// </summary>
        /// <param name="feature">The <see cref="Feature"/> that will be satisfied.</param>
        /// <param name="provider">The <see cref="IVisionOSProvider"/> which can satisfy the given <see cref="Feature"/>.</param>
        public static void RegisterProvider(Feature feature, IVisionOSProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException($"{nameof(provider)}");

            if (!k_DataProviders.TryAdd(feature, provider))
                Debug.LogWarning($"Cannot register AR data provider of type {provider.GetType().Name} for {feature}; a provider has already been registered for this feature.");
        }

        /// <summary>
        /// Unregister a <see cref="IVisionOSProvider"/> instance that should no longer be used to satisfy a given <see cref="Feature"/>.
        /// The provider instance must match the one that is registered. If the provider instance does not match the instance
        /// that was registered, no action is taken, and an error is logged.
        /// </summary>
        /// <param name="feature">The <see cref="Feature"/> to which the provider is currently registered.</param>
        /// <param name="provider">The <see cref="IVisionOSProvider"/> which will be unregistered.</param>
        public static void UnregisterProvider(Feature feature, IVisionOSProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException($"{nameof(provider)}");

            if (!k_DataProviders.TryGetValue(feature, out var existingProvider))
            {
                Debug.LogWarning($"Cannot unregister AR data provider of type {provider.GetType().Name} for {feature}; no existing provider is registered.");
                return;
            }

            if (provider != existingProvider)
            {
                Debug.LogWarning($"Cannot unregister AR data provider of type {provider.GetType().Name} for {feature}; the provided instance is not registered for this feature.");
                return;
            }

            k_DataProviders.Remove(feature);
        }

        /// <summary>
        /// Get an <see cref="IEnumerable{T}"/> which can be used to enumerate the currently registered providers.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> which can be used to enumerate the providers.</returns>
        public static IEnumerable<KeyValuePair<Feature, IVisionOSProvider>> EnumerateProviders()
        {
            return k_DataProviders.AsEnumerable();
        }
    }
}
