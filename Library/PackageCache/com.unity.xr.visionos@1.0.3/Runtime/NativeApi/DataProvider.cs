using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Native API wrappers for data provider.
    /// Signatures should match data_provider.h.
    /// </summary>
    static partial class NativeApi
    {
        internal static class DataProvider
        {
            // TODO: Bring over missing summary comments
            // TODO: Clean up naming

            /// <summary>
            /// Creates an empty collection of data providers.
            /// </summary>
            /// <remarks>
            /// This type supports ARC. In non-ARC files, use `ar_retain()` and `ar_release()` to retain and release the object.
            /// </remarks>
            /// <returns>An instance of `ar_data_providers_t`.</returns>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_data_providers_create")]
            public static extern IntPtr ar_data_providers_create();

            /// <summary>
            /// Add a data provider to collection.
            /// </summary>
            /// <param name="data_providers">Collection to expand.</param>
            /// <param name="data_provider_to_add">Data provider to add.</param>
            [DllImport(Constants.LibraryName, EntryPoint = "ar_data_providers_add_data_provider")]
            public static extern void ar_data_providers_add_data_provider(IntPtr data_providers,
                IntPtr data_provider_to_add);

            // TODO: Implement remaining function signatures
        }
    }
}
