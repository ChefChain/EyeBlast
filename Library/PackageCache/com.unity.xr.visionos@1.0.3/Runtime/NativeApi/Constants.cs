namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Centralized location for tracking constant values for native API.
    /// </summary>
    static partial class NativeApi
    {
        internal static class Constants
        {
            /// <summary>
            /// Library name to be used for DllImport attributes
            /// </summary>
#if UNITY_VISIONOS && !UNITY_EDITOR
            public const string LibraryName = "__Internal";
#else
            public const string LibraryName = "libUnityVisionOSMock";
#endif
        }
    }
}
