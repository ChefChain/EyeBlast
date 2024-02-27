#if UNITY_VISIONOS
#if !(UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15)
#define UNITY_SUPPORT_FOVEATION
#endif

using UnityEngine;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

#if UNITY_HAS_URP
using UnityEngine.Rendering.Universal;
#endif
#endif

namespace UnityEditor.XR.VisionOS
{
    static partial class VisionOSBuildProcessor
    {
        internal const string HandTrackingUsageWarning = "Hand tracking usage description is required when the visionOS" +
            "loader is enabled. The hand subsystem will be started automatically and requires authorization to run.";

        const string k_ARMWorkaroundOriginal = "--additional-defines=IL2CPP_DEBUG=";
        const string k_ARMWorkaroundReplacement = "--additional-defines=IL2CPP_LARGE_EXECUTABLE_ARM_WORKAROUND=1,IL2CPP_DEBUG=";

        const string k_ARMWorkaroundOriginalAlt = "--compile-cpp";
        const string k_ARMWorkaroundReplacementAlt = "--compile-cpp --additional-defines=IL2CPP_LARGE_EXECUTABLE_ARM_WORKAROUND=1";

#if UNITY_VISIONOS
        const string k_SceneManifestKey = "UIApplicationSceneManifest";
        const string k_SupportsMultipleScenesKey = "UIApplicationSupportsMultipleScenes";
        const string k_SessionRoleKey = "UIApplicationPreferredDefaultSceneSessionRole";
        const string k_SessionRoleValue = "CPSceneSessionRoleImmersiveSpaceApplication";
        const string k_HandsTrackingUsageDescriptionKey = "NSHandsTrackingUsageDescription";
        const string k_WorldSensingUsageDescriptionKey = "NSWorldSensingUsageDescription";

        const string k_KeyboardClassPath = "Classes/UI/Keyboard.mm";
        const string k_KeyboardClassSearchString = @"- (BOOL)textViewShouldBeginEditing:(UITextView*)view
{
#if !PLATFORM_TVOS
    view.inputAccessoryView = viewToolbar;
#endif
    return YES;
}";

        const string k_KeyboardClassReplaceString = @"- (BOOL)textViewShouldBeginEditing:(UITextView*)view
{
#if !PLATFORM_TVOS && !PLATFORM_VISIONOS
    view.inputAccessoryView = viewToolbar;
#endif
    return YES;
}";

        class PostProcessor : IPostprocessBuildWithReport
        {
            // Run last
            public int callbackOrder => 9999;

            public void OnPostprocessBuild(BuildReport report)
            {
                PlayerSettings.SplashScreen.show = s_SplashScreenWasEnabled;
                s_SplashScreenWasEnabled = false;

                if (s_LoaderWasEnabled)
                    EnableLoader();

                s_LoaderWasEnabled = false;

                var isLoaderEnabled = IsLoaderEnabled();
                var outputPath = report.summary.outputPath;
                var settings = VisionOSSettings.currentSettings;
                var appMode = settings.appMode;

                // Do not do any build post-processing for windowed apps
                if (appMode == VisionOSSettings.AppMode.Windowed)
                    return;

                FilterXcodeProj(outputPath, isLoaderEnabled, appMode);
                if (isLoaderEnabled)
                    FilterPlist(outputPath, settings, appMode);
            }

            static void FilterXcodeProj(string outputPath, bool isLoaderEnabled, VisionOSSettings.AppMode appMode)
            {
                var xcodeProjectPath = GetXcodeProjectPath(outputPath);
                if (!File.Exists(xcodeProjectPath))
                {
                    Debug.LogError($"Failed to find Xcode project at path {xcodeProjectPath}");
                    return;
                }

                var pbxProject = new PBXProject();
                pbxProject.ReadFromFile(xcodeProjectPath);

                var unityMainTargetGuid = pbxProject.GetUnityMainTargetGuid();
                var unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

                // Swift version 5 is required for swift trampoline
                pbxProject.SetBuildProperty(unityMainTargetGuid, "SWIFT_VERSION", "5.0");

                // Add legacy TARGET_OS_XR define which was renamed to TARGET_OS_VISION to fix builds on earlier Unity versions
                const string cFlagsSettingName = "OTHER_CFLAGS";
                var cFlagsAddValues = new[] { "-DTARGET_OS_XR=1" };
                pbxProject.UpdateBuildProperty(unityMainTargetGuid, cFlagsSettingName, cFlagsAddValues, null);
                pbxProject.UpdateBuildProperty(unityFrameworkTargetGuid, cFlagsSettingName, cFlagsAddValues, null);

                // Patch Keyboard.mm to work around inputAccessoryView being unavailable
                var keyboardClassPath = Path.Combine(outputPath, k_KeyboardClassPath);
                var keyboardClassContents = File.ReadAllText(keyboardClassPath);
                keyboardClassContents = keyboardClassContents.Replace(k_KeyboardClassSearchString, k_KeyboardClassReplaceString);
                File.WriteAllText(keyboardClassPath, keyboardClassContents);

                if (isLoaderEnabled && appMode == VisionOSSettings.AppMode.VR)
                {
                    const string ldFlagsSettingName = "OTHER_LDFLAGS";

                    // Explicitly export the UnityVisionOS_OnInputEvent so it can be called from Swift code
                    var ldFlagsAddValues = new[]
                    {
                        "-Wl,-exported_symbol,_UnityVisionOS_OnInputEvent"
                    };
                    pbxProject.UpdateBuildProperty(unityMainTargetGuid, ldFlagsSettingName, ldFlagsAddValues, null);
                    pbxProject.UpdateBuildProperty(unityFrameworkTargetGuid, ldFlagsSettingName, ldFlagsAddValues, null);

                    // Remove main.mm which is replaced by swift trampoline
                    pbxProject.RemoveFile(pbxProject.FindFileGuidByProjectPath("MainApp/main.mm"));

                    // Move swift trampoline files from UnityFramework to UnityMain
                    const string pluginPath = "Libraries/com.unity.xr.visionos/Runtime/Plugins/visionos";
                    BuildFileWithUnityTarget(pbxProject, $"{pluginPath}/UnityMain.swift", unityMainTargetGuid, unityFrameworkTargetGuid);
                    BuildFileWithUnityTarget(pbxProject, $"{pluginPath}/UnityLibrary.swift", unityMainTargetGuid, unityFrameworkTargetGuid);
                    AddSettingsFile(pbxProject, outputPath, pluginPath, unityMainTargetGuid);
                }

                var pbxProjectContents = pbxProject.WriteToString();

                // Newer versions use a slightly different IL2CPP script without --additional-defines=IL2CPP_DEBUG=
                pbxProjectContents = pbxProjectContents.Contains(k_ARMWorkaroundOriginal)
                    ? pbxProjectContents.Replace(k_ARMWorkaroundOriginal, k_ARMWorkaroundReplacement)
                    : pbxProjectContents.Replace(k_ARMWorkaroundOriginalAlt, k_ARMWorkaroundReplacementAlt);

                File.WriteAllText(xcodeProjectPath, pbxProjectContents);
            }

            static void BuildFileWithUnityTarget(PBXProject pbx, string file, string unityMainTargetGuid, string unityFrameworkTargetGuid)
            {
                var fileGuid = pbx.FindFileGuidByProjectPath(file);
                pbx.RemoveFileFromBuild(unityFrameworkTargetGuid, fileGuid);
                pbx.AddFileToBuild(unityMainTargetGuid, fileGuid);
            }

            static void FilterPlist(string outputPath, VisionOSSettings settings, VisionOSSettings.AppMode appMode)
            {
                var plistPath = outputPath + "/Info.plist";
                var text = File.ReadAllText(plistPath);
                var plist = Plist.ReadFromString(text);

                if (appMode == VisionOSSettings.AppMode.VR)
                {
                    var sceneManifestDictionary = plist.CreateElement("dict");
                    var supportsMultipleScenesValue = plist.CreateElement("true");
                    sceneManifestDictionary[k_SupportsMultipleScenesKey] = supportsMultipleScenesValue;
                    var sessionRoleValue = plist.CreateElement("string", k_SessionRoleValue);
                    sceneManifestDictionary[k_SessionRoleKey] = sessionRoleValue;
                    plist.root[k_SceneManifestKey] = sceneManifestDictionary;
                }

                // TODO: Enable/disable hand tracking
                var handsUsage = settings.handsTrackingUsageDescription;
                if (string.IsNullOrEmpty(handsUsage))
                    Debug.LogWarning(HandTrackingUsageWarning);
                else
                    plist.root[k_HandsTrackingUsageDescriptionKey] = plist.CreateElement("string", handsUsage);

                // TODO: Scene analysis to detect any managers that will request world sensing
                var worldSensingUsage = settings.worldSensingUsageDescription;
                if (!string.IsNullOrEmpty(worldSensingUsage))
                    plist.root[k_WorldSensingUsageDescriptionKey] = plist.CreateElement("string", worldSensingUsage);

                plist.WriteToFile(plistPath);
            }

            static void AddSettingsFile(PBXProject pbx, string outputPath, string pluginPath, string targetGuid)
            {
                const string fileName = "VisionOSSettings.swift";
                var projectPath = Path.Combine(pluginPath, fileName);
                var fullPath = Path.Combine(outputPath, projectPath);
                File.WriteAllText(fullPath, GetSettingsString());
                var guid = pbx.AddFile(fullPath, projectPath);
                pbx.AddFileToBuild(targetGuid, guid);
            }

            static string GetSettingsString()
            {
                const string format = "var VisionOSEnableFoveation = {0}";
#if UNITY_HAS_URP && UNITY_SUPPORT_FOVEATION
                var hasUrpAsset = UniversalRenderPipeline.asset != null;
                if (PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Device && hasUrpAsset)
                    return string.Format(format, "true");
#endif

                return string.Format(format, "false");
            }
        }
#endif
    }
}
