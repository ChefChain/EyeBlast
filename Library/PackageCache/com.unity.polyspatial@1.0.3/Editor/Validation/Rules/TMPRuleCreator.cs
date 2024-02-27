using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates the font material of TextMeshPro and TextMeshProGUI components.
    /// </summary>
    class TMPRuleCreator : IComponentRuleCreator, IPropertyValidator
    {
        const string k_MaterialMessageFormat = "The <b>{0}</b> profile(s) do not support the following TextMeshPro material properties: {1}.";
        const string k_ShaderMessageFormat = "The <b>{0}</b> profile(s) do not support the following shader: {1}. Please use the {2} shader instead.";
        const string k_FixItMessage = "Select a different material or edit your current material to remove the unsupported options/shader.";

        const string k_TMPValidShader = "TextMeshPro/Mobile/Distance Field";
        readonly (string shaderName, string displayName) m_OutlineKeyword = ("OUTLINE_ON", "Outline");
        readonly (string shaderName, string displayName) m_UnderlayKeyword = ("UNDERLAY_ON","Underlay");
        readonly (string shaderName, string displayName) m_FaceDilate = ("_FaceDilate","Face Dilate");
        //The outline softness is actually the face softness in the inspector.
        readonly (string shaderName, string displayName) m_OutlineSoftness = ("_OutlineSoftness","Face Softness");

        string m_UnsupportedProperties = "";

        readonly int k_Dilate;
        readonly int k_Softness;

        internal TMPRuleCreator()
        {
            k_Dilate = Shader.PropertyToID(m_FaceDilate.shaderName);
            k_Softness = Shader.PropertyToID(m_OutlineSoftness.shaderName);
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                FixItAutomatic = false,
                FixIt = () =>
                {
                    var tmpComponent = EditorUtility.InstanceIDToObject(instanceID) as TextMeshProUGUI;
                    if (tmpComponent == null)
                        return;

                    Selection.activeObject = tmpComponent.fontSharedMaterial;
                },
                FixItMessage = k_FixItMessage,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            rule.CheckPredicate = () =>
            {
                var tmpComponent = EditorUtility.InstanceIDToObject(instanceID) as TMP_Text;
                return tmpComponent == null || IsValidMaterial(tmpComponent.fontSharedMaterial, rule);
            };

            createdRules.Add(rule);
        }

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(component.GetType());
            types.Add(typeof(Material));
        }

        bool IsValidMaterial(Material material, BuildValidationRule rule)
        {
            if (material == null)
                return true;

            if (material.shader.name != k_TMPValidShader)
            {
                rule.Message = string.Format(k_ShaderMessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames, material.shader.name, k_TMPValidShader);
                return false;
            }

            m_UnsupportedProperties = string.Empty;

            if (material.IsKeywordEnabled(m_OutlineKeyword.shaderName))
                m_UnsupportedProperties += m_OutlineKeyword.displayName + ", ";
            if (material.IsKeywordEnabled(m_UnderlayKeyword.shaderName))
                m_UnsupportedProperties += m_UnderlayKeyword.displayName + ", ";
            if (material.GetFloat(k_Dilate) != 0)
                m_UnsupportedProperties += m_FaceDilate.displayName + ", ";
            if (material.GetFloat(k_Softness) != 0)
                m_UnsupportedProperties += m_OutlineSoftness.displayName + ", ";

            if (m_UnsupportedProperties.Length >= 2 && m_UnsupportedProperties[m_UnsupportedProperties.Length- 2] == ',')
                m_UnsupportedProperties = m_UnsupportedProperties.Remove(m_UnsupportedProperties.Length - 2, 2);

            rule.Message = string.Format(k_MaterialMessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames, m_UnsupportedProperties);

            return string.IsNullOrEmpty(m_UnsupportedProperties);
        }
    }
}
