using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial.Internals;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules for game objects that have been set to the PolySpatial layer.
    /// </summary>
    static class PolySpatialLayerRule
    {
        /// <summary>
        /// Message for objects breaking the polyspatial layer rule.
        /// </summary>
        const string k_MessageFormat = "<b>\"{0}\"</b> is currently set to the PolySpatial layer. This will cause the object to be invisible/deactivated.";

        /// <summary>
        /// Message for objects to fix their breaking of the polyspatial layer rule.
        /// </summary>
        const string k_FixItMessageFormat = "Move <b>\"{0}\"</b> to the Default layer.";

        /// <summary>
        /// This rule category.
        /// </summary>
        const string k_Category = "ObjectLayer";
        static void UpdateRuleMessages(BuildValidationRule rule, string gameObjectName)
        {
            rule.Message = string.Format(k_MessageFormat, gameObjectName);
            rule.FixItMessage = string.Format(k_FixItMessageFormat, gameObjectName);
        }

        internal static void CreateRules(GameObject gameObject, List<BuildValidationRule> ruleList)
        {
            var instanceId = gameObject.GetInstanceID();

            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, k_Category),
                FixIt = () =>
                {
                    var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                    if (go == null)
                        return;

                    Undo.RecordObject(go, "Remove from PolySpatial layer");
                    go.layer = 0;

                },
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceId),
            };

            rule.CheckPredicate = () =>
            {
                // need to check the layer name every time, as it might have changed by the users
                var polySpatialLayer = LayerMask.NameToLayer(PolySpatialUnityBackend.PolySpatialLayerName);

                // is there a PolySpatial layer at all?
                if (polySpatialLayer == -1)
                    return true;

                var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                if (go == null)
                    return true;

                // Update the rule messages to keep it synced with the game object name.
                UpdateRuleMessages(rule, go.name);
                return go.layer != polySpatialLayer;
            };

            UpdateRuleMessages(rule, gameObject.name);

            ruleList.Add(rule);
        }

    }
}
