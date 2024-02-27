using System;
using System.Linq;
using UnityEngine;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    internal static class TestSceneActions
    {
        /// <summary>
        /// Creates a GameObject Primitive with a PolySpatialMeshRenderer component
        /// </summary>
        /// <param name="meshRenderer">output variable to store reference to created PolySpatialMeshRenderer</param>
        /// <param name="primitiveType">Type of Primitive to create</param>
        /// <param name="primitiveName">Name to give the GameObject</param>
        /// <returns>Created GameObject</returns>
        internal static GameObject CreatePolySpatialPrimitive(out MeshRenderer meshRenderer,
            PrimitiveType primitiveType, string primitiveName)
        {
            var primitiveGameObject = GameObject.CreatePrimitive(primitiveType);
            primitiveGameObject.name = primitiveName;
            meshRenderer = primitiveGameObject.GetComponent<MeshRenderer>();
            return primitiveGameObject;
        }

        /// <summary>
        /// Creates a GameObject with a PolySpatialMeshRenderer component and PolySpatialMaterials
        /// </summary>
        /// <param name="meshRenderer">output variable to store references to created PolySpatialMeshRenderer</param>
        /// <param name="gameObjectName">Name to give the GameObject</param>
        /// <param name="polyspatialMaterials"> List of PolySpatialMaterials to add to the created GameObject's Mesh</param>
        /// <returns></returns>
        internal static GameObject CreatePolySpatialGameObject(
            out MeshRenderer meshRenderer, string gameObjectName, Mesh mesh = null, Material[] materials = null)
        {
            meshRenderer = null;

            var gameObject = new GameObject(gameObjectName);
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (mesh != null)
                meshFilter.sharedMesh = mesh;

            if (materials?.Length > 0)
            {
                Debug.Log($"Adding {materials.Length} Materials to the test GameObject's MR");
                for (var i = 0; i < materials.Length; i++)
                {
                    meshRenderer.materials[i] = materials[i];
                }
            }

            return gameObject;
        }
    }
}
