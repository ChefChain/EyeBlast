using System;
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
    /// Class that validates the material shaders of components.
    /// </summary>
    class RendererRuleCreator : IComponentRuleCreator, IPropertyValidator
    {
        const string k_ShaderGraphExtension = ".shadergraph";
        const string k_MessageFormat = "The <b>{0}</b> profile(s) do not fully support the shaders in the following materials: {1}.";
        const string k_FixtItMessage = "Select a supported shader or replace the material with one that has a supported shader.";

        static readonly HashSet<string> s_SupportedShaderNames = new();

        static RendererRuleCreator()
        {
            foreach (var shader in MaterialShaders.k_SupportedShaders)
                s_SupportedShaderNames.Add(shader);
        }

        readonly bool m_SupportShaderGraph;

        internal RendererRuleCreator(bool supportShaderGraph)
        {
            m_SupportShaderGraph = supportShaderGraph;
        }

        bool HasValidShader(Material material)
        {
            if (material == null)
                return true;

            var shader = material.shader;
            if (shader == null || s_SupportedShaderNames.Contains(shader.name))
                return true;

            if (!m_SupportShaderGraph)
                return false;

            var path = AssetDatabase.GetAssetPath(shader.GetInstanceID());
            return !string.IsNullOrEmpty(path) && path.EndsWith(k_ShaderGraphExtension);
        }

        /// <inheritdoc />
        public virtual void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var unsupportedMaterials = new List<Material>();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, $"{component.GetType().Name} Renderer"),
                Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames, ""),
                FixItAutomatic = false,
                FixItMessage = k_FixtItMessage,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
                HelpText = "Documentation",
                HelpLink = component is ParticleSystem
                    ?"https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/SupportedFeatures.html%23particle-systems"
                    : "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/SupportedFeatures.html%23rendering-components-systems",
            };

            rule.CheckPredicate = () =>
            {
                var componentToCheck = EditorUtility.InstanceIDToObject(instanceID) as Component;
                if (componentToCheck == null)
                    return true;

                Renderer renderer = null;
                if (componentToCheck is Renderer componentRenderer)
                    renderer = componentRenderer;
                else if (componentToCheck is ParticleSystem particleSystem)
                    renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

                if (renderer == null || !renderer.enabled)
                    return true;

                unsupportedMaterials.Clear();
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!HasValidShader(material))
                        unsupportedMaterials.Add(material);
                }

                var materialNames = string.Join(", ", unsupportedMaterials.Select(m => m.name));
                rule.Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames, materialNames);
                return unsupportedMaterials.Count == 0;
            };

            rule.FixIt = () =>
            {
                var componentToFix = EditorUtility.InstanceIDToObject(instanceID) as Component;
                if (componentToFix == null)
                    return;

                Selection.objects = unsupportedMaterials.ToArray();
            };

            createdRules.Add(rule);
        }

        /// <inheritdoc />
        public virtual void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(component.GetType());
            types.Add(typeof(Material));
        }
    }
}
