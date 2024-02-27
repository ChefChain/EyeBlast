using System;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.XR.VisionOS
{
    static class ACTool
    {
        internal class ACToolException : Exception
        {
            protected ACToolException() { }

            protected ACToolException(string msg)
                : base(msg) { }
        }

        class ExecutionFailedException : ACToolException
        {
            public ExecutionFailedException(int exitCode, string stdout, string stderr)
                : base($"Execution of actool failed with exit code {exitCode}. stdout:\n{stdout}\nstderr:\n{stderr}")
            { }
        }

        class CompilationFailedException : ACToolException { }

        internal class XCRunNotFoundException : ACToolException
        {
            public XCRunNotFoundException(Exception innerException)
                : base(innerException.ToString())
            { }
        }

        public static string Compile(string assetCatalogPath, string outputDirectory)
        {
            try
            {
                var (stdout, stderr, exitCode) = Cli.Execute($"xcrun", new[]
                {
                    "actool",
                    $"\"{assetCatalogPath}\"",
                    $"--compile \"{outputDirectory}\"",
                    $"--platform xros",
                    $"--minimum-deployment-target 1.0",
                    "--warnings",
                    "--errors"
                });

                if (exitCode != 0)
                    throw new ExecutionFailedException(exitCode, stdout, stderr);

                // Parse the plist
                var plist = Plist.ReadFromString(stdout);
                var outputFiles = plist.root?["com.apple.actool.compilation-results"]?["output-files"]?.AsArray();
                if (outputFiles?.Length < 1)
                    throw new CompilationFailedException();

                return outputFiles?[0].AsString();
            }
            catch (Win32Exception e)
            {
                throw new XCRunNotFoundException(e);
            }
        }
    }
}
