#if UNITY_VISIONOS || UNITY_IOS || UNITY_EDITOR_OSX
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.iOS.Xcode;

namespace Unity.PolySpatial.Internals.Editor
{
    internal static class BuildUtils
    {
        public static bool IsPackageImmutable()
        {
            return Path.GetFullPath("Packages/com.unity.polyspatial.visionos").Contains("PackageCache");
        }

        static MD5 md5Hasher = MD5.Create();

        /// <summary>
        /// Copy a file from one location to another. Depending on options we may or may not do the copy.
        /// By default, we check if the files are different and if not, skip copying.
        /// .meta files and files starting with '.' are ignored.
        /// </summary>
        /// <param name="sourceFile">Source file to copy.</param>
        /// <param name="destinationFile">The location and name of the file to copy to.</param>
        /// <param name="deleteSource">If true, we delete the source file after copying.</param>
        /// <param name="forceOverwrite">If true, we over-right the destination file regardless on if there is a difference or not.</param>
        public static void CopyFile(string sourceFile, string destinationFile, bool deleteSource = false,
            bool forceOverwrite = false)
        {
            bool shouldCopy = true;

            if (File.Exists(destinationFile) && !forceOverwrite)
            {
                var sourceData = File.ReadAllBytes(sourceFile);
                var destData = File.ReadAllBytes(destinationFile);

                var sourceHash = md5Hasher.ComputeHash(sourceData);
                var destHash = md5Hasher.ComputeHash(destData);

                if (sourceHash.SequenceEqual(destHash))
                    shouldCopy = false;
                else
                    forceOverwrite = true;
            }

            if (shouldCopy)
                File.Copy(sourceFile, destinationFile, forceOverwrite);

            if (deleteSource)
                File.Delete(sourceFile);
        }

        /// <summary>
        /// Takes an array of ("src", "dst") tuples.
        /// If src is a directory, copies it into dst.
        /// If src is a file, copies it into dst (either new filename or directory)
        /// If throwIfMissing is true, this will throw if a src file/dir is missing.
        /// </summary>
        public static void CopyAll((string src, string dst)[] all, bool throwIfMissing = false)
        {
            foreach (var pair in all)
            {
                if (Directory.Exists(pair.src))
                {
                    CopyFilesFromDirectory(pair.src, pair.dst, recursive: true);
                }
                else if (File.Exists(pair.src))
                {
                    var dst = pair.dst;
                    if (Directory.Exists(dst))
                        dst = Path.Combine(pair.dst, Path.GetFileName(pair.src));
                    CopyFile(pair.src, dst);
                }
                else if (throwIfMissing)
                {
                    throw new FileNotFoundException($"Couldn't find required source file: {pair.src}");
                }
            }
        }

        /// <summary>
        /// Copy sourceFile into directory destinationDir, ensuring destinationDir exists
        /// </summary>
        public static void CopyFileTo(string sourceFile, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            CopyFile(sourceFile, Path.Combine(destinationDir, Path.GetFileName(sourceFile)));
        }

        /// <summary>
        /// Copy sourceDir to destinationDir, creating a new directory named sourceDir inside, and ensuring
        /// that destinationDir exists.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        public static void CopyDirectoryTo(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new InvalidOperationException($"Directory {sourceDir} does not exist or is a file");
            }

            // ensure dest exists
            var destPath = Path.Combine(destinationDir, Path.GetFileName(sourceDir));
            Directory.CreateDirectory(destPath);
            CopyFilesFromDirectory(sourceDir, destPath, recursive: true);
        }

        public static void CopyFilesFromDirectory(string sourceDir, string destinationDir, bool recursive = true, bool deleteFolder = false)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                if (File.Exists(targetFilePath) && file.CreationTime <= File.GetCreationTime(targetFilePath))
                    continue;

                CopyFile(file.FullName, targetFilePath, false, false);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyFilesFromDirectory(subDir.FullName, newDestinationDir, true);
                }
            }

            if (deleteFolder)
                Directory.Delete(sourceDir, true);
        }

        /// <summary>
        /// Add a file to an XCode project.
        /// </summary>
        /// <param name="proj">Instance of PBXProject representing the XCode project we are modifying.</param>
        /// <param name="filePath">The path to the file to add to the project.</param>
        /// <param name="projectPath">An optional group path for the file. The path must specify the "folder" path from the root project
        /// item and end with the file name we are adding.
        ///
        /// Example:
        ///
        /// File path on disl: /opt/src/my_special_file.swift
        /// XCode Project hierarchy (as seen in XCOde)
        /// Sample Project
        ///     Source
        ///         External
        ///         Internal
        ///     Resources
        ///     Products
        ///
        /// In order to add the file to the External group, the project path would be `Source/External/my_special_file.swift`
        /// </param>
        /// <param name="leaveInProject">Tells us if we need to alter the project structure for this particular file or not.</param>
        /// <returns>The GUID of the file in the project.</returns>
        public static string AddFileToProject(PBXProject proj, string filePath, string projectPath = "")
        {
            string pathInProject = String.IsNullOrEmpty(projectPath) ? filePath : projectPath;

            var fileGuid = proj.FindFileGuidByProjectPath(pathInProject);
            var realPathGuid = proj.FindFileGuidByRealPath(filePath);

            if (fileGuid != null)
            {
                // File is already in project and at the right path
                if (fileGuid == realPathGuid)
                    return fileGuid;

                // It's in the project, but doesn't point to the right real path.
                // Remove it and we'll re-add it below.
                proj.RemoveFile(fileGuid);
                fileGuid = null;
            }

            proj.AddFile(filePath, pathInProject);
            fileGuid = proj.FindFileGuidByProjectPath(pathInProject);

            return fileGuid;
        }

        public static void RemoveFileFromProject(PBXProject proj, string filePath)
        {
            string fileGuid = proj.FindFileGuidByProjectPath(filePath);

            if (fileGuid != null && !String.IsNullOrEmpty(filePath))
            {
                proj.RemoveFile(fileGuid);
                fileGuid = null;
            }
        }

        /// <summary>
        /// Copy srcPath/fileName to dstPath/fileName, making directories along the way if necessary.
        /// If the destination exists, the file is _not_ copied if the destination is newer.
        /// If append is true, then "append" build logic is used -- the copy doesn't happen if the destination is newer.
        /// If symlinkInstead is true, then symlink instead of copy; dstPath -> srcPath
        /// </summary>
        public static void CopyFileTo(string fileName, string srcPath, string dstPath, bool append = false, bool symlinkInstead = false)
        {
            var srcFile = Path.Combine(srcPath, fileName);
            var dstFile = Path.Combine(dstPath, fileName);

            if (!File.Exists(srcFile))
                throw new FileNotFoundException($"CopyFileTo: {srcFile} not found");

#if UNITY_EDITOR_WIN
            // don't do symlinks on Windows
            symlinkInstead = false;
#endif

            if (File.Exists(dstFile))
            {
                if (append && File.GetCreationTime(dstFile) > File.GetCreationTime(srcFile))
                    return;

                File.Delete(dstFile);
            }

            Directory.CreateDirectory(dstPath);

            if (symlinkInstead)
            {
                // create a symlink from dstFile to srcFile using regular .NET APIs
                var relativeSrcPath = Path.GetRelativePath(dstPath, srcFile);
#if UNITY_EDITOR_WIN
                string command = $"/c mklink \"{relativeSrcPath}\" \"{Path.Combine(dstPath, fileName)}\"";
                var proc = System.Diagnostics.Process.Start("cmd.exe", command);
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                string command = $"-s \"{relativeSrcPath}\" \"{Path.Combine(dstPath, fileName)}\"";
                var proc = System.Diagnostics.Process.Start("ln", command);
#endif
                proc.WaitForExit(1000);
                if (proc.ExitCode != 0)
                {
                    throw new BuildFailedException("Failed to create symlink");
                }
            }
            else
            {
                FileUtil.CopyFileOrDirectory(srcFile, dstFile);
            }
        }

        /// <summary>
        /// Add a file to a build target in the XCode project.
        /// </summary>
        /// <param name="proj">Instance of PBXProject representing the XCode project we are modifying.</param>
        /// <param name="filePath">The path to the file to add to the project.</param>
        /// <param name="targetGuid">The build target this new file will be a part of.</param>
        /// <param name="projectPath">An optional group path for the file. The path must specify the "folder" path from the root project
        /// item and end with the file name we are adding.
        ///
        /// Example:
        ///
        /// File path on disl: /opt/src/my_special_file.swift
        /// XCode Project hierarchy (as seen in XCOde)
        /// Sample Project
        ///     Source
        ///         External
        ///         Internal
        ///     Resources
        ///     Products
        ///
        /// In order to add the file to the External group, the project path would be `Source/External/my_special_file.swift`
        /// </param>
        public static void AddFileToBuildTarget(PBXProject proj, string filePath, string targetGuid,
            string projectPath = "")
        {
            var fileGuid = AddFileToProject(proj, filePath, projectPath);
            if (!String.IsNullOrEmpty(fileGuid) && !String.IsNullOrEmpty(targetGuid))
                proj.AddFileToBuild(targetGuid, fileGuid);
        }

        /// <summary>
        /// Add specified header to the list of public resources. This will expose the header to other
        /// projects in the same workspace.
        /// </summary>
        /// <param name="proj">The XCode project to work with.</param>
        /// <param name="headerPath">The path to the header to expose. If the header isn't already a part of the project, we add it.</param>
        /// <param name="targetGuid">The guid of the target we this header will be part of.</param>
        public static void AddPublicHeaderToProject(PBXProject proj, string headerPath, string targetGuid)
        {
            var headerGuid =
                proj.FindFileGuidByProjectPath(headerPath);

            if (headerGuid == null)
            {
                proj.AddFile(headerPath, headerPath);
                headerGuid =
                    proj.FindFileGuidByProjectPath(headerPath);
            }

            proj.AddPublicHeaderToBuild(targetGuid, headerGuid);
        }

        public static void WriteTextIfChanged(string path, string text)
        {
            EnsureDirectoryExists(Path.GetDirectoryName(path));

            if (File.Exists(path))
            {
                var origText = File.ReadAllText(path, Encoding.UTF8);
                if (origText.Equals(text))
                    return;
            }

            File.WriteAllText(path, text, Encoding.UTF8);
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (String.IsNullOrEmpty(path))
                return;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void RunCommand(string cmd, string args = null, int timeoutMs = 10000)
        {
            using (var proc = Process.Start(new ProcessStartInfo()
                   {
                       FileName = cmd,
                       Arguments = args,
                       UseShellExecute = true,
                   }))
            {
                if (!proc.WaitForExit(timeoutMs))
                {
                    throw new InvalidOperationException($"Process didn't exit after {timeoutMs} args");
                }

                if (proc.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Process {cmd} {args} failed with code {proc.ExitCode}");
                }
            }
        }

        public static (bool success, string output) RunCommandWithOutput(string cmd, string args = null, string workingDirectory = null, int timeoutSec = 300, Dictionary<string, string> env = null)
        {
            StringBuilder output = new();

            void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine) => output.AppendLine(outLine.Data);

            var startInfo = new ProcessStartInfo()
            {
                FileName = cmd,
                Arguments = args,
                LoadUserProfile = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
            };

            if (env != null)
            {
                foreach (var (k,v) in env)
                {
                    startInfo.EnvironmentVariables[k] = v;
                }
            }

            using (var build = new Process() { StartInfo = startInfo })
            {
                build.OutputDataReceived += OutputDataHandler;
                build.ErrorDataReceived += OutputDataHandler;

                var success = true;

                if (!build.Start())
                    throw new Exception($"Failed to start {cmd}.");

                build.BeginOutputReadLine();
                build.BeginErrorReadLine();

                if (!build.WaitForExit(timeoutSec * 1000) || build.ExitCode != 0)
                {
                    bool timeout = !build.HasExited;
                    if (timeout)
                        build.Kill();

                    success = false;
                }

                return (success, output.ToString());
            }
        }

        static MethodInfo s_FindTargetGuidByNameMethod = null;

        public static string FindTargetGuidByName(PBXProject proj, string name)
        {
            // look up FindTargetGuidByName method via reflection on PBXProject, and call it with name
            if (s_FindTargetGuidByNameMethod == null)
            {
                s_FindTargetGuidByNameMethod = typeof(PBXProject).GetMethod("FindTargetGuidByName", BindingFlags.NonPublic | BindingFlags.Instance);
                if (s_FindTargetGuidByNameMethod == null)
                    throw new Exception("Failed to find FindTargetGuidByName method on PBXProject");
            }

            return (string)s_FindTargetGuidByNameMethod.Invoke(proj, new object[] { name });
        }

        public static void GetRuntimeFlagsForAuto(bool autoMeansEnabled, out bool runtimeEnabled, out bool runtimeLinked)
        {
            runtimeEnabled = false;
            runtimeLinked = false;

            if (PolySpatialSettings.RuntimeModeAuto)
            {
                if (!autoMeansEnabled)
                {
                    return;
                }

                runtimeLinked = true;
                runtimeEnabled = true;
            }
            else
            {
                runtimeLinked = PolySpatialSettings.RuntimeModeForceLinked;
                runtimeEnabled = PolySpatialSettings.RuntimeModeForceEnabled;
            }
        }
    }
}
#endif
