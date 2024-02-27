#if UNITY_VISIONOS || UNITY_IOS || UNITY_EDITOR_OSX
using System.Runtime;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEditor.PolySpatial.Utilities;
using UnityEditor.UnityLinker;
using UnityEditor.XR.VisionOS;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Unity.PolySpatial.Internals.Editor
{
    internal class VisionOSBuildPreProcessor : IPreprocessBuildWithReport
    {
        internal const string k_XcodeProjName = "Unity-VisionOS.xcodeproj";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            DoPreprocessBuild(report);
        }

        [Conditional("UNITY_VISIONOS")]
        public void DoPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.VisionOS)
                return;

            var xrSettings = VisionOSSettings.currentSettings;
            var autoMeansEnabled = xrSettings != null && xrSettings.appMode == VisionOSSettings.AppMode.MR;

            BuildUtils.GetRuntimeFlagsForAuto(autoMeansEnabled, out var runtimeEnabled, out var runtimeLinked);

            if (!runtimeLinked)
            {
                return;
            }

#if POLYSPATIAL_INTERNAL && UNITY_EDITOR_OSX
            if (runtimeLinked)
            {
                RealityKitPluginBuilder.BuildVisionOSXRPlugin();
            }
#endif

            try
            {
                SwiftAppShellProcessor.RestoreXcodeProject(report.summary.outputPath, k_XcodeProjName);
            }
            catch (Exception e)
            {
                throw new BuildFailedException(e);
            }
        }
    }

    static class PListElementDictExtensions
    {
        internal static PlistElementDict GetOrCreateDict(this PlistElementDict dict, string key)
        {
            if (dict.values.ContainsKey(key))
                return dict[key].AsDict();
            return dict.CreateDict(key);
        }
    }

    internal class VisionOSBuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 150; // after the plugin builder

        public static bool isSimulator
        {
            get
            {
                return PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            DoPostprocessBuild(report);
        }

        [Conditional("UNITY_VISIONOS")]
        public void DoPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.VisionOS)
                return;

            var xrSettings = VisionOSSettings.currentSettings;
            var autoMeansEnabled = xrSettings != null && xrSettings.appMode == VisionOSSettings.AppMode.MR;

            BuildUtils.GetRuntimeFlagsForAuto(autoMeansEnabled, out var runtimeEnabled, out var runtimeLinked);

            if (!runtimeLinked)
            {
                return;
            }

            try
            {
                var outputPath = report.summary.outputPath;
                WriteVisionOSSettings(outputPath);

                if (runtimeEnabled)
                {
                    var bootConfig = new BootConfigBuildUtility(report);
                    bootConfig.SetValue("polyspatial", "1");
                    bootConfig.Write();
                }

                Dictionary<string, string> extraSourceFiles = new Dictionary<string, string>()
                {
                    { "MainApp/UnityVisionOSSettings.swift", null }
                };

                SwiftAppShellProcessor.ConfigureXcodeProject(report.summary.platform, outputPath,
                    VisionOSBuildPreProcessor.k_XcodeProjName,
                    il2cppArmWorkaround: true,
                    staticLibraryPluginName: isSimulator ? "libPolySpatial_xrsimulator.a" : "libPolySpatial_xros.a",
                    extraSourceFiles: extraSourceFiles
                );

                FilterXcodeProj(outputPath, VisionOSBuildPreProcessor.k_XcodeProjName);
                FilterPlist(outputPath);
            }
            catch (Exception e)
            {
                throw new BuildFailedException(e);
            }
        }

        // Convert the Vector3 dimensions into various snippets of Swift and identifiers we'll need
        static void DimensionsToSwiftStrings(Vector3 dim, out string swiftVec3, out string swiftSizeParams, out string volIdent)
        {
            // tostring with 3 decimal points, and convert to xxx.abc 3 digits of precision in meters consistently
            var dims = new string[] { dim.x.ToString("F3"), dim.y.ToString("F3"), dim.z.ToString("F3") };
            swiftVec3 = $".init({dims[0]}, {dims[1]}, {dims[2]})";
            swiftSizeParams = $"width: {dims[0]}, height: {dims[1]}, depth: {dims[2]}, in: .meters";
            volIdent = $"Bounded-{dims[0]}x{dims[1]}x{dims[2]}";
        }

        // Create a ImmersiveSpace or WindowGroup entry for the specified volume camera configuration. Optionally pass in a name for this entry, or
        // opt to use the one that DimensionsToSwiftStrings() provides.
        static string CreateWindowConfigurationEntry(
            Vector3 dim,
            VolumeCamera.PolySpatialVolumeCameraMode mode,
            string configName)
        {
            DimensionsToSwiftStrings(dim, out var dimsVec3, out var dimsSizeParams, out var _);

            var windowType = "";
            var windowStyle = "";
            switch (mode)
            {
                case VolumeCamera.PolySpatialVolumeCameraMode.Bounded:
                    windowType = "WindowGroup";
                    windowStyle = $".windowStyle(.volumetric).defaultSize({dimsSizeParams})";
                    break;
                case VolumeCamera.PolySpatialVolumeCameraMode.Unbounded:
                    windowType = "ImmersiveSpace";
                    windowStyle = "";
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected VolumeCameraConfiguration mode {mode}");
            }

            // The entry in the App Scene for these types of windows
            return $@"
        {windowType}(id: ""{configName}"", for: UUID.self) {{ uuid in
            PolySpatialContentViewWrapper()
                .environment(\.pslWindow, PolySpatialWindow(uuid.wrappedValue, ""{configName}"", {dimsVec3}))
                    KeyboardTextField()
                .frame(width: 0, height: 0)
        }} defaultValue: {{ UUID() }} {windowStyle}";
        }

        // Create a scene that encapsulates ImmersiveSpace/WindowGroup entries for a given list of volume camera configurations.
        static string CreateWindowConfigurationScene(
            Vector3[] dims,
            string sceneName,
            out string listOfConfigs,
            out string listMatchableConfigs)
        {
            var configEntries = new StringBuilder();
            var delineator = "";
            listOfConfigs = "";
            listMatchableConfigs = "";

            for (var i = 0; i < dims.Length; i++)
            {
                // Create an entry for this in both the list for all volume cameras
                // and the list for the the matchable volume cameras.
                var dim = dims[i];
                DimensionsToSwiftStrings(dim, out var _, out var _, out var configName);

                listOfConfigs += delineator + $"\"{configName}\"";
                listMatchableConfigs += delineator + $".init{dim.ToString("f3")}";
                delineator = ", ";

                // Create an entry for this bounded volume camera.
                var swift = CreateWindowConfigurationEntry(dim, VolumeCamera.PolySpatialVolumeCameraMode.Bounded, configName);
                configEntries.Insert(configEntries.Length, swift);
            }

            return $"@SceneBuilder\n" +
                   $"    var {sceneName}: some Scene {{\n" +
                   $"{configEntries.ToString()}\n" +
                   $"    }}";
        }

#if PLAY_TO_DEVICE
        static String CreatePlayToDeviceConfigurationScene(
            String sceneName,
            ref String allConfigs,
            String allConfigDelineator,
            ref String matchableConfigs,
            String matchableConfigDelineator)
        {
            // TODO LXR-2979: hardcoded for now, will be moved somewhere else.
            Vector3[] possibleDimValues = {
                new Vector3(0.25f, 0.25f, 0.25f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(2.0f, 2.0f, 2.0f),
                new Vector3(3.0f, 3.0f, 3.0f),
                new Vector3(1.33f, 1.0f, 1.0f), // 4:3:3
                new Vector3(1.77f, 1.0f, 1.0f), // 16:9:9
                new Vector3(2.0f, 1.0f, 1.0f), // 2:1:1
                new Vector3(1.0f, 1.0f, 2.0f), // 1:1:2
                new Vector3(1.0f, 1.0f, 1.41f), // 1:1:1.41
            };

            var scene = CreateWindowConfigurationScene(
                possibleDimValues,
                sceneName,
                out var listOfConfigs,
                out var listMatchableConfigs);

            matchableConfigs += matchableConfigDelineator + listMatchableConfigs;
            allConfigs += allConfigDelineator + listOfConfigs;

            return scene;
        }
#endif

        static void WriteVisionOSSettings(string outputPath)
        {
            List<VolumeCameraWindowConfiguration> configurations = new();

            // TODO -- load referenced items from scenes
            configurations.AddRange(Resources.LoadAll<VolumeCameraWindowConfiguration>(""));

            var initialConfig = GetDefaultVolumeConfig();

            if (!configurations.Contains(initialConfig))
            {
                configurations.Add(initialConfig);
            }

            StringBuilder sceneContent = new();
            // Scene name encapsulating all window configurations that were found in Resources/
            var windowConfigurationsScene = "windowConfigurations";
            var allScenes = windowConfigurationsScene;

            var allAvailableConfigs = "";
            var delineator = "";

            foreach (var config in configurations)
            {
                var configName = NameForVolumeConfig(config);

                // for Unbounded, we always treat is as 1.0 (?)
                var dim = config.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded ? Vector3.one : config.Dimensions;
                var swift = CreateWindowConfigurationEntry(dim, config.Mode, configName);

                // The default/initial configuration needs to be first in the Scene list,
                // because this is what the OS will open on launch. Otherwise just append to the string.
                sceneContent.Insert(config == initialConfig ? 0 : sceneContent.Length, swift);

                allAvailableConfigs += delineator + $"\"{configName}\"";
                delineator = ", ";
            }

            var ptdWindowConfigScene = "";
            var availableConfigsForMatch = "";
#if PLAY_TO_DEVICE
            {
                var ptdBoundedSceneName = "ptdWindowConfigurations";
                ptdWindowConfigScene += CreatePlayToDeviceConfigurationScene(
                    ptdBoundedSceneName,
                    ref allAvailableConfigs,
                    delineator,
                    ref availableConfigsForMatch,
                    "");

                allScenes += $"\n        {ptdBoundedSceneName}";
            }
#endif

            var parameters = isSimulator ?
                PolySpatialSettings.instance.SimulatorDisplayProviderParameters :
                PolySpatialSettings.instance.DeviceDisplayProviderParameters;

// ==============================
// the template of the entire file.
            var content = $@"// GENERATED BY BUILD
import Foundation
import SwiftUI
import PolySpatialRealityKit

extension UnityPolySpatialApp {{
    func initialWindowName() -> String {{ return ""{NameForVolumeConfig(initialConfig)}"" }}

    func getAllAvailableWindows() -> [String] {{ return [{allAvailableConfigs}] }}

    func getAvailableWindowsForMatch() -> [simd_float3] {{ return [{availableConfigsForMatch}] }}

    func displayProviderParameters() -> DisplayProviderParameters {{
        return .init(
            framebufferWidth: {parameters.framebufferWidth},
            framebufferHeight: {parameters.framebufferHeight},
            leftEyePose: .init(position: .init(x: {parameters.leftEyePose.position.x},
                                               y: {parameters.leftEyePose.position.y},
                                               z: {parameters.leftEyePose.position.z}),
                               rotation: .init(x: {parameters.leftEyePose.rotation.x},
                                               y: {parameters.leftEyePose.rotation.y},
                                               z: {parameters.leftEyePose.rotation.z},
                                               w: {parameters.leftEyePose.rotation.w})),
            rightEyePose: .init(position: .init(x: {parameters.rightEyePose.position.x},
                                                y: {parameters.rightEyePose.position.y},
                                                z: {parameters.rightEyePose.position.z}),
                                rotation: .init(x: {parameters.rightEyePose.rotation.x},
                                                y: {parameters.rightEyePose.rotation.y},
                                                z: {parameters.rightEyePose.rotation.z},
                                                w: {parameters.rightEyePose.rotation.w})),
            leftProjectionHalfAngles: .init(left: {parameters.leftProjectionHalfAngles.left},
                                            right: {parameters.leftProjectionHalfAngles.right},
                                            top: {parameters.leftProjectionHalfAngles.top},
                                            bottom: {parameters.leftProjectionHalfAngles.bottom}),
            rightProjectionHalfAngles: .init(left: {parameters.rightProjectionHalfAngles.left},
                                             right: {parameters.rightProjectionHalfAngles.right},
                                             top: {parameters.rightProjectionHalfAngles.top},
                                             bottom: {parameters.rightProjectionHalfAngles.bottom})
        )
    }}

    @SceneBuilder
    var {windowConfigurationsScene}: some Scene {{
{sceneContent.ToString()}
    }}

    {ptdWindowConfigScene}

    @SceneBuilder
    var mainScene: some Scene {{
        {allScenes}
    }}
}}
";
// ==============================

            File.WriteAllText(Path.Combine(outputPath, "MainApp", "UnityVisionOSSettings.swift"), content);
        }

        static VolumeCameraWindowConfiguration GetDefaultVolumeConfig()
        {
            var initialConfig = PolySpatialSettings.instance.DefaultVolumeCameraWindowConfiguration;
            if (initialConfig == null)
            {
                // handle projects without this setting that have never opened the PolySpatial Settings window
                initialConfig = Resources.Load<VolumeCameraWindowConfiguration>("Default Unbounded Configuration");
            }

            return initialConfig;
        }

        static string NameForVolumeConfig(VolumeCameraWindowConfiguration config)
        {
            switch (config?.Mode)
            {
                case VolumeCamera.PolySpatialVolumeCameraMode.Bounded:
                    DimensionsToSwiftStrings(config.Dimensions, out var _, out var _, out var volIdent);
                    return volIdent;

                case VolumeCamera.PolySpatialVolumeCameraMode.Unbounded:
                case null:
                    return "Unbounded";
            }

            throw new InvalidOperationException($"Unexpected VolumeCameraConfiguration mode {config.Mode}");
        }

        void ReplaceStrings(ref string contents, string[][] replacements)
        {
            foreach (var subs in replacements)
            {
                if (!contents.Contains(subs[0]))
                {
                    Debug.LogWarning($"BuildProcessor ReplaceStrings: couldn't find string '{subs[0]}'");
                }

                contents = contents.Replace(subs[0], subs[1]);
            }
        }

        void FilterXcodeProj(string outputPath, string xcodeProjName)
        {
            var xcodeProj = Path.Combine(outputPath, xcodeProjName);
            var xcodePbx = Path.Combine(xcodeProj, "project.pbxproj");

            var pbx = new PBXProject();
            pbx.ReadFromFile(xcodePbx);

            // add in -ld argument, for object file compat
            foreach (var tgt in new[] { pbx.GetUnityFrameworkTargetGuid(), pbx.GetUnityMainTargetGuid() })
            {
                foreach (var cfgname in pbx.BuildConfigNames())
                {
                    var cfguid = pbx.BuildConfigByName(tgt, cfgname);
                    if (cfguid == null)
                        continue;

                    var existing = pbx.GetBuildPropertyForConfig(cfguid, "OTHER_LDFLAGS") ?? "";
                    pbx.SetBuildPropertyForConfig(cfguid, "OTHER_LDFLAGS", $"-ld_classic -Wl,-exported_symbol,_SetPolySpatialNativeAPIImplementation {existing}");

                    // TODO: remove this from the template (sets to YES)
                    pbx.SetBuildPropertyForConfig(cfguid, "INFOPLIST_KEY_UIApplicationSceneManifest_Generation", "NO");

                    var cflags = pbx.GetBuildPropertyForConfig(cfguid, "OTHER_CFLAGS") ?? "";

                    // Add TARGET_OS_XR define which was renamed to TARGET_OS_VISION in visionOS beta 2 (Xcode beta 5)
                    cflags = $"-DTARGET_OS_XR=1 {cflags}";

                    // Add UNITY_POLYSPATIAL for stub
                    cflags = $"-DUNITY_POLYSPATIAL=1 {cflags}";

                    pbx.SetBuildPropertyForConfig(cfguid, "OTHER_CFLAGS", cflags);
                }
            }

            pbx.WriteToFile(xcodePbx);
        }

        private void FilterPlist(string outputPath)
        {
            var settings = VisionOSSettings.currentSettings;
            if (settings.appMode == VisionOSSettings.AppMode.VR)
                return;

            var plistPath = outputPath + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var root = plist.root;

            // TODO -- remove these from template!
            root.values.Remove("UIRequiredDeviceCapabilities");
            root.values.Remove("UILaunchStoryboardName");
            root.values.Remove("UILaunchStoryboardName~iphone");
            root.values.Remove("UILaunchStoryboardName~ipad");
            root.values.Remove("UILaunchStoryboardName~ipod"); // lol?
            root.values.Remove("LSRequiresIPhoneOS");
            root.values.Remove("UIRequiresFullScreen");
            root.values.Remove("UIStatusBarHidden");
            root.values.Remove("UIViewControllerBasedStatusBarAppearance");

            PlistElementDict sceneManifest = root.GetOrCreateDict("UIApplicationSceneManifest");

            sceneManifest["UIApplicationSupportsMultipleScenes"] = new PlistElementBoolean(true);

            var initialConfig = GetDefaultVolumeConfig();

            if (initialConfig.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)
            {
                sceneManifest.SetString("UIApplicationPreferredDefaultSceneSessionRole", "UISceneSessionRoleImmersiveSpaceApplication");
                var sceneConfigs = sceneManifest.CreateDict("UISceneConfigurations");
                var array = sceneConfigs.CreateArray("UISceneSessionRoleImmersiveSpaceApplication");
                var dict = array.AddDict();
                dict.SetString("UISceneConfigurationName", "Unbounded");
                dict.SetString("UISceneInitialImmersionStyle", "UIImmersionStyleMixed");

                // remove PreferredLaunchSize if present from previous build
                if (root.values.ContainsKey("UILaunchPlacementParameters"))
                {
                    var launchParams = root["UILaunchPlacementParameters"].AsDict();
                    launchParams.values.Remove("PreferredLaunchSize");
                }

            }
            else if (initialConfig.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Bounded)
            {
                sceneManifest.SetString("UIApplicationPreferredDefaultSceneSessionRole", "UIWindowSceneSessionRoleVolumetricApplication");

                float metersToPoints = 2834.65f;

                var launchParams = root.GetOrCreateDict("UILaunchPlacementParameters");
                var preferredSize = launchParams.GetOrCreateDict("PreferredLaunchSize");
                // these are always in points
                preferredSize.SetReal("Width", initialConfig.Dimensions.x * metersToPoints);
                preferredSize.SetReal("Height", initialConfig.Dimensions.y * metersToPoints);
                preferredSize.SetReal("Depth", initialConfig.Dimensions.z * metersToPoints);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected VolumeCameraConfiguration mode {initialConfig.Mode}");
            }

            plist.WriteToFile(plistPath);
        }
    }
}
#endif
