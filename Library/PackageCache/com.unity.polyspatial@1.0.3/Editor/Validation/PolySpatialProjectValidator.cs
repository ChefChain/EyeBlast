using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules for the PolySpatial project.
    /// </summary>
    [InitializeOnLoad]
    static class PolySpatialProjectValidator
    {
        static PolySpatialProjectValidator()
        {
            // Delay the initialization to allow AssetDatabase.FindAssets to work properly in a clean checkout (LXR-2335)
            EditorApplication.delayCall += Initialize;
        }

        static void Initialize()
        {
            if(PolySpatialSettings.instance.PolySpatialValidationOption == PolySpatialSettings.ValidationOption.Disabled)
                return;

            var rules = new List<BuildValidationRule>();
            rules.Add(CreateCollisionMatrixRule());

            if (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.VisionOS &&
                PolySpatialSettings.instance.PolySpatialValidationOption == PolySpatialSettings.ValidationOption.EnabledForAllPlatforms)
            {
                BuildValidator.AddRules(EditorUserBuildSettings.selectedBuildTargetGroup, rules);
            }

            BuildValidator.AddRules(BuildTargetGroup.VisionOS, rules);
        }

        static BuildValidationRule CreateCollisionMatrixRule()
        {
            return new BuildValidationRule()
            {
                IsRuleEnabled = () => LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName) != -1 && CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, "LayerCollision"),
                Message = $"The <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer should not collide with any layer.",
                CheckPredicate = () =>
                {
                    var polyspatialLayer = LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName);
                    if (polyspatialLayer == -1)
                        return false;

                    for (var layer = 0; layer < 32; layer++)
                    {
                        var layerName = LayerMask.LayerToName(layer);
                        if (string.IsNullOrEmpty(layerName))
                            continue;

                        if(!Physics.GetIgnoreLayerCollision(polyspatialLayer, layer))
                            return false;
                    }

                    return true;
                },
                FixIt = () =>
                {
                    var polySpatialLayer = LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName);
                    if (polySpatialLayer == -1)
                        return;

                    PolySpatialUnityBackend.IgnoreLayerCollision(polySpatialLayer);
                },
                FixItMessage = $"Disable any physics layer collision with the <b>{VolumeCamera.PolySpatialLayerName}</b> layer."
            };
        }
    }
}
