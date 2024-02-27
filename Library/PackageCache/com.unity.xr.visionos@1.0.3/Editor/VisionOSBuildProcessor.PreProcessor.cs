#if UNITY_VISIONOS || (UNITY_VISIONOS_MAC_STUB && UNITY_STANDALONE_OSX)
using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#endif

namespace UnityEditor.XR.VisionOS
{
    static partial class VisionOSBuildProcessor
    {
#if UNITY_VISIONOS || (UNITY_VISIONOS_MAC_STUB && UNITY_STANDALONE_OSX)
        class Preprocessor : IPreprocessBuildWithReport
        {
            const string k_PreCompiledLibraryName = "libUnityVisionOS.a";
            static readonly string[] k_SourcePluginNames =
            {
                "UnityVisionOS.m",
                "VisionOSAppController.mm",
                // PolySpatial.visionOS package keeps these in Lib~ folder, so no need to distinguish between theirs and ours
                "UnityMain.swift",
                "UnityLibrary.swift"
            };

            public int callbackOrder => 0;

            void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
            {
                DisableSplashScreenIfEnabled();
                SetRuntimePluginCopyDelegate();
                RestoreARMWorkaround(report.summary.outputPath);

                if (!IsLoaderEnabled())
                    return;

                var settings = VisionOSSettings.currentSettings;
                var appMode = settings.appMode;
                if (appMode == VisionOSSettings.AppMode.Windowed)
                {
                    Debug.LogWarning("The Apple visionOS XR loader is not supported when building a visionOS Windowed application. It will be disabled for this " +
                        "build and re-enabled afterward. You may need to manually re-enable the loader in XR Plugin Management settings if this build fails.");

                    s_LoaderWasEnabled = true;
                    DisableLoader();
                    return;
                }

                if (settings.appMode == VisionOSSettings.AppMode.MR)
                {
#if !UNITY_HAS_POLYSPATIAL_VISIONOS
                    throw new BuildFailedException("Mixed Reality app mode requires the PolySpatial visionOS support package");
#else
                    // TODO: Figure out how to report this warning only if Unbounded is set as the default
                    //if (settings.initialVolumeCameraConfiguration?.Mode != VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)
                    Debug.Log("Notice: an Unbounded volume configuration is required for ARKit features when building for Mixed Reality");
#endif
                }
            }

            static void DisableSplashScreenIfEnabled()
            {
                s_SplashScreenWasEnabled = PlayerSettings.SplashScreen.show;
                if (!s_SplashScreenWasEnabled)
                    return;

                Debug.LogWarning("The Unity splash screen is not supported on visionOS. It will be disabled for this build and re-enabled afterward. " +
                    "You may need to manually re-enable the splash screen in Player Settings if this build fails.");

                PlayerSettings.SplashScreen.show = false;
            }

            static void SetRuntimePluginCopyDelegate()
            {
                var allPlugins = PluginImporter.GetAllImporters();
                foreach (var plugin in allPlugins)
                {
                    if (!plugin.isNativePlugin)
                        continue;

                    // Process pre-compiled library separately. Exactly one version should always be included in the build
                    // regardless of whether the loader is enabled. Otherwise, builds will fail in the linker stage
                    if (plugin.assetPath.Contains(k_PreCompiledLibraryName))
                    {
                        plugin.SetIncludeInBuildDelegate(ShouldIncludePreCompiledLibraryInBuild);
                        continue;
                    }

                    foreach (var pluginName in k_SourcePluginNames)
                    {
                        if (plugin.assetPath.Contains(pluginName))
                        {
                            plugin.SetIncludeInBuildDelegate(ShouldIncludeSourcePluginsInBuild);
                            break;
                        }
                    }
                }
            }

            static bool ShouldIncludeSourcePluginsInBuild(string path)
            {
                // Including plugins will cause errors because post process is different if loader is disabled
                if (!IsLoaderEnabled())
                    return false;

                // Always include `UnityVisionOS.m` file if the loader is enabled to provide export methods to XR library
                if (path.Contains("UnityVisionOS.m"))
                    return true;

                // Also include .swift files and UnityAppController.m for VR builds
                return VisionOSSettings.currentSettings.appMode == VisionOSSettings.AppMode.VR;
            }

            static bool ShouldIncludePreCompiledLibraryInBuild(string path)
            {
                // Exclude libraries that don't match the target SDK
                if (PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Device)
                {
                    if (path.Contains("Simulator"))
                        return false;
                }
                else
                {
                    if (path.Contains("Device"))
                        return false;
                }

                return true;
            }

            static void RestoreARMWorkaround(string outputPath)
            {
                // For append builds, we need to restore the original command line so that the Unity
                // build process doesn't see it as missing and add a duplicate to replace it.
                var xcodeProjectPath = GetXcodeProjectPath(outputPath);
                if (!File.Exists(xcodeProjectPath))
                    return;

                var projContents = File.ReadAllText(xcodeProjectPath);

                projContents = projContents.Replace(k_ARMWorkaroundReplacement, k_ARMWorkaroundOriginal);
                projContents = projContents.Replace(k_ARMWorkaroundReplacementAlt, k_ARMWorkaroundOriginalAlt);

                File.WriteAllText(xcodeProjectPath, projContents);
            }
        }
#endif
    }
}
