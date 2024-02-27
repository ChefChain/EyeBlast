using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates if a <see cref="SkinnedMeshRenderer"/>s is not using optimized skinned models (LXR-600).
    /// </summary>
    class SkinnedMeshRendererRuleCreator : RendererRuleCreator
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) do not support <b>Optimized skinned mesh renderers</b>.";

        internal SkinnedMeshRendererRuleCreator() : base(true)
        {
        }

        /// <inheritdoc />
        public override void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            base.CreateRules(component, createdRules);

            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                FixItAutomatic = false,
                CheckPredicate = () =>
                {
                    var renderer = EditorUtility.InstanceIDToObject(instanceID) as SkinnedMeshRenderer;
                    return renderer == null || (renderer.bones.Length != 0 && renderer.rootBone != null);
                },
                SceneOnlyValidation = true,
                Error = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }
    }
}
