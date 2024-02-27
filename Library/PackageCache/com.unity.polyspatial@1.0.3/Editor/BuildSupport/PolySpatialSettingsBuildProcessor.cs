using Unity.PolySpatial;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PolySpatial.Utilities;

namespace UnityEditor.PolySpatial.Internals
{
    internal class PolySpatialSettingsBuildProcessor : IPostprocessBuildWithReport
    {
        // If this is too high (e.g. 900), tests seem to run before it gets updated?
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
#if POLYSPATIAL_INTERNAL
            var runtimeEnabled = PolySpatialSettings.RuntimeModeForceEnabled;
            if (runtimeEnabled)
            {
                var bootConfig = new BootConfigBuildUtility(report);
                bootConfig.SetValue("polyspatial", "1");
                bootConfig.Write();
            }
#endif
        }
    }
}
