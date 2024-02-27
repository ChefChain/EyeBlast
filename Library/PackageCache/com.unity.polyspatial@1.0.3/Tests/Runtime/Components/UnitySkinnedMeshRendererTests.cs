using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnitySkinnedMeshRendererTests : ComponentTestBase
    {
        GameObject m_TestGameObject;
        Transform[] m_TestSkeleton;
        SkinnedMeshRenderer m_SkinnedMeshRenderer;

        const string k_TestMaterialName = "SkinnedMeshRendererTestMaterial";
        const string k_TestMeshName = "SkinnedMeshRendererTestMesh";
        const string k_TestGameObjectName = "SkinnedMeshRenderer Test Object";

        private Mesh CreateTestSkinnedMesh()
        {
            Assert.IsNotNull(m_TestSkeleton, "Test skeleton was null. Make sure to call CreateTestObjects() before attempting to create a mesh.");
            Assert.IsTrue(m_TestSkeleton.Length > 0, "Skeleton bone length was 0. Make sure to call CreateTestObjects() before attempting to create a mesh.");

            var mesh = new Mesh();
            // a mesh must have some vertices and at least 1 submesh in order to be sent
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.down };
            mesh.SetIndices(new[] { 0, 1, 2}, MeshTopology.Triangles, 0);
            mesh.name = k_TestMeshName;

            // Set bone weights. The size of the boneweights array must match the size of the mesh vertices array.
            BoneWeight[] weights = new BoneWeight[3];
            weights[0].boneIndex0 = 0;
            weights[0].weight0 = 1;
            weights[1].boneIndex0 = 0;
            weights[1].weight0 = 1;
            weights[2].boneIndex0 = 1;
            weights[2].weight0 = 1;
            mesh.boneWeights = weights;

            // Set bind poses. Skip the first one in the m_TestSkeleton array since that one is the root game object for
            // the other bones and has no parent, as set up in CreateTestObjects. Note the length of the bindposes has to be equal to the skeleton array length regardless,
            // so the first bindpose will just be identity.
            var bindPoses = new Matrix4x4[m_TestSkeleton.Length];
            bindPoses[0] = Matrix4x4.identity;
            for (int i = 1; i < m_TestSkeleton.Length; i++)
            {
                bindPoses[i] = m_TestSkeleton[i].worldToLocalMatrix * m_TestSkeleton[i].parent.localToWorldMatrix;
            }

            mesh.bindposes = bindPoses;

            return mesh;
        }

        void CreateTestObjects()
        {
            m_TestGameObject = new GameObject(k_TestGameObjectName);
            m_SkinnedMeshRenderer = m_TestGameObject.AddComponent<SkinnedMeshRenderer>();
            m_TestSkeleton = new Transform[4];

            var boneIndex = 0;
            for (int i = 0; i < m_TestSkeleton.Length; i++)
            {
                var childName = "ChildBones: " + boneIndex++;
                var newGameObjects = new GameObject(childName);
                m_TestSkeleton[i] = newGameObjects.transform;
            }

            // Arbitrarily create an hierarchy.
            m_TestSkeleton[1].parent = m_TestSkeleton[0];
            m_TestSkeleton[2].parent = m_TestSkeleton[0];
            m_TestSkeleton[3].parent = m_TestSkeleton[1];

            m_TestSkeleton[1].localPosition = new Vector3(0, 5, 0);
            m_TestSkeleton[2].localPosition = new Vector3(5, 0, 0);
            m_TestSkeleton[3].localPosition = new Vector3(0, 0, 5);

            // Set defaults for the skinned mesh renderer. It has to have bones and a rootBone to work, and the quality
            // is set to default.
            m_SkinnedMeshRenderer.bones = m_TestSkeleton;
            m_SkinnedMeshRenderer.quality = SkinQuality.Auto;
            m_SkinnedMeshRenderer.rootBone = m_TestGameObject.transform;
        }

        internal override IEnumerator InternalUnityTearDown()
        {
            if (m_SkinnedMeshRenderer != null)
                Object.Destroy(m_SkinnedMeshRenderer);
            if (m_TestGameObject != null)
                Object.Destroy(m_TestGameObject);
            foreach (var bone in m_TestSkeleton)
            {
                if (bone != null)
                {
                    Object.Destroy(bone.gameObject);
                }
            }
            m_TestGameObject = null;
            m_SkinnedMeshRenderer = null;
            m_TestSkeleton = null;

            yield return base.InternalUnityTearDown();
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(data, PolySpatialTrackingFlags.Running);

            var criid = m_SkinnedMeshRenderer.GetInstanceID();

            // destroy the component
            Object.Destroy(m_SkinnedMeshRenderer);

            yield return null;

#if UNITY_EDITOR
            // Check to see if data stays deleted. We no longer clear the dirty flag, as we will not touch this data any more.
            var deletedData = PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(criid);
            Assert.IsTrue(deletedData.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(deletedData, PolySpatialTrackingFlags.Destroyed);
#endif

            yield return null;

            // After another frame, the tracking data is gone, and GetSkinnedMeshRendererTrackingData throws.
            Assert.Throws<InvalidOperationException>(() => PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(criid));
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Disable_Tracking()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.IsTrue(backingGO.GetComponent<SkinnedMeshRenderer>().enabled);

            m_SkinnedMeshRenderer.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            if (backingGO != null)
                Assert.IsFalse(backingGO.GetComponent<SkinnedMeshRenderer>() != null);
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Set_Mesh_Updates_MeshRendererData()
        {
            CreateTestObjects();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var oldSkinMeshAssetId = data.customData.meshRendererTrackingData.meshId;

            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // let the changes happen
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreNotEqual(oldSkinMeshAssetId, data.customData.meshRendererTrackingData.meshId);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_MaterialChanges()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // When the skinned mesh renderer has its material changed/set, it creates a new instance of the original material
            // and uses that instead, so we have to track the material in the skinned mesh renderer, not the original.
            m_SkinnedMeshRenderer.materials = new Material[] {PolySpatialComponentUtils.CreateUnlitMaterial(Color.red)};
            m_SkinnedMeshRenderer.material.name = k_TestMaterialName;
            var material1 = m_SkinnedMeshRenderer.material;

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            var materialIds1 = data1.customData.meshRendererTrackingData.materials.materialIds;
            Assert.AreEqual(1, materialIds1.Length);
            Assert.IsTrue(materialIds1[0].IsValid(), "Expected first material to be valid.");
            Assert.AreEqual(materialIds1[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(material1, PolySpatialCore.LocalAssetManager.GetRegisteredResource(materialIds1[0]));

            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(materialIds1[0], out var materialData1);
            // this isn't a complete verification of material conversion, that needs a separate suite
            Assert.AreEqual(PolySpatialMaterialType.Unlit, materialData1.materialType);
            Assert.IsTrue(materialData1.baseColorMap.isEnabled, "Base color map was not enabled.");
            Assert.AreEqual(Color.red, materialData1.baseColorMap.color);

            // now modify the material's color.  We should get a material change notification and the material data
            // should get updated
            material1.color = Color.blue;

            yield return null;

            // The AssetID shouldn't change
            var data2 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            var materialIds2 = data1.customData.meshRendererTrackingData.materials.materialIds;
            Assert.AreEqual(materialIds1[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(materialIds1[0], materialIds2[0]);

            // but the data should
            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(materialIds1[0], out var materialData2);
            Assert.AreEqual(Color.blue, materialData2.baseColorMap.color);
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_CheckSkinnedMeshRendererBones()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            var cachedData = data.customData;

            // Test to see if it's got the correct data.
            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            if (backingGO != null)
            {
                var backingSkinnedMeshRenderer = backingGO.GetComponent<SkinnedMeshRenderer>();

                for (var i = 0; i < m_SkinnedMeshRenderer.bones.Length; i++)
                {
                    var simulationId = PolySpatialInstanceID.For(m_SkinnedMeshRenderer.bones[i].gameObject);
                    var backingBoneGO = backingSkinnedMeshRenderer.bones[i].gameObject;
                    Assert.AreEqual(BackingComponentUtils.GetBackingGameObjectFor(simulationId), backingBoneGO,
                        $"Backing bone game object {backingBoneGO} does not match original bone {m_SkinnedMeshRenderer.bones[i].gameObject}");
                }

                var simulatorRootBoneId = PolySpatialInstanceID.For(m_SkinnedMeshRenderer.rootBone.gameObject);
                var backingRootBone = backingSkinnedMeshRenderer.rootBone.gameObject;
                Assert.AreEqual(BackingComponentUtils.GetBackingGameObjectFor(simulatorRootBoneId), backingRootBone,
                    $"Backing root bone {backingRootBone} does not match original root {m_SkinnedMeshRenderer.rootBone.gameObject}.");

                Assert.AreEqual(m_SkinnedMeshRenderer.quality, backingSkinnedMeshRenderer.quality, "Skinned mesh quality are not equivalent.");

                yield return null;

                data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
                Assert.IsTrue(data.ValidateTrackingFlags());
                cachedData = data.customData;

                // Destroy the skinned mesh renderer and ensure proper cleanup happens. Cache the ids so we can attempt to access them later for testing.
                var smriid = m_SkinnedMeshRenderer.GetInstanceID();
                var mesh = m_SkinnedMeshRenderer.sharedMesh;
                var material = m_SkinnedMeshRenderer.material;
                m_SkinnedMeshRenderer.DestroyAppropriately();

                yield return null;

    #if UNITY_EDITOR
                data = PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(smriid);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsFalse(data.customData.meshRendererTrackingData.meshId.IsValid());
                foreach (var materialId in data.customData.meshRendererTrackingData.materials.materialIds)
                    Assert.IsFalse(materialId.IsValid());
    #endif

                mesh.DestroyAppropriately();
                material.DestroyAppropriately();

                yield return null;

                void ClearIfDeleted(IAssetManager mgr, ref PolySpatialAssetID aid) {
                    if (aid != PolySpatialAssetID.InvalidAssetID && mgr.GetRegisteredResource(aid) == null)
                    {
                        aid = PolySpatialAssetID.InvalidAssetID;
                    }
                }

                Assert.AreEqual(1, cachedData.meshRendererTrackingData.materials.materialIds.Length);

                var cachedDataBackend = cachedData;

                m_TestPlatformWrapper.OnAfterAssetsDeletedCalled = (assetIds) =>
                {
                    ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cachedData.meshRendererTrackingData.meshId);
                    ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cachedData.meshRendererTrackingData.materials.materialIds.ElementAt(0));

                    // Assert that meshes and materials have been properly removed from the Platform Asset Manager.
                    ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cachedDataBackend.meshRendererTrackingData.meshId);
                    ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cachedDataBackend.meshRendererTrackingData.materials.materialIds.ElementAt(0));
                };

                yield return null;

                Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedData.meshRendererTrackingData.meshId);
                Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedData.meshRendererTrackingData.materials.materialIds[0]);
                Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedDataBackend.meshRendererTrackingData.meshId);
                Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedDataBackend.meshRendererTrackingData.materials.materialIds[0]);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_MultipleMaterials()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            m_SkinnedMeshRenderer.sharedMaterials = Array.Empty<Material>();

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            Assert.AreEqual(0, data1.customData.meshRendererTrackingData.materials.materialIds.Length);
            Assert.IsFalse(data1.customData.meshRendererTrackingData.materials.hasExternalMaterials);

            int maxMaterialsInFixedBuffer = data1.customData.meshRendererTrackingData.materials.materialIds.Capacity;
            var materials = new Material[maxMaterialsInFixedBuffer + 1];
            for (int i=0; i<maxMaterialsInFixedBuffer + 1; i++)
                materials[i] = PolySpatialComponentUtils.CreateUnlitMaterial(Color.red);

            m_SkinnedMeshRenderer.sharedMaterial = materials[0];

            yield return null;

            var data2 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            Assert.AreEqual(1, data2.customData.meshRendererTrackingData.materials.materialIds.Length);
            Assert.IsFalse(data2.customData.meshRendererTrackingData.materials.hasExternalMaterials);

            m_SkinnedMeshRenderer.sharedMaterials = materials;

            yield return null;

            var data3 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data3.ValidateTrackingFlags());
            Assert.AreEqual(0, data3.customData.meshRendererTrackingData.materials.materialIds.Length);

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.AreEqual(materials.Length, backingGO.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length);

            Assert.IsTrue(data3.customData.meshRendererTrackingData.materials.hasExternalMaterials);

            m_SkinnedMeshRenderer.sharedMaterials = new[] {materials[0]};

            yield return null;

            var data4 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data4.ValidateTrackingFlags());
            Assert.AreEqual(1, data4.customData.meshRendererTrackingData.materials.materialIds.Length);
            Assert.IsFalse(data4.customData.meshRendererTrackingData.materials.hasExternalMaterials);
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_ShouldUpdateSkeletonWhenEnableDisable()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            bool? shouldUpdateSkeleton = true;

            void AssertUpdateSkeleton(bool? actualValue)
            {
                UnityEngine.Assertions.Assert.AreEqual(shouldUpdateSkeleton, actualValue, $"Expected skeleton update to be set to {shouldUpdateSkeleton} but was {actualValue}!");
                // shouldUpdateSkeleton should be true the first time a valid change is detected,
                // then false from then on.
                shouldUpdateSkeleton = false;
            }
            SkinnedMeshRendererTracker.OnShouldUpdateSkeleton += AssertUpdateSkeleton;

            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            yield return null;

            m_SkinnedMeshRenderer.gameObject.SetActive(false);
            shouldUpdateSkeleton = false;
            yield return null;

            m_SkinnedMeshRenderer.gameObject.SetActive(true);
            shouldUpdateSkeleton = true;
            yield return null;

            // Should ignore the fact that a mesh was changed since
            // skinned mesh is still inactive.
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            m_SkinnedMeshRenderer.gameObject.SetActive(false);
            shouldUpdateSkeleton = false;
            yield return null;

            m_SkinnedMeshRenderer.gameObject.SetActive(true);
            shouldUpdateSkeleton = true;
            yield return null;

            // With the renderer only casting shadows, shouldUpdateSkeleton should be null.
            m_SkinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            shouldUpdateSkeleton = null;
            yield return null;

            SkinnedMeshRendererTracker.OnShouldUpdateSkeleton -= AssertUpdateSkeleton;
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_ShouldUpdateSkeletonWhenChangeMesh()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            bool? shouldUpdateSkeleton = false;

            void AssertUpdateSkeleton(bool? actualValue)
            {
                UnityEngine.Assertions.Assert.AreEqual(shouldUpdateSkeleton, actualValue, $"Expected skeleton update to be set to {shouldUpdateSkeleton} but was {actualValue}!");
                // shouldUpdateSkeleton should be true the first time a valid change is detected,
                // then false from then on.
                shouldUpdateSkeleton = false;
            }
            SkinnedMeshRendererTracker.OnShouldUpdateSkeleton += AssertUpdateSkeleton;

            // Attempt to start off with a disabled skinned mesh renderer, before enabling it.
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            m_SkinnedMeshRenderer.enabled = false;
            yield return null;

            m_SkinnedMeshRenderer.enabled = true;
            shouldUpdateSkeleton = true;
            yield return null;

            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            shouldUpdateSkeleton = true;
            yield return null;

            // With no mesh, shouldUpdateSkeleton should be null.
            m_SkinnedMeshRenderer.sharedMesh = null;
            shouldUpdateSkeleton = null;
            yield return null;

            SkinnedMeshRendererTracker.OnShouldUpdateSkeleton -= AssertUpdateSkeleton;
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_NullMesh()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = null;

            yield return null;

            AssertNullSkinnedMeshRenderer();
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_ShadowsOnly()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            m_SkinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            yield return null;

            AssertNullSkinnedMeshRenderer();
        }

        void AssertNullSkinnedMeshRenderer()
        {
#if UNITY_EDITOR
            var backingGameObject = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            Assert.NotNull(backingGameObject);
            var skinnedMeshRenderer = backingGameObject.GetComponent<SkinnedMeshRenderer>();
            Assert.Null(skinnedMeshRenderer);
#endif
        }
    }
}
