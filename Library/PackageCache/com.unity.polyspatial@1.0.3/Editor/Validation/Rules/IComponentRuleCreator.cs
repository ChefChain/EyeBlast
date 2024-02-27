using System.Collections.Generic;
using Unity.XR.CoreUtils.Editor;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// An interface that represents a creator of validation rules for components.
    /// </summary>
    /// <seealso cref="IPropertyValidator"/>
    internal interface IComponentRuleCreator
    {
        /// <summary>
        /// Creates validation rules for the given component.
        /// This method is automatically called by Unity when a component of the registered type is detected in the
        /// loaded scenes.
        /// </summary>
        /// <param name="component">The component to create validation rules. Only components in loaded scenes are used.</param>
        /// <param name="createdRules">Returns the newly created validation rules.</param>
        void CreateRules(Component component, List<BuildValidationRule> createdRules);
    }
}
