using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Optional interface that an <see cref="IComponentRuleCreator"/> can implement to revalidate issues when a Unity object property is changed.
    /// </summary>
    interface IPropertyValidator
    {
        /// <summary>
        /// Get the types to track changes, any serialized property change in the instances of the returned <paramref name="types"/> will trigger an issues
        /// revalidation.
        /// This method is automatically called by Unity when a new <see cref="IComponentRuleCreator"/> target component is created.
        /// </summary>
        /// <param name="component">The new added component.</param>
        /// <param name="types">Returns the types to track instance changes. Only the type in this list is tracked, no derived types are registered.</param>
        /// <remarks>Only track changes in <see cref="UnityEngine.Object"/>> derived types and when the <see cref="SerializedObject"/> API is used, changes by script should explicitly call <see cref="EditorUtility.SetDirty"/>.</remarks>
        void GetTypesToTrack(Component component, List<Type> types);
    }
}
