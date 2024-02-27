using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Runtime.Functional.Components
{
    public class UnitySpriteRendererTests : PooledComponentTestBase<SpriteRenderer>
    {
        static IEnumerable<SpriteDrawMode> DrawModeEnum()
        {
            yield return SpriteDrawMode.Simple;
            yield return SpriteDrawMode.Sliced;
            yield return SpriteDrawMode.Tiled;
        }

        static IEnumerable<(bool, bool)> FlipModeEnum()
        {
            yield return (false, false);
            yield return (true, false);
            yield return (false, true);
            yield return (true, true);
        }

        void CreateTestObjects()
        {
            CreateTestObjects("Sprite Renderer Test");
        }

        internal override (GameObject, SpriteRenderer) CreateLocalTestObjects(string name)
        {
            var (go, sr) = base.CreateLocalTestObjects(name);
            var texture = new Texture2D(256, 256);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100, 0,
               SpriteMeshType.FullRect, new Vector4(64, 64, 64, 64));
            sr.sprite = sprite;
            sr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            return (go, sr);
        }

        internal (GameObject, SpriteMask) CreateLocalSpriteMaskTestObjects(string name)
        {
            var (go, sm) = base.CreateLocalTestObjects<SpriteMask>(name);
            var texture = new Texture2D(256, 256);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100, 0,
               SpriteMeshType.FullRect, new Vector4(64, 64, 64, 64));
            sm.sprite = sprite;
            return (go, sm);
        }

        void PopulateDefaultData(SpriteDrawMode drawMode)
        {
            var texture = new Texture2D(256, 256);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100, 0,
                SpriteMeshType.FullRect, new Vector4(64, 64, 64, 64));

            m_TestComponent.sprite = sprite;
            m_TestComponent.drawMode = drawMode;
            m_TestComponent.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        void CheckSortIndexForSprite(int instanceId, int expectedSortIndex, string name)
        {
            var data = PolySpatialComponentUtils.GetSpriteRendererData(instanceId);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsNotNull(data);
            Assert.AreEqual(expectedSortIndex, data.customData.globalSortIndex, $"Failed sort order check on {name}");
        }

        [UnityTest]
        public IEnumerator Test_Availability_Checks()
        {
            CreateTestObjects();

            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsTrue(data.IsActive());
                Assert.IsTrue(data.IsEnabled());
            }

            m_TestComponent.enabled = false;

            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsFalse(data.IsEnabled());
                Assert.IsTrue(data.IsActive());
                Assert.IsFalse(data.IsActiveAndEnabled());
            }
        }

        [UnityTest]
        public IEnumerator Test_IsActive_Flag()
        {
            CreateTestObjects();

            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsTrue(data.IsActive());
                Assert.IsTrue(data.IsEnabled());
                Assert.IsTrue(data.IsActiveAndEnabled());
            }

            m_TestComponent.gameObject.SetActive(false);

            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsFalse(data.IsActive());
                Assert.IsTrue(data.IsEnabled());
                Assert.IsFalse(data.IsActiveAndEnabled());
            }
        }

        [UnityTest]
        public IEnumerator Test_UnitySpriteRenderer_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var sriid = m_TestComponent.GetInstanceID();

            DestroyAllTestObjects();

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetSpriteRendererData(sriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_UnitySpriteRenderer_Disable_Tracking()
        {
            CreateTestObjects();
            PopulateDefaultData(SpriteDrawMode.Simple);

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestComponent.gameObject));
            if (backingGO != null)
                Assert.IsTrue(backingGO.GetComponent<MeshRenderer>().enabled);

            m_TestComponent.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            if (backingGO != null)
                Assert.IsFalse(backingGO.GetComponent<MeshRenderer>() != null);
        }

        void CheckMeshMatchesDrawMode(PolySpatialAssetID meshAssetId, SpriteDrawMode drawMode)
        {
            Mesh mesh = PolySpatialCore.LocalAssetManager.GetRegisteredResource<Mesh>(meshAssetId);
            Assert.IsNotNull(mesh, "Mesh should not be null.");

            switch (drawMode)
            {
                case SpriteDrawMode.Simple:
                    Assert.AreEqual(4, mesh.vertexCount,
                        $"Invalid vertex count for {drawMode} mesh. Expected 4, got {mesh.vertexCount}.");
                    Assert.AreEqual(6, mesh.triangles.Length,
                        $"Invalid triangle count for {drawMode} mesh. Expected 6, got {mesh.triangles.Length}.");
                    break;

                case SpriteDrawMode.Sliced:
                    Assert.AreEqual(36, mesh.vertexCount,
                        $"Invalid vertex count for {drawMode} mesh. Expected 36, got {mesh.vertexCount}.");
                    Assert.AreEqual(54, mesh.triangles.Length,
                        $"Invalid triangle count for {drawMode} mesh. Expected 54, got {mesh.triangles.Length}.");
                    break;

                case SpriteDrawMode.Tiled:
                    Assert.AreEqual(36, mesh.vertexCount,
                        $"Invalid vertex count for {drawMode} mesh. Expected 36, got {mesh.vertexCount}.");
                    Assert.AreEqual(54, mesh.triangles.Length,
                        $"Invalid triangle count for {drawMode} mesh. Expected 54, got {mesh.triangles.Length}.");
                    break;
            }
        }

        [UnityTest]
        public IEnumerator Test_SpriteRenderer_Create_Destroy_Data(
            [ValueSource("DrawModeEnum")] SpriteDrawMode drawMode)
        {
            CreateTestObjects();
            PopulateDefaultData(drawMode);

            // Allow for change processing to run
            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsTrue(data.customData.meshId.IsValid());
                CheckMeshMatchesDrawMode(data.customData.meshId, drawMode);
                Assert.IsTrue(data.customData.spriteMaterialId.IsValid());
                Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            }

            var sriid = m_TestComponent.GetInstanceID();

            DestroyAllTestObjects();

            yield return null;

#if UNITY_EDITOR
            {
                var data = PolySpatialComponentUtils.GetSpriteRendererData(sriid);
                var customData = data.customData;
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsFalse(customData.meshId.IsValid());
                Assert.IsFalse(customData.spriteMaterialId.IsValid());
            }
#endif
        }

        [UnityTest]
        public IEnumerator Test_SpriteRenderer_ShadowsOnly()
        {
            CreateTestObjects();
            m_TestComponent.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            
            yield return null;

#if UNITY_EDITOR
            var backingGameObject = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestComponent.gameObject));
            Assert.NotNull(backingGameObject);
            var spriteRenderer = backingGameObject.GetComponent<SpriteRenderer>();
            Assert.Null(spriteRenderer);
#endif
        }

        [UnityTest]
        public IEnumerator Test_SprintRenderer_SetFlip_ChangesMesh(
            [ValueSource("DrawModeEnum")] SpriteDrawMode drawMode, [ValueSource("FlipModeEnum")] (bool, bool) flipMode)
        {
            CreateTestObjects();
            PopulateDefaultData(drawMode);

            yield return null;

            // We only care about scale flipping on X/Y
            var data = PolySpatialComponentUtils.GetSpriteRendererData(m_TestComponent.GetInstanceID());
            var customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(customData.meshId.IsValid(), "Original mesh is valid.");
            Mesh oldMesh = PolySpatialCore.LocalAssetManager.GetRegisteredResource<Mesh>(customData.meshId);
            var oldFlipX = data.customData.flipX;
            var oldFlipY = data.customData.flipY;
            Assert.IsNotNull(oldMesh, "Missing asset for original mesh id.");
            var oldVertices = oldMesh.vertices;
            var oldTriangles = oldMesh.triangles;

            m_TestComponent.flipX = flipMode.Item1;
            m_TestComponent.flipY = flipMode.Item2;

            yield return null;

            Action<string, bool, float, float> FlipCheck = (label, flipped, oldValue, newValue) =>
            {
                float scale = flipped ? -1.0f : 1.0f;
                var valDiff = Math.Abs(oldValue - (scale * newValue));
                Assert.IsTrue(valDiff < 0.00001,
                    $"Flip scale for {label} failed. Expected {oldValue * scale} but got {newValue}.");
            };

            data = PolySpatialComponentUtils.GetSpriteRendererData(m_TestComponent.GetInstanceID());
            customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(customData.meshId.IsValid(), "New mesh id is valid.");
            var newMesh = PolySpatialCore.LocalAssetManager.GetRegisteredResource<Mesh>(customData.meshId);

            Assert.IsNotNull(newMesh, "Missing asset for new mesh asset id.");

            if (oldFlipX == data.customData.flipX && oldFlipY == data.customData.flipY)
            {
                Assert.AreEqual(oldMesh, newMesh, "Mesh should be the same asset.");
            }
            else
            {
                Assert.AreNotEqual(oldMesh, newMesh, "Mesh should not be the same asset.");
            }

            for (int i = 0; i < oldMesh.vertexCount; i++)
            {
                FlipCheck("X", m_TestComponent.flipX, oldVertices[i].x, newMesh.vertices[i].x);
                FlipCheck("Y", m_TestComponent.flipY, oldVertices[i].y, newMesh.vertices[i].y);
            }

            if (flipMode.Item1 != flipMode.Item2)
            {
                Assert.IsTrue(oldTriangles[0] == newMesh.triangles[0] &&
                              oldTriangles[1] == newMesh.triangles[2] &&
                              oldTriangles[2] == newMesh.triangles[1],
                    "Expected triangle ordering to be changed for unpaired flip axis");
            }
            else
            {
                Assert.IsTrue(oldTriangles[0] == newMesh.triangles[0] &&
                              oldTriangles[1] == newMesh.triangles[1] &&
                              oldTriangles[2] == newMesh.triangles[2],
                    "Expected triangle ordering to be unchanged unpaired flip axis");
            }

            m_TestComponent.flipX = false;
            m_TestComponent.flipY = false;

            yield return null;

            data = PolySpatialComponentUtils.GetSpriteRendererData(m_TestComponent.GetInstanceID());
            customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(customData.meshId.IsValid(), "New mesh id is valid.");
            var newOldMesh = PolySpatialCore.LocalAssetManager.GetRegisteredResource<Mesh>(customData.meshId);
            Assert.AreEqual(oldMesh, newOldMesh, "Expected meshes to be the same after resetting flip state.");

            for (int i = 0; i < oldMesh.vertexCount; i++)
            {
                Assert.AreEqual(oldVertices[i], newOldMesh.vertices[i]);
            }

            Assert.IsTrue(oldTriangles[0] == newOldMesh.triangles[0] &&
                          oldTriangles[1] == newOldMesh.triangles[1] &&
                          oldTriangles[2] == newOldMesh.triangles[2],
                "Expected triangle ordering to be unchanged unpaired flip axis");
        }

        [UnityTest]
        public IEnumerator Test_SortingChange_Changes_SortOrder()
        {
            var (goOne, srOne) = CreateLocalTestObjects("Test Sort One");
            var (goTwo, srTwo) = CreateLocalTestObjects("Test Sort Two");

            int srOneIid = srOne.GetInstanceID();
            int srTwoIid = srTwo.GetInstanceID();

            srOne.sortingOrder = 0;
            srTwo.sortingOrder = 1;
            //yield return new WaitForSeconds(1);

            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 0, "Test Sort One");
                CheckSortIndexForSprite(srTwoIid, 1, "Test Sort Two");
            }

            srOne.sortingOrder = 2;
            srTwo.sortingOrder = 1;
            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 1, "Test Sort One");
                CheckSortIndexForSprite(srTwoIid, 0, "Test Sort Two");
            }

            // No guarantee on sort order if the sort keys are the same. All we can do is check that
            // they sorted in some order.
            srOne.sortingOrder = 0;
            srTwo.sortingOrder = 0;
            yield return null;

            {
                var dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
                var dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

                Assert.IsTrue(dataOne.ValidateTrackingFlags());
                Assert.IsTrue(dataTwo.ValidateTrackingFlags());

                var customDataOne = dataOne.customData;
                var customDataTwo = dataTwo.customData;
                Assert.IsNotNull(customDataOne);
                Assert.IsNotNull(customDataTwo);

                Assert.IsTrue(customDataOne.globalSortIndex != customDataTwo.globalSortIndex,
                    "Expected sorted items to be different.");
            }
        }

        [UnityTest]
        public IEnumerator Test_AddDelete_Sprites_SortsCorrectly()
        {
            var (goOne, srOne) = CreateLocalTestObjects("Test Sort One");
            var srOneIid = srOne.GetInstanceID();

            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 0, "Test Sort One");
            }

            var (goTwo, srTwo) = CreateLocalTestObjects("Test Sort Two");
            var srTwoIid = srTwo.GetInstanceID();
            srTwo.sortingOrder = 1;
            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 0, "Test Sort One");
                CheckSortIndexForSprite(srTwoIid, 1, "Test Sort Two");
            }

            var (goThree, srThree) = CreateLocalTestObjects("Test Sort Three");
            var srThreeIid = srThree.GetInstanceID();
            srThree.sortingOrder = 2;
            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 0, "Test Sort One");
                CheckSortIndexForSprite(srTwoIid, 1, "Test Sort Two");
                CheckSortIndexForSprite(srThreeIid, 2, "Test Sort Three");
            }

            DestroyLocalTestObjects(goTwo, srTwo);
            yield return null;

            {
                CheckSortIndexForSprite(srOneIid, 0, "Test Sort One");
                CheckSortIndexForSprite(srThreeIid, 1, "Test Sort Three");
            }
        }

        [UnityTest]
        public IEnumerator Test_SpriteMaskChanges_UpdatesSpriteRenderer()
        {
            CreateTestObjects();
            PopulateDefaultData(SpriteDrawMode.Simple);

            var (m_SpriteTwoGO, m_SpriteTwoSR) = CreateLocalTestObjects("Sprite Two");

            m_TestComponent.sortingOrder = 0;
            m_SpriteTwoSR.sortingOrder = 10;

            var srOneIid = m_TestComponent.GetInstanceID();
            var srTwoIid = m_SpriteTwoSR.GetInstanceID();

            yield return null;

            var dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            var dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            var customDataOne = dataOne.customData;
            var customDataTwo = dataTwo.customData;

            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            // Applying sprite mask with default filtering
            // and no mask interactions includes no sprites.
            var (smGo, smSm) = CreateLocalSpriteMaskTestObjects("Sprite Mask");
            var smIid = smSm.GetInstanceID();

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            // Applying sprite mask with default filtering
            // and set mask interactions includes all sprites.
            m_TestComponent.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            m_SpriteTwoSR.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreEqual(PolySpatialMaskingOperation.MaskInside, customDataOne.maskingOperation);
            Assert.AreEqual(PolySpatialMaskingOperation.MaskOutside, customDataTwo.maskingOperation);

            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            Assert.AreEqual(smIid, customDataOne.appliedSpriteMask.id);
            Assert.AreEqual(smIid, customDataTwo.appliedSpriteMask.id);

            // Applying sprite mask with default filtering
            // and set mask interactions includes no sprites.
            m_TestComponent.maskInteraction = SpriteMaskInteraction.None;
            m_SpriteTwoSR.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreEqual(PolySpatialMaskingOperation.None, customDataOne.maskingOperation);
            Assert.AreEqual(PolySpatialMaskingOperation.MaskOutside, customDataTwo.maskingOperation);

            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            Assert.AreEqual(smIid, customDataTwo.appliedSpriteMask.id);

            m_TestComponent.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            yield return null;

            // Adjusting filter range to custom range still
            // includes all sprites
            smSm.isCustomRangeActive = true;
            smSm.frontSortingOrder = 100;
            smSm.backSortingOrder = 0;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            Assert.AreEqual(smIid, customDataOne.appliedSpriteMask.id);
            Assert.AreEqual(smIid, customDataTwo.appliedSpriteMask.id);

            m_SpriteTwoSR.sortingOrder = 150;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            Assert.AreEqual(smIid, customDataOne.appliedSpriteMask.id);

            // Adjusting range filters out sprites that don't match
            smSm.frontSortingOrder = 200;
            smSm.backSortingOrder = 100;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);
            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);

            Assert.AreEqual(smIid, customDataTwo.appliedSpriteMask.id);

            // Destroying the mask causes all sprites to be unmasked.
            DestroyLocalTestObjects(smGo, smSm);

            yield return null;

            dataOne = PolySpatialComponentUtils.GetSpriteRendererData(srOneIid);
            dataTwo = PolySpatialComponentUtils.GetSpriteRendererData(srTwoIid);

            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());

            customDataOne = dataOne.customData;
            customDataTwo = dataTwo.customData;

            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);
        }

        [UnityTest]
        public IEnumerator Test_SpriteGroups_ApplyMasks_Correctly()
        {
            var (srGoOne, srSrOne) = CreateLocalTestObjects("Masked Sprite One");
            var (srGoTwo, srSrTwo) = CreateLocalTestObjects("Masked Sprite Two");

            var (smGoOne, smSmOne) = CreateLocalSpriteMaskTestObjects("Sprite Mask One");
            var (smGoTwo, smSmTwo) = CreateLocalSpriteMaskTestObjects("Sprite Mask Two");

            var (sgGoZero, sgSgZero) = CreateLocalTestObjects<SortingGroup>("Sprite Group Zero");
            var (sgGoOne, sgSgOne) = CreateLocalTestObjects<SortingGroup>("Sprite Group One");
            var (sgGoTwo, sgSgTwo) = CreateLocalTestObjects<SortingGroup>("Sprite Group Two");

            sgGoOne.transform.parent = sgGoZero.transform;
            sgGoTwo.transform.parent = sgGoZero.transform;

            srSrOne.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            srGoOne.transform.parent = sgGoOne.transform;
            smGoOne.transform.parent = sgGoOne.transform;
            int smOneIid = smSmOne.GetInstanceID();

            srSrTwo.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            srGoTwo.transform.parent = sgGoTwo.transform;
            smGoTwo.transform.parent = sgGoTwo.transform;
            int smTwoIid = smSmTwo.GetInstanceID();

            yield return null;

            var dataOne = PolySpatialComponentUtils.GetTrackingData(srSrOne);
            var customDataOne = dataOne.customData;
            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(smOneIid, customDataOne.appliedSpriteMask.id);

            var dataTwo = PolySpatialComponentUtils.GetTrackingData(srSrTwo);
            var customDataTwo = dataTwo.customData;
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);
            Assert.AreEqual(smTwoIid, customDataTwo.appliedSpriteMask.id);

            srGoTwo.transform.parent = sgGoOne.transform;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetTrackingData(srSrOne);
            customDataOne = dataOne.customData;
            Assert.IsTrue(dataOne.ValidateTrackingFlags());
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);
            Assert.AreEqual(smOneIid, customDataOne.appliedSpriteMask.id);

            dataTwo = PolySpatialComponentUtils.GetTrackingData(srSrTwo);
            customDataTwo = dataTwo.customData;
            Assert.IsTrue(dataTwo.ValidateTrackingFlags());
            Assert.AreNotEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);
            Assert.AreEqual(smOneIid, customDataTwo.appliedSpriteMask.id);

            srGoTwo.transform.parent = null;

            yield return null;

            dataTwo = PolySpatialComponentUtils.GetTrackingData(srSrTwo);
            customDataTwo = dataTwo.customData;
            Assert.AreEqual(PolySpatialInstanceID.None, customDataTwo.appliedSpriteMask);

            srGoOne.transform.parent = null;

            yield return null;

            dataOne = PolySpatialComponentUtils.GetTrackingData(srSrOne);
            customDataOne = dataOne.customData;
            Assert.AreEqual(PolySpatialInstanceID.None, customDataOne.appliedSpriteMask);

        }
    }
}
