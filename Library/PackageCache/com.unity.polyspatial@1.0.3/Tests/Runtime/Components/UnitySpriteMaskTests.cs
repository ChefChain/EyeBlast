using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.Functional.Components
{


    public class UnitySpriteMasksTests : PooledComponentTestBase<SpriteMask>
    {
        const string k_TestMaxLayerName = "Test Max Layer";
        const string k_DefaultLayerName = "Default";
#if UNITY_EDITOR
        SerializedObject m_TagManager;
#endif

        internal override void InternalSetup()
        {
#if UNITY_EDITOR
            m_TagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
#endif
            base.InternalSetup();
        }

        internal override void InternalTearDown()
        {
#if UNITY_EDITOR
            m_TagManager = null;
#endif
            base.InternalTearDown();
        }

        void CreateTestObjects()
        {
            CreateTestObjects("Sprite Mask Test");
        }

        void PopulateDefautlData()
        {
            var texture = new Texture2D(256, 256);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100, 0,
                SpriteMeshType.FullRect, new Vector4(64, 64, 64, 64));

            m_TestComponent.sprite = sprite;
            m_TestComponent.alphaCutoff = 0.5f;
        }

        [UnityTest]
        public IEnumerator Test_SpriteMask_Create_Destroy_Tracking()
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
            data = PolySpatialComponentUtils.GetSpriteMaskData(sriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_SpriteMask_NoSpriteOrTexture_NoTextureAssetId()
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            Assert.IsFalse(data.customData.spriteMaskTextureId.IsValid());

            var iid = m_TestComponent.GetInstanceID();

            m_TestComponent.sprite = Sprite.Create(null, new(0, 0, 1, 1), Vector2.zero);

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.customData.spriteMaskTextureId.IsValid());

            DestroyAllTestObjects();

            yield return null;

#if UNITY_EDITOR
            data = PolySpatialComponentUtils.GetSpriteMaskData(iid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.customData.spriteMaskTextureId.IsValid());
#endif
        }

        [UnityTest]
        public IEnumerator Test_SpriteMask_CreateDestroy_AllData()
        {
            CreateTestObjects();
            PopulateDefautlData();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            Assert.IsTrue(data.customData.spriteMaskTextureId.IsValid());

            var iid = m_TestComponent.GetInstanceID();

            DestroyAllTestObjects();

            yield return null;

#if UNITY_EDITOR
            data = PolySpatialComponentUtils.GetSpriteMaskData(iid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.customData.spriteMaskTextureId.IsValid());
#endif
        }

        [UnityTest]
        public IEnumerator Test_SpriteMask_AlphaCutoff_UpdatesCorrectly()
        {
            CreateTestObjects();
            PopulateDefautlData();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            Assert.IsTrue(MathExtensions.ApproximatelyEqual(m_TestComponent.alphaCutoff, data.customData.alphaOpacityCutoff));

            m_TestComponent.alphaCutoff = 0.2f;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(MathExtensions.ApproximatelyEqual(m_TestComponent.alphaCutoff, data.customData.alphaOpacityCutoff));

            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator Test_SpriteMask_SortingLayer_InEditor()
        {
            SortingLayer frontFilteringLayer;
            if (SortingLayer.layers.Length < 2)
            {
                UnityLayers.AddSortingLayer(m_TagManager, k_TestMaxLayerName);
                frontFilteringLayer = (from sl in SortingLayer.layers
                    where sl.name == k_TestMaxLayerName
                    select sl).First();

                Assert.IsNotNull(frontFilteringLayer);
            }
            else
            {
                frontFilteringLayer = SortingLayer.layers[SortingLayer.layers.Length - 1];
            }

            var defaultLayerValue = SortingLayer.GetLayerValueFromName(k_DefaultLayerName);
            var frontFilteringLayerKey = new PolySpatialSortingLayerKey((short)frontFilteringLayer.value, 0);
            var baclFilteringLayerKey = new PolySpatialSortingLayerKey((short)defaultLayerValue, 0);

            CreateTestObjects();
            PopulateDefautlData();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            var customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialSortingLayerKey.s_MaxSortingLayerKey, customData.frontFilteringSortingLayerKey);
            Assert.AreEqual(PolySpatialSortingLayerKey.s_MinSortingLayerKey, customData.backFilteringSortingLayerKey);

            m_TestComponent.isCustomRangeActive = true;
            m_TestComponent.frontSortingLayerID = frontFilteringLayer.id;
            m_TestComponent.backSortingLayerID = SortingLayer.NameToID(k_DefaultLayerName);

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(frontFilteringLayerKey, customData.frontFilteringSortingLayerKey);
            Assert.AreEqual(baclFilteringLayerKey, customData.backFilteringSortingLayerKey);

            m_TestComponent.frontSortingOrder = -100;
            m_TestComponent.backSortingOrder = 100;

            yield return null;

            frontFilteringLayerKey = new PolySpatialSortingLayerKey((short)frontFilteringLayer.value, -100);
            baclFilteringLayerKey = new PolySpatialSortingLayerKey((short)defaultLayerValue, 100);

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(frontFilteringLayerKey, customData.frontFilteringSortingLayerKey);
            Assert.AreEqual(baclFilteringLayerKey, customData.backFilteringSortingLayerKey);

            m_TestComponent.isCustomRangeActive = false;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            customData = data.customData;
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialSortingLayerKey.s_MaxSortingLayerKey, customData.frontFilteringSortingLayerKey);
            Assert.AreEqual(PolySpatialSortingLayerKey.s_MinSortingLayerKey, customData.backFilteringSortingLayerKey);

            DestroyAllTestObjects();

            yield return null;
        }
#endif
    }
}
