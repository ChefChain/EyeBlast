using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.PolySpatial.Utilities
{
    /// <summary>
    /// Utility methods for getting Components.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    static class ComponentUtility<T> where T : Component
    {
        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<GameObject> k_GameObjects = new List<GameObject>();
        static readonly List<T> k_Components = new List<T>();

        /// <summary>
        /// Retrieves all components of the given type in all loaded scenes.
        /// </summary>
        /// <param name="components">List that will be filled out with components retrieved.</param>
        /// <param name="includeInactive">Should Components on inactive GameObjects be included in the found set?</param>
        /// <param name="hideFlagsFilter">Objects with these flags are filtered out.</param>
        public static void GetComponentsInAllScenes(List<T> components, bool includeInactive, int hideFlagsFilter)
        {
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    GetComponentsInScene(scene, components, includeInactive, hideFlagsFilter);
            }
        }

        /// <summary>
        /// Retrieves all components of the given type in a scene.
        /// </summary>
        /// <param name="scene">The scene to search.</param>
        /// <param name="components">List that will be filled out with components retrieved.</param>
        /// <param name="includeInactive">Should Components on inactive GameObjects be included in the found set?</param>
        /// <param name="hideFlagsFilter">Objects with these flags are filtered out.</param>
        static void GetComponentsInScene(Scene scene, List<T> components, bool includeInactive, int hideFlagsFilter)
        {
            // k_GameObjects is cleared by GetRootGameObjects
            scene.GetRootGameObjects(k_GameObjects);
            foreach (var gameObject in k_GameObjects)
            {
                if (!includeInactive && !gameObject.activeInHierarchy)
                    continue;

                if (((int)gameObject.hideFlags & hideFlagsFilter) != 0)
                    continue;

                GetComponentsInChildren(gameObject, components, includeInactive, hideFlagsFilter);
            }

            // Clearing lists to avoid holding references
            k_GameObjects.Clear();
            k_Components.Clear();
        }

        /// <summary>
        /// Retrieves all components of the given type in the GameObject.
        /// </summary>
        /// <param name="gameObject">The object to search.</param>
        /// <param name="components">List that will be filled out with components retrieved.</param>
        /// <param name="includeInactive">Should Components on inactive GameObjects be included in the found set?</param>
        /// <param name="hideFlagsFilter">Objects with these flags are filtered out.</param>
        static void GetComponentsInChildren(GameObject gameObject, List<T> components, bool includeInactive, int hideFlagsFilter)
        {
            // k_Components is cleared by GetComponents
            gameObject.GetComponents(k_Components);
            components.AddRange(k_Components);
            foreach (Transform child in gameObject.transform)
            {
                var childGameObject = child.gameObject;

                if (!includeInactive && !childGameObject.activeInHierarchy)
                    continue;

                if (((int)childGameObject.hideFlags & hideFlagsFilter) != 0)
                    continue;

                GetComponentsInChildren(childGameObject, components, includeInactive, hideFlagsFilter);
            }
        }
    }
}
