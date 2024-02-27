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
    /// Warning about non-uniform collider scales.
    /// </summary>
    class ColliderNonUniformScaleRule : IComponentRuleCreator, IPropertyValidator
    {
        IPropertyValidator m_PropertyValidatorImplementation;

        const string k_MessageFormatOffsetError = "The <b>{0}</b> profile(s) does not support Collider Offsets on a GameObject with non-uniform scale.";
        const string k_MessageFormatScaleError = "The <b>{0}</b> profile(s) does not support a Sphere or Capsule Collider on a GameObject with non-uniform scale.";
        const string k_MessageFormatChildScaleError = "The <b>{0}</b> profile(s) does not support a Collider on a GameObject which is a " +
                                                      "child of another GameObject with non-uniform scale.";

        const string k_FixItScaleErrorMessage = "Set Transform Scale Uniform.";
        const string k_FixItScaleChildErrorMessage = "Unchild GameObject.";

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(typeof(BoxCollider));
            types.Add(typeof(CapsuleCollider));
            types.Add(typeof(SphereCollider));
            types.Add(typeof(MeshCollider));
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();

            var offsetError = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormatOffsetError, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                FixItAutomatic = false,
                CheckPredicate = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return true;

                    if (IsOffsetZero(collider) ||
                        // fix non-uniform parent scale first if that is an issue
                        (collider.transform.parent != null && !IsScaleUniform(collider.transform.parent.lossyScale)))
                        return true;

                    return IsScaleUniform(collider.transform.localScale);
                },
                FixIt = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return;

                    SetTransformScaleUniform(collider.transform);
                },
                FixItMessage = k_FixItScaleErrorMessage,
                SceneOnlyValidation = false,
                OnClick = () => BuildValidator.SelectObject(instanceID),
                Error = true,
            };

            var nonUniformScaleError = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormatScaleError, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                FixItAutomatic = false,
                CheckPredicate = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return true;

                    return IsScaleUniform(component.transform.localScale);
                },
                FixIt = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return;

                    SetTransformScaleUniform(collider.transform);
                },
                FixItMessage = k_FixItScaleErrorMessage,
                SceneOnlyValidation = false,
                OnClick = () => BuildValidator.SelectObject(instanceID),
                Error = true,
            };

            var nonUniformChildScaleError = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormatChildScaleError, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                FixItAutomatic = false,
                CheckPredicate = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return true;

                    if (collider.transform.parent == null)
                        return true;

                    return IsScaleUniform(collider.transform.parent.lossyScale);
                },
                FixIt = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return;

                    collider.transform.SetParent(null, true);
                },
                FixItMessage = k_FixItScaleChildErrorMessage,
                SceneOnlyValidation = false,
                OnClick = () => BuildValidator.SelectObject(instanceID),
                Error = true,
            };

            switch (component)
            {
                case BoxCollider boxCollider:
                case MeshCollider meshCollider:
                    createdRules.Add(offsetError);
                    createdRules.Add(nonUniformChildScaleError);
                    break;
                case CapsuleCollider capsuleCollider:
                case SphereCollider sphereCollider:
                    createdRules.Add(nonUniformScaleError);
                    createdRules.Add(nonUniformChildScaleError);
                    break;
            }
        }

        static bool IsOffsetZero(Collider collider)
        {
            switch (collider)
            {
                case BoxCollider boxCollider:
                    if (boxCollider.center == Vector3.zero)
                        return true;
                    break;
                case CapsuleCollider capsuleCollider:
                    if (capsuleCollider.center == Vector3.zero)
                        return true;
                    break;
                case SphereCollider sphereCollider:
                    if (sphereCollider.center == Vector3.zero)
                        return true;
                    break;
            }

            return false;
        }

        static bool IsScaleUniform(Vector3 scale)
        {
            var xyUniform = Mathf.Approximately(scale.x, scale.y);
            var yzUniform = Mathf.Approximately(scale.y, scale.z);
            return xyUniform && yzUniform;
        }

        static void SetColliderOffsetToZero(Collider collider)
        {
            Undo.RecordObject(collider, "Set Collider Offset to Zero");
            switch (collider)
            {
                case BoxCollider boxCollider:
                    boxCollider.center = Vector3.zero;
                    break;
                case CapsuleCollider capsuleCollider:
                    capsuleCollider.center = Vector3.zero;
                    break;
                case SphereCollider sphereCollider:
                    sphereCollider.center = Vector3.zero;
                    break;
            }
        }

        static void SetTransformScaleUniform(Transform transform)
        {
            Undo.RecordObject(transform, "Set Transform Scale Uniform");
            var localScale = transform.localScale;
            var maxValue = Mathf.Max(localScale.x, localScale.y, localScale.z);
            maxValue = (float)Math.Round(maxValue, 4);
            transform.localScale = new Vector3(maxValue, maxValue, maxValue);
        }
    }
}
