using System;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.VisionOS;

namespace UnityEditor.XR.VisionOS
{
    static partial class VisionOSBuildProcessor
    {
        const string k_XcodeProjectFolder = "Unity-VisionOS.xcodeproj";
        const string k_XcodeProjectName = "project.pbxproj";
        const string k_VisionOSLoaderTypeName = "UnityEngine.XR.VisionOS.VisionOSLoader";

        static bool s_SplashScreenWasEnabled;
        static bool s_LoaderWasEnabled;

        static void GetXRManagerSettings(out BuildTargetGroup buildTargetGroup, out XRManagerSettings manager)
        {
            buildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget.VisionOS);
            var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (xrSettings == null)
            {
                manager = null;
                return;
            }

            manager = xrSettings.Manager;
        }

        internal static bool IsLoaderEnabled()
        {
            GetXRManagerSettings(out _, out var manager);
            if (manager == null)
                return false;

            var activeLoaders = manager.activeLoaders;
            return activeLoaders != null && activeLoaders.OfType<VisionOSLoader>().Any();
        }

        internal static void DisableLoader()
        {
            GetXRManagerSettings(out var visionOSBuildTargetGroup, out var manager);
            if (manager == null)
                return;

            if (!XRPackageMetadataStore.RemoveLoader(manager, k_VisionOSLoaderTypeName, visionOSBuildTargetGroup))
                Debug.LogError("Failed to disable Apple visionOS XR loader");
        }

        internal static void EnableLoader()
        {
            GetXRManagerSettings(out var visionOSBuildTargetGroup, out var manager);
            if (manager == null)
                return;

            if (!XRPackageMetadataStore.AssignLoader(manager, k_VisionOSLoaderTypeName, visionOSBuildTargetGroup))
                Debug.LogError("Failed to enable Apple visionOS XR loader");
        }

        static string GetXcodeProjectPath(string outputPath)
        {
            return Path.Combine(outputPath, k_XcodeProjectFolder, k_XcodeProjectName);
        }

        class LinkerProcessor : IUnityLinkerProcessor
        {
            public int callbackOrder => 0;

            public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
            {
                return Path.GetFullPath(AssetDatabase.GUIDToAssetPath("bdb2b35a4686f4d8ca0540be9862764d"));
            }
        }
    }
}
