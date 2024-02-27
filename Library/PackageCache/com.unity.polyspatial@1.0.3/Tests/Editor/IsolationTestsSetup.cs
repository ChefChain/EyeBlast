using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// This class and method are used to enable additional tests from Tests.Isolation.IsolationTests fixture that are
    /// gated behind INCLUDE_ISOLATION_TESTS define symbol. The static method is expected to get invoked from test runs, either
    /// via upm-ci or UTR, passing in UTR option: "-executeMethod Tests.Editor.IsolationTestSetup.AddScriptingDefineSymbol".
    /// See usage reference here: https://docs.unity3d.com/2022.2/Documentation/Manual/EditorCommandLineArguments.html
    /// </summary>
    class IsolationTestsSetup
    {
        private static readonly string k_IsolationTestsDefineSymbol = "INCLUDE_ISOLATION_TESTS";

        public static void AddScriptingDefineSymbol()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var allDefines = scriptingDefineSymbols.Split(';').ToList();
            allDefines.Add(k_IsolationTestsDefineSymbol);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", allDefines));
        }
    }
}
