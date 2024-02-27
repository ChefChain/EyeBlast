#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

using PackageManagerClient = UnityEditor.PackageManager.Client;

namespace UnityEngine.XR.VisionOS.Internal
{
    [InitializeOnLoad]
    internal static class LicenseCheck
    {
        static string k_LicenseCheckTitle = "Pro License Required";
        static string k_LicenseCheckMessage = "Unity PolySpatial and visionOS XR support is only available to Unity Pro, Unity Enterprise, and Unity Industry users. These packages will be uninstalled. Learn more about Unity plans at unity.com/pricing.";
        static string k_OkButton = "Ok";
        static string k_SeePricingButton = "Learn about a 30-day trial";
        static string k_PricingUrl = "https://unity.com/pages/pro-free-trial";

        static string[] k_PackagesToRemove = new[]{
            "com.unity.xr.visionos",
        };

        class BuildCheck : IPreprocessBuildWithReport
        {
            public int callbackOrder => -1;

            public void OnPreprocessBuild(BuildReport report)
            {
                CheckLicenseRequirements(true);
            }
        }

        static LicenseCheck()
        {
            CheckLicenseRequirements();
        }

        static AddAndRemoveRequest s_AddAndRemoveRequest;

        internal static void CheckLicenseRequirements(bool isBuilding = false)
        {
            if (UnityEditorInternal.InternalEditorUtility.HasPro())
            {
                return;
            }

            if (isBuilding)
            {
                throw new BuildFailedException(k_LicenseCheckMessage);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(-1);
                // Exit is not as immediate as it implies. It will exit somewhere after this function returns
                // so we have to guard against trying to display UI or do any work.
                throw new Exception(k_LicenseCheckMessage);
            }

            // If they want to see pricing then we need to send them to the Unity page for that. The pricing button is
            // actually the "cancel" button and will return false if selected.
            var wantsToSeePricing = !EditorUtility.DisplayDialog(
                k_LicenseCheckTitle,
                k_LicenseCheckMessage,
                k_OkButton,
                k_SeePricingButton);
            if (wantsToSeePricing)
            {
                Application.OpenURL(k_PricingUrl);
            }

            // Log an error as a reminder of what happened and why.
            Debug.LogError(k_LicenseCheckMessage);

            var addAndRemove = PackageManagerClient.AddAndRemove(null, k_PackagesToRemove);
            if (addAndRemove.Status == StatusCode.Failure)
            {
                Debug.LogError($"Failed to remove packages: {addAndRemove.Error.message}");
                return;
            }
            s_AddAndRemoveRequest = addAndRemove;

            EditorApplication.update += OnEditorUpdatePackagesRemove;
        }

        // Simple async handler that allows us to wait for the package removal to complete
        // and log any errors from the attempt if it fails.
        static void OnEditorUpdatePackagesRemove()
        {
            EditorApplication.update -= OnEditorUpdatePackagesRemove;

            if (!s_AddAndRemoveRequest.IsCompleted)
            {
                EditorApplication.update += OnEditorUpdatePackagesRemove;
                return;
            }

            if (s_AddAndRemoveRequest.Status == StatusCode.Failure)
            {
                Debug.LogError($"Failed to remove packages: {s_AddAndRemoveRequest.Error.message}");
            }
        }
    }
}
#endif
