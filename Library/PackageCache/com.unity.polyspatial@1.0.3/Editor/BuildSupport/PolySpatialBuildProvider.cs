using System;
using System.Linq;
using System.Reflection;
using Platforms.Unity;
using TMPro;
using Unity.PolySpatial;
using Unity.PolySpatial.Editor;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PolySpatial.Utilities;

#if HAS_SCRIPTABLE_BUILDPIPELINE
using UnityEditor.Build.Pipeline;
#endif

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// Performs custom build steps, such as ensuring that the default material will be present in standalone
    /// builds.
    /// </summary>
    internal class PolySpatialBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// The order in which this provider is called relative to other build providers.
        /// </summary>
        /// <seealso cref="IOrderedCallback.callbackOrder"/>
        public int callbackOrder => 0;

#if HAS_SCRIPTABLE_BUILDPIPELINE
        [InitializeOnLoadMethod]
        static void InitBuildCallbackLogger()
        {
            ContentPipeline.BuildCallbacks.PostDependencyCallback += (parameters, data) =>
            {
                if (PolySpatialBuildProvider.ShouldProcessBuild(parameters.Target))
                {
                    PlayerSettingsBridge.SetRequiresReadableAssets(true);
                }

                return ReturnCode.Success;
            };
            ContentPipeline.BuildCallbacks.PostWritingCallback += (parameters, data, arg3, arg4) =>
            {
                if (PolySpatialBuildProvider.ShouldProcessBuild(parameters.Target))
                {
                    PlayerSettingsBridge.SetRequiresReadableAssets(false);
                }

                return ReturnCode.Success;
            };
        }
#endif

        static bool ShouldProcessBuild(BuildTarget target)
        {
            // While caching is a lovely idea, it won't work if you don't trigger
            // a domain reload (thus clearing out the cache) in between changing settings.
            //
            // Ultimately this is going to get called so few times that it doesn't matter.

            var args = new object[] { target };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // consider only PolySpatial assemblies
                if (!asm.FullName.Contains("PolySpatial"))
                    continue;

                foreach (var type in asm.GetTypes().Where(t =>
                             !t.IsAbstract && t.IsClass && typeof(IPolySpatialEditorPlatform).IsAssignableFrom(t)))
                {
                    var method = type.GetMethod("ShouldEnablePolySpatialRuntimeForBuild", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                    {
                        Debug.LogError($"{type.FullName} is an IPolySpatialEditorPlatform but is missing ShouldEnablePolySpatialRuntimeForBuild()");
                        continue;
                    }

                    bool enable = (bool)method.Invoke(null, args);
                    if (enable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void MapAllFontAssets()
        {
            var assetGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");

            foreach (var assetGuid in assetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var tmpFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (tmpFontAsset != null)
                {
                    PolySpatialFontManager.Instance.UpdateFontAssociation(tmpFontAsset, PolySpatialPlatformTextUtility.FontForTmpFont(tmpFontAsset));
                }
            }
        }

        /// <summary>
        /// Called before a build is started.
        /// </summary>
        /// <param name="report">The build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!ShouldProcessBuild(report.summary.platform))
                return;

            PlayerSettingsBridge.SetRequiresReadableAssets(true);

            MapAllFontAssets();

            PolySpatialFontManager.WriteFontMapToAssets();
        }

        /// <summary>
        /// Called after a build is finished.
        /// </summary>
        /// <param name="report">The build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (!ShouldProcessBuild(report.summary.platform))
                return;

            PlayerSettingsBridge.SetRequiresReadableAssets(false);

            PolySpatialFontManager.RemoveFontMapFromAssets();

            WriteBootConfigSettings(report);
        }

        public void WriteBootConfigSettings(BuildReport report)
        {
            var bootConfig = new BootConfigBuildUtility(report);
            bootConfig.SetValue("polyspatial", "1");
            bootConfig.SetValue("polyspatial_logging", Logging.LoggingPrefsToString());
            bootConfig.Write();
        }
    }
}
