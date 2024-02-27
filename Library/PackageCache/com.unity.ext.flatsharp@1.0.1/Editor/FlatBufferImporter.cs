using System;
using System.Diagnostics;
using UnityEngine;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class FlatBufferImporter
{
    const string outputFileName = "FlatSharp.generated.cs";

    public static void RunFlatSharpCompiler(string inputFile, string outputFile)
    {
        var inputFilePath = Path.GetFullPath(inputFile);
        var outputDirectory = Path.Combine(Application.temporaryCachePath, "FlatSharp");
        var tempOutputFile = Path.Combine(outputDirectory, outputFileName);

        if (File.Exists(tempOutputFile))
            File.Delete(tempOutputFile);

        Directory.CreateDirectory(outputDirectory);

        var compiler = Path.GetFullPath(Path.Combine("Packages", "com.unity.ext.flatsharp", "Tools~", "FlatSharp.Compiler", "FlatSharp.Compiler.dll"));
        var unityEngine = Path.Combine(EditorApplication.applicationContentsPath, "Managed", "UnityEngine","UnityEngine.CoreModule.dll");

        RunProcess(Application.platform == RuntimePlatform.WindowsEditor ? "dotnet.exe" : "dotnet",
            $"{compiler} --normalize-field-names true --flatc-path \"{GetFlatCCompilerPath()}\" --input \"{inputFilePath}\" --output \"{outputDirectory}\" --unity-assembly-path=\"{unityEngine}\"");

        File.Copy(tempOutputFile, outputFile, true);
    }

    public static void RunFlatCCompiler(string inputFile, string outputDirectory)
    {
        var inputFilePath = Path.GetFullPath(inputFile);
        var outputDirectoryPath = Path.GetFullPath(outputDirectory);

        Directory.CreateDirectory(outputDirectoryPath);

        var flatc = GetFlatCCompilerPath();
        RunProcess(flatc, $"-o \"{outputDirectoryPath}\" --filename-suffix \".gen\" --swift \"{inputFilePath}\"");
    }

    static string GetFlatCCompilerPath()
    {
        var flatc = Path.Combine("Packages", "com.unity.ext.flatsharp", "Tools~", "FlatSharp.Compiler", "flatc");
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                flatc = Path.Combine(flatc, "macos", "flatc");
                break;
            case RuntimePlatform.WindowsEditor:
                flatc = Path.Combine(flatc, "windows", "flatc.exe");
                break;
            case RuntimePlatform.LinuxEditor:
                flatc = Path.Combine(flatc, "linux", "flatc");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return  Path.GetFullPath(flatc);
    }

    static void RunProcess(string executable, string arguments)
    {
        var psi = new ProcessStartInfo()
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        Debug.Log($"Running process: {psi.FileName} {psi.Arguments}");

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
                    $"Failed running flatc:\n{psi.FileName} {psi.Arguments}\n\nstdout:\n{string.Join('\n', standardOutput)}\n\nstderr:\n{string.Join('\n', standardError)}";
                Debug.LogError(error);
                throw new Exception(error);
            }
        }
    }

    [MenuItem("Assets/FlatSharp/Generate Package Tests", false, 10)]
    static void Generate()
    {
        var inputFile = Path.GetFullPath(Path.Combine("Packages", "com.unity.ext.flatsharp", "Tests", "Runtime", "test.fbs"));
        RunFlatSharpCompiler(inputFile, Path.ChangeExtension(inputFile, ".generated.cs"));
    }
}
