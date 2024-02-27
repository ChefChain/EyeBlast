using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace UnityEditor.PolySpatial.Utilities
{
    static class FlatBufferGenerator
    {
#if POLYSPATIAL_INTERNAL
        [MenuItem("Tools/Generate PolySpatial API", false, 100)]
#endif
        static void Generate()
        {
            var unityEngine = Path.Combine(EditorApplication.applicationContentsPath, "Managed", "UnityEngine","UnityEngine.CoreModule.dll");

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var script = Path.Combine("..", "..", "Tools", "generate-platformapi.cmd");
                RunProcess(
                    $@"""{script}""",
                    $@"""{unityEngine}""");
            }
            else
            {
                var dotnet = Path.Combine(EditorApplication.applicationContentsPath, "NetCoreRuntime", "dotnet");
                RunProcess(
                    Path.Combine("..", "..", "Tools", "generate-platformapi.sh"),
                    unityEngine,
                    new() { { "DOTNET", dotnet } });
            }
        }

        static void RunProcess(string executable, string arguments, Dictionary<string, string> env = null)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            if (env != null)
            {
                foreach (var k in env)
                    psi.Environment.Add(k);
            }

            UnityEngine.Debug.Log($"Running process: {psi.FileName} {psi.Arguments}");

            var standardOutput = new List<string>();
            var standardError = new List<string>();
            using (var p = new Process())
            {
                p.StartInfo = psi;
                p.OutputDataReceived += (sender, args) => standardOutput.Add(args.Data);
                p.ErrorDataReceived += (sender, args) => standardError.Add(args.Data);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    var error =
                        $"Failed running process:\n{psi.FileName} {psi.Arguments}\n\nstdout:\n{string.Join('\n', standardOutput)}\n\nstderr:\n{string.Join('\n', standardError)}";
                    UnityEngine.Debug.LogError(error);
                    throw new Exception(error);
                }
            }
        }
    }
}
