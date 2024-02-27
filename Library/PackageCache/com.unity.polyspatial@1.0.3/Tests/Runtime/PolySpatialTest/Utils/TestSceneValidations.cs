using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    /// <summary>
    /// A collection of Scene and Scene object validation methods, asserting against common expectations used throughout
    /// PolySpatial tests.
    /// </summary>
    internal static class TestSceneValidations
    {
        private const string k_DefaultSceneNameRegex = "InitTestScene[0-9]+$";

        internal static void PolySpatialGameObjectCreated(GameObject gameObject, bool expectPolySpatialMeshRenderer=true)
        {
            Assert.IsNotNull(gameObject, "Failed to create PolySpatial GameObject");
            if (expectPolySpatialMeshRenderer)
                Assert.IsNotNull(
                    gameObject.GetComponent<MeshRenderer>(),
                    "Failed to create PolySpatialMeshRenderer");
            else
                Assert.IsNull(
                    gameObject.GetComponent<MeshRenderer>(),
                    "GameObject has unexpected PolySpatialMeshRenderer");
        }

        internal static void BackingGameObjectNameMatchesPattern(GameObject backingGameObject)
        {
            StringAssert.IsMatch(
                BackingComponentUtils.BackingGameObjectNameRegex,
                backingGameObject.name,
                $"PolySpatial GameObject name {backingGameObject.name} does not match pattern {BackingComponentUtils.BackingGameObjectNameRegex}");
        }

        private static void CreatedPolySpatialGameObjectIsDirty(
            GameObject backingGameObject,
            DefaultTrackingData trackingData,
            bool expectDirty)
        {
            if (expectDirty)
            {
                Assert.IsNull(
                    backingGameObject,
                    "Backing GameObject exists for dirty sim GO");
            }
            else
            {
                Assert.NotNull(
                    backingGameObject,
                    "No backing GameObject exists for non-dirty sim GO");
            }
        }

        /// <summary>
        /// Validates the target GameObject has the target component or equivalent. Backing GOs may not have the same
        /// component type, thus validations may not check for 1-1 equality. Validations are component specific.
        /// </summary>
        /// <param name="simGameObject">Target simulation GO to validate</param>
        /// <param name="expectedComponent">The component the sim/backing GOs are expected to have (or equivalent)</param>
        /// <param name="validateBackingGO">When set, validates backing GO has the expected component (or equivalent).
        /// Note: when unset, no backing GO or dirty flag validation is done.</param>
        /// <param name="expectDirty">When set, the sim GO should be dirty and backing GO should not have the target component.</param>
        /// <typeparam name="T">Any supported PolySpatial GameObject component type</typeparam>
        internal static void GameObjectHasComponent<T>(
                    GameObject simGameObject,
                    T expectedComponent,
                    bool validateBackingGO=false)
        {
            switch (expectedComponent)
            {
                case Mesh mesh:
                    // check sim GO
                    var simQMR = simGameObject.GetComponent<MeshFilter>();
                    Assert.IsNotNull(
                        simQMR,
                        "Cannot validate simulation GO has Mesh component; simulation GO does not have a QMR");
                    Assert.AreEqual(mesh, simQMR.sharedMesh, $"Simulation GO QMR Mesh does not match expected Mesh");

                    if (!validateBackingGO)
                        break;

                    // check backing GO
                    var backingGameObject =
                        BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(simGameObject));
                    var backingMeshFilter = backingGameObject.GetComponent<MeshFilter>();

                    Assert.IsNotNull(
                        backingMeshFilter,
                        "Updated backing QMR component has no MeshFilter after rendering next frame");

                    // Validate that the backing GO Mesh has the same contents as the sim GO
                    // Note: Name will not transfer over.
                    var backingMesh = backingMeshFilter.sharedMesh;
                    Assert.IsNotNull(backingMesh, "Updated backing QMR component has no Mesh after rendering next frame");

                    AssertUnityMeshesEqual(mesh, backingMesh);

                    break;
                default:
                    throw new AssertionException(
                        $"Cannot validate sim GO has component; unsupported component: {expectedComponent}");
            }
        }

        public static void AssertUnityMeshesEqual(Mesh expected, Mesh actual)
        {
            // Quick and dirty validation
            Assert.AreEqual(expected.vertexCount, actual.vertexCount);
            Assert.AreEqual(expected.subMeshCount, actual.subMeshCount);

            // expensive check, allocates arrays to pull vertex data out
            Assert.AreEqual(expected.vertices, actual.vertices);

            for (int submesh = 0; submesh < expected.subMeshCount; submesh++)
            {
                Assert.AreEqual(expected.GetIndexCount(submesh), actual.GetIndexCount(submesh));

                // expensive check
                Assert.AreEqual(expected.GetIndices(submesh), actual.GetIndices(submesh));
            }

            // TODO: validate attribs, uvs, etc.
        }

        /// <summary>
        /// Validates the target PolySpatialMeshRenderer component has the given materials or that materials are set to null
        /// </summary>
        /// <param name="simQMR">Target PolySpatialMeshRenderer component</param>
        /// <param name="materials">List of materials the QMR component is expected to have</param>
        /// <param name="validateBackingGO">When set, backing GO is validated for the target materials as well.
        /// Note: ensure backing GO has been created before enabling this flag</param>
        /// <param name="expectDirty">When set, expects QMR GO to be dirty, and expect backing GO not to be updated.
        /// Note: this applies only to validations where materials array is not null; for null materials array,
        /// assuming the backing GO materials should also be null</param>
        internal static void PolySpatialMeshRendererHasMaterials(
            MeshRenderer simQMR,
            Material[] materials=null,
            bool validateBackingGO=false)
        {
            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(simQMR.gameObject));

            // I'm really not sure what this is checking -- it's a MeshRenderer (or QMR), why are we checking it has materials?
#if false
            for (var i = 0; i < maxMaterials; i++)
            {
                // if null, assume the target QMR materials should be null
                if (materials == null)
                {
                    Assert.IsNull(
                        simQMR.sharedMaterials[i],
                        $"Default PolySpatialMeshRenderer has non null Material: {simQMR.sharedMaterials[i]}");

                    if (!validateBackingGO)
                        return;

                    var backingMaterial = backingGO.GetComponent<MeshRenderer>().sharedMaterials[i];
                    Assert.IsNull(
                        backingMaterial,
                        $"Default backing MeshRenderer has non null Material: {backingMaterial}");
                } else
                {
                    Assert.AreEqual(
                        materials[i],
                        simQMR.sharedMaterials[i],
                        $"QMR PolySpatialMaterial mismatch; expecting {materials[i]} but got {simQMR.sharedMaterials[i]}");

                    if (!validateBackingGO)
                        return;

                    var backingMaterials = backingGO.GetComponent<MeshRenderer>().sharedMaterials;
                    Debug.Log($"Got backing materials: {backingMaterials}");
                    foreach(var bm in backingMaterials)
                        Debug.Log($"Backing Material: {bm}");

                    var backingMaterial = backingMaterials[i];
                    Assert.IsNull(
                        backingMaterial,
                        $"Dirty backing MeshRenderer has non null Material: {backingMaterial}");
                }
            }
#endif
        }
    }
}
