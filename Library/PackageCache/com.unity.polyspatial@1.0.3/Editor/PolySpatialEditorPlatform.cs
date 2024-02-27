using UnityEditor;

namespace Unity.PolySpatial.Editor
{
    internal class PolySpatialEditorPlatform : IPolySpatialEditorPlatform
    {
        public static bool ShouldEnablePolySpatialRuntime()
        {
            // this is the generic check, that allows this to be overridden in settings
            return PolySpatialSettings.RuntimeModeForceEnabled;
        }

        public static bool ShouldEnablePolySpatialRuntimeForBuild(BuildTarget target)
        {
            return PolySpatialSettings.RuntimeModeForceEnabled;
        }
    }
}
