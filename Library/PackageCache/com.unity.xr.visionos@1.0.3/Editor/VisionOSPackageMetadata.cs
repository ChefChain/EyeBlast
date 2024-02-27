using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.VisionOS;

namespace UnityEditor.XR.VisionOS
{
    class XRPackage : IXRPackage
    {
        class VisionOSLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class VisionOSPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }

        static IXRPackageMetadata s_Metadata = new VisionOSPackageMetadata()
        {
            packageName = VisionOSPackageInfo.displayName,
            packageId = VisionOSPackageInfo.identifier,
            settingsType = typeof(VisionOSSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new VisionOSLoaderMetadata()
                {
                    loaderName = "Apple visionOS",
                    loaderType = typeof(VisionOSLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>()
                    {
                        BuildTargetGroup.VisionOS,
#if UNITY_VISIONOS_MAC_STUB
                        BuildTargetGroup.Standalone
#endif
                    }
                },
            }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            if (obj is VisionOSSettings settings)
            {
                VisionOSSettings.currentSettings = settings;
                return true;
            }

            return false;
        }
    }
}
