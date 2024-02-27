using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PolySpatial.Utilities
{
    /// <summary>
    /// Utility class to remove components from Game Objects.
    /// </summary>
    static class RemoveComponentUtils
    {
        static IEnumerable<Component> ComponentDependencies([DisallowNull] Component component)
        {
            var componentType = component.GetType();
            foreach (var c in component.gameObject.GetComponents<Component>())
            {
                foreach (var rc in c.GetType().GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>())
                {
                    if (rc.m_Type0 == componentType || rc.m_Type1 == componentType || rc.m_Type2 == componentType)
                    {
                        yield return c;
                        break;
                    }
                }
            }
        }

        static void GetAllComponentDependencies([DisallowNull] Component comp, Stack<Component> allDependencies)
        {
            var dependencies = ComponentDependencies(comp);
            foreach (var c in dependencies)
            {
                if (!allDependencies.Contains(c))
                {
                    allDependencies.Push(c);
                    GetAllComponentDependencies(c, allDependencies);
                }
            }
        }

        static bool CanRemoveComponent([DisallowNull] Component component, IEnumerable<Component> dependencies)
        {
            if (dependencies.Count() == 0)
                return true;

            var firstDependency = dependencies.First();
            var error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            EditorUtility.DisplayDialog("Can't remove component", error, "Ok");
            return false;
        }

        static bool RemoveComponent([DisallowNull] Component comp)
        {
            var dependencies = ComponentDependencies(comp);
            if (!CanRemoveComponent(comp, dependencies))
                return false;

            Undo.DestroyObjectImmediate(comp);
            return true;
        }

        /// <summary>
        /// Removes the given component and all other components that depends on it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        internal static void RemoveComponentAndDependencies([DisallowNull] Component component)
        {
            var allDependencies = new Stack<Component>();
            allDependencies.Push(component);
            GetAllComponentDependencies(component, allDependencies);

            while (allDependencies.Count > 0)
            {
                var c = allDependencies.Pop();
                // It's possible to have cyclical component dependencies
                if (!RemoveComponent(c))
                    break;
            }
        }
    }
}
