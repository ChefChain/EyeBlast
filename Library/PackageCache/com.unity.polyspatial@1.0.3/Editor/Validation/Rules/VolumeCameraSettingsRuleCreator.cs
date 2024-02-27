using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.PolySpatial;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates if a <see cref="VolumeCamera"/>'s <see cref="VolumeCameraConfiguration"/> is inside of a Resources folder.
    /// </summary>
    class VolumeCameraSettingsRuleCreator : IComponentRuleCreator, IPropertyValidator
    {
        IPropertyValidator m_PropertyValidatorImplementation;

        const string k_Message = "The <b>VolumeCameraConfiguration</b> asset being used is not inside of a <b>Resources</b> folder. For this <b>VolumeCameraConfiguration</b> asset to be available at runtime, it must be inside of a <b>Resources</b> folder.";
        const string k_FixItMessage = "Move the <b>Volume Camera Configuration</b> asset into a <b>Resources</b> folder.";

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(typeof(VolumeCamera));
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = k_Message,
                FixItAutomatic = true,
                CheckPredicate = () =>
                {
                    var volumeCamera = EditorUtility.InstanceIDToObject(instanceID) as VolumeCamera;
                    if (volumeCamera == null)
                        return true;

                    var configuration = volumeCamera.WindowConfiguration;
                    return configuration == null || AssetDatabase.GetAssetPath(configuration).Contains("/Resources/");
                },
                FixIt = () =>
                {
                    var volumeCamera = EditorUtility.InstanceIDToObject(instanceID) as VolumeCamera;
                    if (volumeCamera == null)
                        return;

                    var path = AssetDatabase.GetAssetPath(volumeCamera.WindowConfiguration);
                    if (string.IsNullOrEmpty(path) || path.Contains("/Resources/"))
                        return;

                    var fileName = Path.GetFileName(path);
                    var directory = Path.GetDirectoryName(path);
                    if (!AssetDatabase.IsValidFolder($"{directory}/Resources"))
                        AssetDatabase.CreateFolder(directory, "Resources");

                    var newPath = $"{directory}/Resources/{fileName}";
                    AssetDatabase.MoveAsset(path, newPath);
                },
                FixItMessage = k_FixItMessage,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }
    }
}
