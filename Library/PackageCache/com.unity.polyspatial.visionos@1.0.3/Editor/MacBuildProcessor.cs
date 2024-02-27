#if UNITY_VISIONOS || UNITY_IOS || UNITY_EDITOR_OSX
#if POLYSPATIAL_INTERNAL
using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Unity.PolySpatial.Internals.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEditor.PolySpatial.Utilities;
using Debug = UnityEngine.Debug;

namespace Unity.PolySpatial.Internals.Editor
{
    internal class PolySpatialMacBuildProcessor : IPostprocessBuildWithReport
    {
        readonly static string k_RequiredXcodeVersionFriendlyName = "Xcode 15.0 beta 8";
        readonly static string k_RequiredXcodeVersion = "15A5229m";

        public int callbackOrder => 150;

        bool m_isNewPlugin = false;

        readonly static string k_PackageLibPath = "Packages/com.unity.polyspatial.visionos/Lib~";
        readonly static string k_NewPluginName = "PolySpatial-macOSNew.bundle";
        readonly static string k_OldPluginName = "PolySpatial-macOS.bundle";

        string PluginName => m_isNewPlugin ? k_NewPluginName : k_OldPluginName;

        internal static bool HasNewPlugin()
        {
            return Directory.Exists(Path.Combine(k_PackageLibPath, k_NewPluginName));
        }

        internal static bool HasOldPlugin()
        {
            return Directory.Exists(Path.Combine(k_PackageLibPath, k_OldPluginName));
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
                return;

            var runtimeLinked = PolySpatialSettings.RuntimeModeForceLinked;
            var runtimeEnabled = PolySpatialSettings.RuntimeModeForceEnabled;

            if (!runtimeLinked)
            {
                return;
            }

            if (runtimeEnabled)
            {
                var bootConfig = new BootConfigBuildUtility(report);
                bootConfig.SetValue("polyspatial", "1");
                bootConfig.Write();
            }

            m_isNewPlugin = HasNewPlugin();

            if (!m_isNewPlugin && !HasOldPlugin())
            {
#if POLYSPATIAL_INTERNAL
                Debug.LogWarning($"Expected to find {k_OldPluginName} or {k_NewPluginName} in {k_PackageLibPath}, but it doesn't exist");
#endif
                return;
            }

            if (m_isNewPlugin)
            {
                Debug.Log($"Using {PluginName}");
            }

            try
            {
                CopyMacPlugin(report);
                if (m_isNewPlugin)
                    AddEnvironmentForXcode15b8(report);
            }
            catch (Exception e)
            {
                throw new BuildFailedException(e);
            }
        }

        void CopyMacPlugin(BuildReport report)
        {
            var path = report.summary.outputPath;
            var projectName = PlayerSettings.productName;

            var pluginBundle = Path.GetFullPath(Path.Combine(k_PackageLibPath, PluginName));
            if (Path.GetExtension(path) == ".app")
            {
                BuildUtils.CopyDirectoryTo(pluginBundle, Path.Combine(path, "Contents", "PlugIns"));
            }
            else
            {
                BuildUtils.CopyDirectoryTo(pluginBundle, Path.Combine(path, projectName, "PlugIns"));

                var pbxPath = Path.Combine(path, Path.GetFileName(path) + ".xcodeproj", "project.pbxproj");

                var projText = File.ReadAllText(pbxPath);

                // Don't add it twice, in the case of append builds
                if (!projText.Contains(PluginName))
                {
                    var proj = new PBXProject();
                    proj.ReadFromFile(pbxPath);

                    var target = BuildUtils.FindTargetGuidByName(proj, projectName);
                    var pluginGuid = BuildUtils.AddFileToProject(proj, $"{projectName}/PlugIns/{PluginName}");
                    foreach (var phaseGuid in proj.GetAllBuildPhasesForTarget(target))
                    {
                        if (proj.GetBuildPhaseName(phaseGuid) == "CopyPlugIns")
                        {
                            proj.AddFileToBuildSection(target, phaseGuid, pluginGuid);
                            break;
                        }
                    }

                    proj.WriteToFile(pbxPath);
                }
            }
        }

        static string GetXcodeSelectDeveloperPath()
        {
            var (ok, xcodeSelectPath) = BuildUtils.RunCommandWithOutput("xcode-select", "-p");
            if (!ok || xcodeSelectPath.Contains(" "))
            {
                throw new BuildFailedException($"Either xcode-select -p failed, or path contains a space: {xcodeSelectPath}");
            }

            return xcodeSelectPath.Trim();
        }

        static string GetXcode15b8DeveloperPath()
        {
            var xcodeDeveloperPaths = new[]
            {
                GetXcodeSelectDeveloperPath(),
                "/Applications/Xcode-15b8.app/Contents/Developer",
                "/opt/UnitySrc/Xcode-15b8.app/Contents/Developer",
                "/Users/bokken/Xcode-15b8.app/Contents/Developer",
            };

            foreach (var path in xcodeDeveloperPaths)
            {
                if (!Directory.Exists(path))
                    continue;

                var (ok, output) = BuildUtils.RunCommandWithOutput("plutil", $"-extract ProductBuildVersion raw {path}/../version.plist");
                if (!ok)
                {
                    throw new BuildFailedException($"Error using plutil to extract ProductBuildVersion from {path}/../version.plist: {output}");
                }

                if (output.Trim() == k_RequiredXcodeVersion)
                {
                    return path;
                }
            }

            throw new BuildFailedException($"Couldn't find {k_RequiredXcodeVersionFriendlyName} at any of the following paths:\n" +
                                           String.Join("\n", xcodeDeveloperPaths) + "\n" +
                                           "Please install it and try again.");
        }

        [Conditional("UNITY_EDITOR_OSX")]
        void AddEnvironmentForXcode15b8(BuildReport report)
        {
            var projectName = PlayerSettings.productName;

            // Add it to LSEnvironment in the Info.plist; this takes into affect
            // only when executed from the Finder, via `open` on the command line,
            // or when Unity itself launches the app. This will only work if Xcode 15b8
            // is the xcode-select path.
            var infoPlistInXcode = Path.Combine(report.summary.outputPath, projectName, "Info.plist");
            var infoPlistInApp = Path.Combine(report.summary.outputPath, "Contents", "Info.plist");

            var xcodeDeveloperPath = GetXcode15b8DeveloperPath();

            foreach (var plist in new[] { infoPlistInApp, infoPlistInXcode })
            {
                if (!File.Exists(plist))
                    continue;

                // Note: these will fail if the key exists, which could be a problem for LSEnvironment; Unity doesn't set anything there by default.
                if (!File.ReadAllText(plist).Contains("DYLD_FRAMEWORK_PATH"))
                {
                    BuildUtils.RunCommand("plutil", $"-insert LSEnvironment -dictionary \"{plist}\"");
                    BuildUtils.RunCommand("plutil", $"-insert LSEnvironment.DYLD_FRAMEWORK_PATH -string {xcodeDeveloperPath}/../SharedFrameworks \"{plist}\"");
                    BuildUtils.RunCommand("plutil", $"-insert LSEnvironment.DYLD_VERSIONED_FRAMEWORK_PATH -string {xcodeDeveloperPath}/../SystemFrameworks \"{plist}\"");
                }
            }

            var pbxPath = Path.Combine(report.summary.outputPath,
                Path.GetFileName(report.summary.outputPath) + ".xcodeproj", "project.pbxproj");
            if (File.Exists(pbxPath))
            {
                var pbxStr = File.ReadAllText(pbxPath);
                // Don't add this twice
                if (!pbxStr.Contains($"/../SystemFrameworks"))
                {
                    pbxStr = pbxStr.Replace(
                        "PRODUCT_BUNDLE_IDENTIFIER =",
                        // reference private frameworks inside Xcode 15b8
                        "OTHER_LDFLAGS = \"$(inherited) " +
                        $"-Wl,-dyld_env,DYLD_FRAMEWORK_PATH={xcodeDeveloperPath}/../SharedFrameworks " +
                        $"-Wl,-dyld_env,DYLD_VERSIONED_FRAMEWORK_PATH={xcodeDeveloperPath}/../SystemFrameworks\";\n" +
                        "PRODUCT_BUNDLE_IDENTIFIER ="
                    );

                    File.WriteAllText(pbxPath, pbxStr);
                }
            }
        }
    }
}
#endif
#endif
