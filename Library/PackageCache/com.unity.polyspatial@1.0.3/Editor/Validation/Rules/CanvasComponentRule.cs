using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates if a <see cref="Canvas"/> is using world-space layout.
    /// </summary>
    class CanvasComponentRule : IComponentRuleCreator, IPropertyValidator
    {
        IPropertyValidator m_PropertyValidatorImplementation;
        const string k_MessageFormat = "The <b>{0}</b> profile(s) do not support layouts other than <b>world-space</b> for Canvases.";
        const string k_FixItMessage = "Set Canvas layout to world-space.";

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(typeof(Canvas));
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                FixItAutomatic = true,
                CheckPredicate = () =>
                {
                    var canvas = EditorUtility.InstanceIDToObject(instanceID) as Canvas;
                    return canvas == null || canvas.renderMode == RenderMode.WorldSpace;
                },
                FixIt = () =>
                {
                    var canvas = EditorUtility.InstanceIDToObject(instanceID) as Canvas;
                    if (canvas != null)
                    {
                        Undo.RecordObject(canvas,"Set Canvas layout to world-space");
                        canvas.renderMode = RenderMode.WorldSpace;
                    }

                },
                FixItMessage = k_FixItMessage,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }
    }
}
