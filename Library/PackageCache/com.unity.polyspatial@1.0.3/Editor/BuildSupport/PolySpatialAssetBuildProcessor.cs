using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.PolySpatial.Internals;
using UnityEditor.Build;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.PolySpatial.Internals
{
    // PolySpatialAssetProcessor will find all assets used in the Build (using ContentBuildInterface APIs and
    // Resources.LoadAll) which have PolySpatialAssetData, and will tell the build pipeline to add the PolySpatialAssetData
    // to StreamingAssets (using BuildPlayerContext.AddAdditionalPathToStreamingAssets).
    //
    // Also, it will create a mapping table for each scene which maps object references to PolySpatial Asset paths, so
    // we can map objects to their PolySpatialAssetData at runtime. This mapping table will be added to a new GameObject in each
    // serialized scene during the build.
    class PolySpatialAssetBuildProcessor : BuildPlayerProcessor, IProcessSceneWithReport
    {
        public override int callbackOrder => 0;

        static private Dictionary<string, Dictionary<string, string>> s_SceneAssetLocators;
        static private Dictionary<string, string> s_AssetNameLocators;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    throw new Exception("User aborted.");
            }

            var openedScenePath = SceneManager.GetActiveScene().path;
            try
            {
                PrepareForBuildInner(buildPlayerContext);
            }
            finally
            {
                if (!string.IsNullOrEmpty(openedScenePath))
                {
                    EditorSceneManager.OpenScene(openedScenePath);
                }
            }
        }

        void PrepareForBuildInner(BuildPlayerContext buildPlayerContext)
        {
            s_SceneAssetLocators = new();
            s_AssetNameLocators = new();

            bool firstScene = true;
            foreach (var scene in buildPlayerContext.BuildPlayerOptions.scenes)
            {
                Dictionary<string, string> locators = new();
                var sceneDeps = ContentBuildInterface.CalculatePlayerDependenciesForScene(scene, new(), new());
                foreach (var obj in sceneDeps.referencedObjects)
                {
                    CollectAssetReference(buildPlayerContext, obj.guid.ToString(), locators);
                }

                if (firstScene)
                {
                    // Add manager and resource dependencies to the first scene, so that the object mapping for those
                    // becomes available right from the beginning.
                    var managerDeps = ContentBuildInterface.CalculatePlayerDependenciesForGameManagers(
                        new(), new(), new());
                    foreach (var obj in managerDeps.referencedObjects)
                    {
                        CollectAssetReference(buildPlayerContext, obj.guid.ToString(), locators);
                    }

                    foreach (var resourcePath in new[] { "", "Packages/com.unity.polyspatial/Resources" })
                    {
                        var resources = Resources.LoadAll(resourcePath);
                        foreach (var obj in resources)
                        {
                            var objPath = AssetDatabase.GetAssetOrScenePath(obj);
                            foreach (var dependencyPath in AssetDatabase.GetDependencies(objPath, true))
                            {
                                var guid = AssetDatabase.AssetPathToGUID(dependencyPath);
                                CollectAssetReference(buildPlayerContext, guid, locators);
                            }
                        }
                    }

                    // Find and include all shader graph artifacts and store a mapping by name.  This is a temporary
                    // measure to allow using shader graphs referenced in asset bundles or included after scene
                    // processing.
                    // TODO (LXR-1716): Remove this when we have a better solution for the above cases.
                    foreach (var guid in AssetDatabase.FindAssets("t:shader"))
                    {
                        if (EditorPolySpatialAssetProvider.GetPathsForAsset(guid).Length == 0)
                            continue;

                        CollectAssetReference(buildPlayerContext, guid, locators);
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var name = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)).name;
                        s_AssetNameLocators[name] = locators[guid];
                    }

                    firstScene = false;
                }

                s_SceneAssetLocators[scene] = locators;
            }
        }

        private static void CollectAssetReference(
            BuildPlayerContext buildPlayerContext, string guid, Dictionary<string, string> locators)
        {
            if (locators.ContainsKey(guid))
                return;

            var paths = EditorPolySpatialAssetProvider.GetPathsForAsset(guid);
            if (paths.Length == 0)
                return;

            locators.Add(
                guid,
                // Unity stores secondary artifacts for Assets in the Library folder. All secondary artifacts for an asset
                // are stored next to each other, with the artifact key as an extension. So, we just store the base path
                // without the extension here, which will let us find all the artifacts with a wildcard search. We don't use
                // Path.GetFileNameWithoutExtension, as there are usually multiple extensions
                // (ie: [file hash].[key].polyspatialasset).
                paths[0].Substring(0, paths[0].IndexOf('.'))
            );

            foreach (var p in paths)
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(Path.GetFullPath(p),
                    Path.Combine("PolySpatialAssets", p));
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // If s_SceneAssetLocators wasn't initialized, we are building an asset bundle.
            if (s_SceneAssetLocators == null)
                return;

            var locators = s_SceneAssetLocators[scene.path];
            var qam = new GameObject().AddComponent<PolySpatialSceneAssetMap>();
            qam.m_AssetGUIDMap = new(locators.Select(entry =>
                new PolySpatialSceneAssetMap.AssetGUIDMapEntry()
                {
                    locator = entry.Value,
                    obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(entry.Key))
                }
            ).Where(agme => agme.obj != null));

            if (scene.buildIndex == 0)
            {
                qam.m_AssetNameMap = new(s_AssetNameLocators.Select(entry =>
                    new PolySpatialSceneAssetMap.AssetNameMapEntry()
                {
                    name = entry.Key,
                    locator = entry.Value,
                }));
            }
        }
    }
}
