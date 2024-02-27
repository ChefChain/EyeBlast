using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

using Assert = NUnit.Framework.Assert;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityMeshRendererTests : ComponentTestBase
    {
        static readonly string[] k_ShadersToTest = MaterialShaders.k_ShadersToTest;

        GameObject m_TestGameObject;
        MeshFilter m_TestMeshFilter;
        MeshRenderer m_TestMeshRenderer;
        Material m_TestMaterial;

        private Mesh CreateTestMesh()
        {
            var mesh = new Mesh();
            // a mesh must have some vertices and at least 1 submesh in order to be sent
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.down };
            mesh.SetIndices(new[] { 0, 1, 2}, MeshTopology.Triangles, 0);
            mesh.name = "Test Mesh";
            return mesh;
        }

        (GameObject, MeshRenderer, MeshFilter) CreateTestObjects()
        {
            m_TestGameObject = new GameObject("MeshRenderer Test Object");
            m_TestMeshFilter = m_TestGameObject.AddComponent<MeshFilter>();
            m_TestMeshRenderer = m_TestGameObject.AddComponent<MeshRenderer>();
            return (m_TestGameObject, m_TestMeshRenderer, m_TestMeshFilter);
        }


        internal override void InternalTearDown()
        {
            if (m_TestMeshRenderer != null)
                m_TestMeshRenderer.DestroyAppropriately();
            if (m_TestMeshFilter != null)
                m_TestMeshFilter.DestroyAppropriately();
            if (m_TestGameObject != null)
                m_TestGameObject.DestroyAppropriately();
            if (m_TestMaterial != null)
                m_TestMaterial.DestroyAssetIrrecoverably();

            m_TestGameObject = null;
            m_TestMeshFilter = null;
            m_TestMeshRenderer = null;
            base.InternalTearDown();
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var mriid = m_TestMeshRenderer.GetInstanceID();

            // destroy the component
            m_TestMeshRenderer.DestroyAppropriately();
            m_TestMeshFilter.DestroyAppropriately();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetMeshRendererTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_Create_Disable_Enable_Tracking()
        {
            CreateTestObjects();
            m_TestMeshFilter.sharedMesh = CreateTestMesh();
            m_TestGameObject.SetActive(false);

            // Let a frame be processed to create the inactive backing objects
            yield return null;

            // right after the frame, we expect inactive in the tracking flags
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse((data.TrackingFlags & PolySpatialTrackingFlags.Inactive) == 0);

            var mriid = m_TestMeshRenderer.GetInstanceID();

            // DebugPolySpatialGameObjectLink is only set up in Editor and Development builds
            // On CI and in shipped packages, we pre-compile runtime code, but not tests. As a result, the runtime assembly is built without `DEVELOPMENT_BUILD`
            // or `UNITY_EDITOR` defined. Thus, on CI runs, DebugPolySpatialGameObjectLink components are not used. For macOS tests, the RealityKit backend
            // is used, but on Windows, PolySpatialUnityBackend is used, without DebugPolySpatialGameObjectLink. This is why we also skip validation on backing
            // objects when UNITY_STANDALONE_WIN is defined.
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && !UNITY_STANDALONE_WIN
            DebugPolySpatialGameObjectLink link = null;
            if (PolySpatialCore.LocalBackend is PolySpatialUnityBackend)
            {
                // get linked object, and verify that it does not have rendering components yet
                link = m_TestGameObject.GetComponent<DebugPolySpatialGameObjectLink>();
                Assert.IsNotNull(link);
                Assert.IsNull(link.LinkedTo.GetComponent<MeshFilter>());
                Assert.IsNull(link.LinkedTo.GetComponent<MeshRenderer>());
            }
#endif

            m_TestGameObject.SetActive(true);

            // wait a frame to track the activation
            yield return null;

            // we should no longer be inactive
            data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue((data.TrackingFlags & PolySpatialTrackingFlags.Inactive) == 0);

#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && !UNITY_STANDALONE_WIN
            if (link != null)
            {
                // check that backing objects have a MeshRenderer and MeshCollider
                Assert.IsNotNull(link.LinkedTo.GetComponent<MeshFilter>());
                Assert.IsNotNull(link.LinkedTo.GetComponent<MeshRenderer>());
            }
#endif

            // destroy the component
            m_TestMeshRenderer.DestroyAppropriately();
            m_TestMeshFilter.DestroyAppropriately();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetMeshRendererTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_Disable_Tracking()
        {
            CreateTestObjects();
            m_TestMeshFilter.mesh = CreateTestMesh();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.IsTrue(backingGO.GetComponent<MeshRenderer>().enabled);

            m_TestMeshRenderer.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            if (backingGO != null)
                Assert.IsFalse(backingGO.GetComponent<MeshRenderer>() != null);
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_Create_DestroyGameObject_Tracking()
        {
            CreateTestObjects();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var mriid = m_TestMeshRenderer.GetInstanceID();

            // destroy the gameobject
            m_TestGameObject.DestroyAppropriately();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetMeshRendererTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif

            // todo need GetGameObjectTrackingData; at least we can make sure this doesn't crash
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_Set_Mesh_Updates_MeshRendererData()
        {
            CreateTestObjects();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var oldMeshAsset = data.customData.meshId;

            m_TestMeshFilter.mesh = CreateTestMesh();

            // let the changes happen
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreNotEqual(oldMeshAsset, data.customData.meshId, "Expected the mesh asset ID to be different.");
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_CleanupResourcesOnDestroy()
        {
            CreateTestObjects();

            m_TestMeshFilter.sharedMesh = CreateTestMesh();
            m_TestMeshRenderer.sharedMaterial = PolySpatialComponentUtils.CreateUnlitMaterial(
                Color.blue, "Textures/Texture2DBlue");

            // Let a frame be processed and trigger the above assertions
            yield return null;

            var mriid = m_TestMeshRenderer.GetInstanceID();

            var data = PolySpatialComponentUtils.GetMeshRendererTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            var cacheData = data.customData;
            var cacheMaterials = cacheData.materials;
            var cacheDataBackend = cacheData;
            Assert.IsNotNull(PolySpatialCore.LocalAssetManager.GetRegisteredResource(cacheData.meshId));
            Assert.IsTrue(cacheMaterials.materialIds[0].IsValid());
            Assert.IsNotNull(PolySpatialCore.LocalAssetManager.GetRegisteredResource(cacheMaterials.materialIds[0]));
            Assert.IsFalse(cacheMaterials.hasExternalMaterials);

            yield return null;

            PolySpatialMaterialData cacheQmd;
            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(cacheMaterials.materialIds[0], out cacheQmd);
            Assert.IsTrue(cacheQmd.baseColorMap.assetId.IsValid());
            Assert.IsNotNull(PolySpatialCore.LocalAssetManager.GetRegisteredResource(cacheQmd.baseColorMap.assetId));

            // Since we created these, and since they are shared, the components will
            // not destroy them when they are destroyed. We have to destroy them manually
            // later.
            var mesh = m_TestMeshFilter.sharedMesh;
            var material = m_TestMeshRenderer.sharedMaterial;

            //destroy the component
            m_TestMeshRenderer.DestroyAppropriately();
            m_TestMeshFilter.DestroyAppropriately();
            m_TestGameObject.DestroyAppropriately();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetMeshRendererTrackingData(mriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.customData.meshId.IsValid());
            foreach (var materialId in data.customData.materials.materialIds)
            {
                Assert.IsFalse(materialId.IsValid());
            }
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

            Assert.AreEqual(1, cacheMaterials.materialIds.Length);

            // OnAfterAssetsDeleteCalled is technically wrong -- that function can be called
            // multiple times per frame, and there's no guarantee that all assets will have been
            // deleted after a single call.  So this needs to handle that case.
            // TODO this would be much better if we could wait for an EndFrame command and do
            // our checks after that
            m_TestPlatformWrapper.OnAfterAssetsDeletedCalled = (assetIds) =>
            {
                ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cacheData.meshId);
                ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cacheMaterials.materialIds.ElementAt(0));

                // Assert that meshes and materials have been properly removed from the Platform Asset Manager.
                ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cacheDataBackend.meshId);
                ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cacheDataBackend.materials.materialIds.ElementAt(0));
            };

            yield return null;

            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cacheData.meshId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cacheMaterials.materialIds[0]);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cacheDataBackend.meshId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cacheDataBackend.materials.materialIds[0]);
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_MaterialChanges()
        {
            CreateTestObjects();
            m_TestMeshFilter.mesh = CreateTestMesh();

            var material1 = PolySpatialComponentUtils.CreateUnlitMaterial(Color.red);
            m_TestMeshRenderer.sharedMaterial = material1;

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            Assert.AreEqual(1, data1.customData.materials.materialIds.Length);
            Assert.IsTrue(data1.customData.materials.materialIds[0].IsValid());
            Assert.AreEqual(data1.customData.materials.materialIds[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(material1, PolySpatialCore.LocalAssetManager.GetRegisteredResource(data1.customData.materials.materialIds[0]));

            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(data1.customData.materials.materialIds[0], out var materialData1);
            // this isn't a complete verification of material conversion, that needs a separate suite
            Assert.AreEqual(PolySpatialMaterialType.Unlit, materialData1.materialType);
            Assert.IsTrue(materialData1.baseColorMap.isEnabled);
            Assert.AreEqual(Color.red, materialData1.baseColorMap.color);

            // now modify the material's color.  We should get a material change notification and the material data
            // should get updated
            material1.color = Color.blue;

            yield return null;

            // The AssetID shouldn't change
            var data2 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            Assert.AreEqual(data1.customData.materials.materialIds[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(data1.customData.materials.materialIds[0], data2.customData.materials.materialIds[0]);

            // but the data should
            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(data1.customData.materials.materialIds[0], out var materialData2);
            Assert.AreEqual(Color.blue, materialData2.baseColorMap.color);
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_MultipleMaterials()
        {
            CreateTestObjects();
            m_TestMeshFilter.sharedMesh = CreateTestMesh();
            m_TestMeshRenderer.sharedMaterials = Array.Empty<Material>();

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            Assert.AreEqual(0, data1.customData.materials.materialIds.Length);
            Assert.IsFalse(data1.customData.materials.hasExternalMaterials);

            int maxMaterialsInFixedBuffer = data1.customData.materials.materialIds.Capacity;
            var materials = new Material[maxMaterialsInFixedBuffer + 1];
            for (int i=0; i<maxMaterialsInFixedBuffer + 1; i++)
                materials[i] = PolySpatialComponentUtils.CreateUnlitMaterial(Color.red);

            m_TestMeshRenderer.sharedMaterial = materials[0];

            yield return null;

            var data2 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            Assert.AreEqual(1, data2.customData.materials.materialIds.Length);
            Assert.IsFalse(data2.customData.materials.hasExternalMaterials);

            m_TestMeshRenderer.sharedMaterials = materials;

            yield return null;

            var data3 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data3.ValidateTrackingFlags());
            Assert.AreEqual(0, data3.customData.materials.materialIds.Length);

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.AreEqual(materials.Length, backingGO.GetComponent<MeshRenderer>().sharedMaterials.Length);

            Assert.IsTrue(data3.customData.materials.hasExternalMaterials);

            m_TestMeshRenderer.sharedMaterials = new[] {materials[0]};

            yield return null;

            var data4 = PolySpatialComponentUtils.GetTrackingData(m_TestMeshRenderer);
            Assert.IsTrue(data4.ValidateTrackingFlags());
            Assert.AreEqual(1, data4.customData.materials.materialIds.Length);
            Assert.IsFalse(data4.customData.materials.hasExternalMaterials);
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_SupportedShader([ValueSource(nameof(k_ShadersToTest))] string shaderName)
        {
            if (shaderName.Equals(String.Empty))
                yield break;

            CreateTestObjects();
            m_TestMeshFilter.sharedMesh = CreateTestMesh();
            var shader = Shader.Find(shaderName);
            if (shader == null)
                Assert.Ignore("Shader not found");

            m_TestMaterial = new Material(shader);
            m_TestMeshRenderer.sharedMaterial = m_TestMaterial;

            yield return null;

#if UNITY_EDITOR
            var backingGameObject = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestMeshRenderer.gameObject));
            Assert.NotNull(backingGameObject);
            var meshRenderer = backingGameObject.GetComponent<MeshRenderer>();
            Assert.NotNull(meshRenderer);
            Assert.NotNull(meshRenderer.sharedMaterial);
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_NullMesh()
        {
            CreateTestObjects();
            m_TestMeshFilter.sharedMesh = null;

            yield return null;

            AssertNullMeshRenderer();
        }

        [UnityTest]
        public IEnumerator Test_UnityMeshRenderer_ShadowsOnly()
        {
            CreateTestObjects();
            m_TestMeshFilter.sharedMesh = CreateTestMesh();
            m_TestMeshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            yield return null;

            AssertNullMeshRenderer();
        }

        void AssertNullMeshRenderer()
        {
#if UNITY_EDITOR
            var backingGameObject = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestMeshRenderer.gameObject));
            Assert.NotNull(backingGameObject);
            var meshRenderer = backingGameObject.GetComponent<MeshRenderer>();
            Assert.Null(meshRenderer);
#endif
        }
    }
}
