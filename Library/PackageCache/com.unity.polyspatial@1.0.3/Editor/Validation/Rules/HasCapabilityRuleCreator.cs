using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules to check if any selected <see cref="CapabilityProfile"/> in <see cref="CapabilityProfileSelection"/> have a
    /// specific capability.
    /// </summary>
    /// <seealso cref="StandardCapabilityKeys"/>
    internal sealed class HasCapabilityRuleCreator : IComponentRuleCreator
    {
        const string k_MessageFormat = "The {0} profile(s) don't support the <b>\"{1}\"</b> capability and the <b>{2}</b> component will not work properly.";
        const string k_RemoveMessageFormat = "Remove the <b>{0}</b> component.";
        const string k_DisableMessageFormat = "Disable the <b>{0}</b> component.";

        // EditorUtility.GetObjectEnabled returns -1 for objects that don't show the enabled toggle in the InspectorTitleBar
        const int k_HiddenEnabledToggle = -1;

        static readonly List<CapabilityProfile> s_Profiles = new List<CapabilityProfile>();

        readonly string k_CapabilityKey;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="capabilityKey">The capability key to validate.</param>
        /// <exception cref="ArgumentException">Throws when the <paramref name="capabilityKey"/> parameter is <see langword="null"/> or empty.</exception>
        public HasCapabilityRuleCreator(string capabilityKey)
        {
            if (string.IsNullOrEmpty(capabilityKey))
                throw new ArgumentException($"{nameof(capabilityKey)} cannot be null or empty.");

            k_CapabilityKey = capabilityKey;
        }

        void GetProfilesWithoutCapability(List<CapabilityProfile> profilesWithoutCapability)
        {
            if (CapabilityProfileSelection.Selected.Count == 0)
                return;

            foreach (var selectedProfile in CapabilityProfileSelection.Selected)
            {
                if (selectedProfile == null || selectedProfile is not ICapabilityModifier capabilityModifier)
                    continue;

                if (!capabilityModifier.TryGetCapabilityValue(k_CapabilityKey, out var capabilityValue) || !capabilityValue)
                    profilesWithoutCapability.Add(selectedProfile);
            }
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var componentType = component.GetType();
            var instanceID = component.GetInstanceID();

            var rule = new BuildValidationRule
            {
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, $"{componentType.Name} Capability"),
                Message = string.Format(k_MessageFormat, "", k_CapabilityKey, componentType.Name),
                FixIt = () =>
                {
                    var componentToFix = EditorUtility.InstanceIDToObject(instanceID) as Component;
                    if (componentToFix == null)
                        return;

                    if (EditorUtility.GetObjectEnabled(component) == k_HiddenEnabledToggle)
                    {
                        RemoveComponentUtils.RemoveComponentAndDependencies(componentToFix);
                    }
                    else
                    {
                        Undo.RecordObject(componentToFix, "Fix Component");
                        EditorUtility.SetObjectEnabled(componentToFix, false);
                    }
                },
                FixItMessage = EditorUtility.GetObjectEnabled(component) == k_HiddenEnabledToggle ?
                    string.Format(k_RemoveMessageFormat, componentType.Name) :
                    string.Format(k_DisableMessageFormat, componentType.Name),
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            rule.CheckPredicate = () =>
            {
                var componentToCheck = EditorUtility.InstanceIDToObject(instanceID);
                if (componentToCheck == null || EditorUtility.GetObjectEnabled(componentToCheck) == 0)
                    return true;

                s_Profiles.Clear();
                GetProfilesWithoutCapability(s_Profiles);
                if (s_Profiles.Count == 0)
                    return true;

                var profileNames = string.Join(", ", s_Profiles.Select(p => p.name));
                rule.Message = string.Format(k_MessageFormat, profileNames, k_CapabilityKey, componentToCheck.GetType().Name);
                return false;
            };

            createdRules.Add(rule);
        }
    }
}
