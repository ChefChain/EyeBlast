using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.PolySpatial.Internals;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.PolySpatial.Utilities
{
    // Note that PostprocessBuildUtillity.AddProjectBuildConfigEntry and friends exist,
    // but it's not clear when/where the data is actually added. The entries seem to persist
    // across builds, and by the time build postprocessors are called it's too late to add
    // entries. But entries added persist for the _next_ time a build is done, which I have
    // no idea if it's intentional.
    //
    // This helper utility just modifies an already-written boot.config directly.
    internal class BootConfigBuildUtility
    {
        public static string BootConfigPathForBuild(BuildReport report)
        {
            var platform = report.summary.platform;

            var projectName = PlayerSettings.productName;

            string bootConfigPath = null;

            switch (platform)
            {
                case BuildTarget.StandaloneOSX:
                    var bootConfigInXcode = Path.Combine(report.summary.outputPath, projectName, "Resources", "Data", "boot.config");
                    var bootConfigInApp = Path.Combine(report.summary.outputPath, "Contents", "Resources", "Data", "boot.config");

                    bootConfigPath = File.Exists(bootConfigInApp) ? bootConfigInApp : bootConfigInXcode;
                    break;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                    // outputPath here is the actual .exe/binary
                    var outname = Path.GetFileNameWithoutExtension(report.summary.outputPath);
                    var dirname = Path.GetDirectoryName(report.summary.outputPath);

                    bootConfigPath = Path.Combine(dirname, $"{outname}_Data", "boot.config");
                    break;

                case BuildTarget.iOS:
                case BuildTarget.tvOS:
                case BuildTarget.VisionOS:
                    bootConfigPath = Path.Combine(report.summary.outputPath, "Data", "boot.config");
                    break;

                default:
                    return null;
            }

            if (!File.Exists(bootConfigPath))
            {
                return null;
            }

            return bootConfigPath;
        }

        string bootConfigPath;

        BootConfigUtility bootConfig;

        public BootConfigBuildUtility(BuildReport report)
        {
            bootConfigPath = BootConfigPathForBuild(report);
            if (bootConfigPath == null)
            {
                throw new System.Exception("Couldn't find boot.config for player built in " + report.summary.outputPath);
            }

            bootConfig = new BootConfigUtility(bootConfigPath);
        }

        public void Write()
        {
            var data = bootConfig.ToString();
            File.WriteAllText(bootConfigPath, data);
        }

        public void SetValue(string key, string value)
        {
            bootConfig.SetValue(key, value);
        }

        public string GetValue(string key)
        {
            return bootConfig.GetValue(key);
        }
    }
}
