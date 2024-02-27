using Unity.XR.CoreUtils.Editor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.VisionOS;
using UnityObject = UnityEngine.Object;

#if UNITY_HAS_URP
using System;
using UnityEngine.Rendering.Universal;
#endif

namespace UnityEditor.XR.VisionOS
{
    [InitializeOnLoad]
    static class VisionOSProjectValidation
    {
        const string k_VisionOS = "VisionOS";
        const string k_ARSessionMessageVR = "An ARSession component is required to be active in the scene.";
        const string k_ARSessionMessageMR = "An ARSession component is required to be active in the scene to provide access to ARKit features.";
        const string k_RendererDataListPropertyName = "m_RendererDataList";
        const string k_CopyDepthModePropertyName = "m_CopyDepthMode";

        const BuildTargetGroup k_VisionOSBuildTarget = BuildTargetGroup.VisionOS;

        static readonly BuildValidationRule[] k_Rules;

        static VisionOSProjectValidation()
        {
            k_Rules = new BuildValidationRule[]
            {
                new ()
                {
                    Message = "The Color Space inside Player Settings must be set to Linear.",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = () => PlayerSettings.colorSpace == ColorSpace.Linear,
                    FixIt = () => PlayerSettings.colorSpace = ColorSpace.Linear
                },

                new ()
                {
                    Message = k_ARSessionMessageVR,
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = () =>
                    {
                        var thisRule = k_Rules?[1];
                        if (thisRule != null)
                        {
                            var isVR = CheckAppMode(VisionOSSettings.AppMode.VR);
                            thisRule.Error = isVR;
                            thisRule.Message = isVR ? k_ARSessionMessageVR : k_ARSessionMessageMR;
                        }

                        return UnityObject.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include) != null;
                    },
                    FixIt = () => CreateARSession(),
                    IsRuleEnabled = VisionOSBuildProcessor.IsLoaderEnabled
                },

                new ()
                {
                    Message = "The ARSession component requires the Apple visionOS plug-in to be enabled in the XR Plug-in Management.",
                    FixItMessage = "Enable the Apple visionOS plug-in",
                    Category = k_VisionOS,
                    CheckPredicate = VisionOSBuildProcessor.IsLoaderEnabled,
                    FixIt = EnableVisionOSLoader,
                    IsRuleEnabled = () => UnityObject.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include) != null
                },

                new ()
                {
                    Message = "An ARInputManager component is required to be active in the scene.",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = () => UnityObject.FindAnyObjectByType<ARInputManager>(FindObjectsInactive.Include) != null,
                    FixIt = CreateARInputManager,
                    IsRuleEnabled = () => VisionOSBuildProcessor.IsLoaderEnabled() && CheckAppMode(VisionOSSettings.AppMode.VR)
                },
#if UNITY_HAS_URP
                new ()
                {
                    Message = "Each camera must generate a depth texture.",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = IsCamerasDepthTextureDisabled,
                    FixIt = SetCamerasDepthTextureToEnabled,
                    IsRuleEnabled = () => VisionOSBuildProcessor.IsLoaderEnabled() && CheckAppMode(VisionOSSettings.AppMode.VR)
                },
                new ()
                {
                    Message = "After Opaques is the only supported Depth Texture Mode for visionOS VR applications.",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = IsDepthTextureModeNotAfterOpaques,
                    FixIt = SetDepthTextureModeToAfterOpaques,
                    IsRuleEnabled = () => VisionOSBuildProcessor.IsLoaderEnabled() && CheckAppMode(VisionOSSettings.AppMode.VR)
                },
#endif
                new ()
                {
                    Message = "Hand Tracking Usage Description (in the Apple visionOS settings) is required to automatically start the Hands subsystem.",
                    FixItMessage = "Update the Hand Tracking Usage Description",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = () => VisionOSSettings.currentSettings == null ||
                                           !string.IsNullOrEmpty(VisionOSSettings.currentSettings.handsTrackingUsageDescription),
                    FixItAutomatic = false,
                    FixIt = () => SettingsService.OpenProjectSettings("Project/XR Plug-in Management/Apple visionOS"),
                    IsRuleEnabled = VisionOSBuildProcessor.IsLoaderEnabled
                },

                new ()
                {
                    Message = "World Sensing features (images, planes or meshes) are disabled. If your app uses world sensing, you need to add a " +
                              "World Sensing Usage Description in the Apple visionOS settings.",
                    FixItMessage = "Update the World Sensing Usage Description",
                    Category = k_VisionOS,
                    Error = false,
                    CheckPredicate = () => VisionOSSettings.currentSettings == null ||
                                           !string.IsNullOrEmpty(VisionOSSettings.currentSettings.worldSensingUsageDescription),
                    FixItAutomatic = false,
                    FixIt = () => SettingsService.OpenProjectSettings("Project/XR Plug-in Management/Apple visionOS"),
                    IsRuleEnabled = VisionOSBuildProcessor.IsLoaderEnabled
                },
                new ()
                {
                    Message = "Splash screen is not yet supported for visionOS. If the splash screen is enabled, you may have errors when building or when " +
                              "running your application in the simulator or in the device.",
                    FixItMessage = "Disable the splash screen",
                    Category = k_VisionOS,
                    Error = true,
                    CheckPredicate = () =>  !PlayerSettings.SplashScreen.show,
                    FixIt = () => PlayerSettings.SplashScreen.show = false
                },
            };

            BuildValidator.AddRules(k_VisionOSBuildTarget, k_Rules);
        }

        static bool CheckAppMode(VisionOSSettings.AppMode mode)
        {
            return VisionOSSettings.currentSettings != null && VisionOSSettings.currentSettings.appMode == mode;
        }

        static GameObject CreateARSession()
        {
            var arSession = UnityObject.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
            if (arSession != null)
                return arSession.gameObject;

            var newARSession = new GameObject("AR Session");
            newARSession.AddComponent<ARSession>();
            Undo.RegisterCreatedObjectUndo(newARSession, "Create AR Session");
            return newARSession;
        }

        static void CreateARInputManager()
        {
            var arSession = CreateARSession();
            Undo.AddComponent(arSession, typeof(ARInputManager));
        }

#if UNITY_HAS_URP
        static void SetCamerasDepthTextureToEnabled()
        {
            if (UniversalRenderPipeline.asset == null)
                return;

            var cameras = UnityObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                var cameraData = camera.GetUniversalAdditionalCameraData();
                if (cameraData != null && !cameraData.requiresDepthTexture)
                {
                    Undo.RegisterCompleteObjectUndo(cameraData, "Enable Depth Texture");
                    cameraData.requiresDepthTexture = true;
                }
            }
        }

        static bool IsCamerasDepthTextureDisabled()
        {
            // Passes validation if no asset is set.
            if (UniversalRenderPipeline.asset == null)
                return true;

            var cameras = UnityObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                var cameraData = camera.GetUniversalAdditionalCameraData();
                if (cameraData != null && !cameraData.requiresDepthTexture)
                    return false;
            }

            return true;
        }

        static void ForEachRendererData(UniversalRenderPipelineAsset asset, Action<UnityObject> action)
        {
            // NB: Copy Depth Mode is not exposed on UniversalRenderer, so we need to snoop SerializedProperties to check for UniversalRendererData references.
            var serializedObject = new SerializedObject(asset);
            var rendererDataList = serializedObject.FindProperty(k_RendererDataListPropertyName);
            if (rendererDataList == null)
                return;

            var count = rendererDataList.arraySize;
            for (var i = 0; i < count; i++)
            {
                var rendererDataListElement = rendererDataList.GetArrayElementAtIndex(i);
                var rendererData = rendererDataListElement.objectReferenceValue;
                if (rendererData == null)
                    continue;

                action.Invoke(rendererData);
            }
        }

        static SerializedProperty GetCopyDepthModeProperty(UnityObject rendererData)
        {
            var serializedObject = new SerializedObject(rendererData);
            return serializedObject.FindProperty(k_CopyDepthModePropertyName);
        }

        static void SetDepthTextureModeToAfterOpaques()
        {
            var asset = UniversalRenderPipeline.asset;
            if (asset == null)
                return;

            ForEachRendererData(asset, rendererData =>
            {
                var copyDepthModeProperty = GetCopyDepthModeProperty(rendererData);
                if (copyDepthModeProperty == null)
                    return;

                copyDepthModeProperty.intValue = (int)CopyDepthMode.AfterOpaques;
                copyDepthModeProperty.serializedObject.ApplyModifiedProperties();
            });
        }


        static bool IsDepthTextureModeNotAfterOpaques()
        {
            // Passes validation if no asset is set.
            var asset = UniversalRenderPipeline.asset;
            if (asset == null)
                return true;

            var foundInvalidRenderer = false;
            ForEachRendererData(asset, rendererData =>
            {
                var copyDepthModeProperty = GetCopyDepthModeProperty(rendererData);
                if (copyDepthModeProperty == null)
                    return;

                if (copyDepthModeProperty.intValue != (int)CopyDepthMode.AfterOpaques)
                    foundInvalidRenderer = true; // If any renderer's copy depth mode is not set to AfterOpaques, rendering issues can occur on visionOS hardware.
            });

            return !foundInvalidRenderer;
        }
#endif

        static void EnableVisionOSLoader()
        {
            var visionOSLoaderGUIDs = AssetDatabase.FindAssets($"t:{nameof(VisionOSLoader)}");
            if (visionOSLoaderGUIDs.Length == 0)
                return;

            var visionOSLoader = AssetDatabase.LoadAssetAtPath<VisionOSLoader>(AssetDatabase.GUIDToAssetPath(visionOSLoaderGUIDs[0]));
            if (visionOSLoader == null)
                return;

            var visionOSXRSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                BuildPipeline.GetBuildTargetGroup(BuildTarget.VisionOS));

            if (visionOSXRSettings == null)
                return;

            var manager = visionOSXRSettings.Manager;
            if (manager == null)
                return;

            if (manager.TryAddLoader(visionOSLoader))
            {
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssetIfDirty(manager);
            }
        }
    }
}
