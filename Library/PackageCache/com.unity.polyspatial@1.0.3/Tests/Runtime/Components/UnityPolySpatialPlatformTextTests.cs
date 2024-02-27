using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using TMPro;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.Functional.Components
{
    public class UnityPolySpatialPlatformTextTests : PooledComponentTestBase<VisionOSNativeText>
    {
        const string k_TestText = "This is test text";
        const string k_TestText2 = "This is test text 2";

        void CreateTestObjects()
        {
            CreateTestObjects("Text Test Objects");
        }

#if UNITY_EDITOR
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
        public IEnumerator Test_UnityPolySpatialText_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var sriid = m_TestComponent.GetInstanceID();

            DestroyAllTestObjects();

            yield return null;

        }

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialText_Disable_Tracking()
        {
            CreateTestObjects();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            m_TestComponent.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));
        }

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialText_ChangedText_IsReflected()
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            var iid = data.InstanceId;

            var text = VisionOSNativeTextTracker.GetStringForInstance(iid);
            Assert.IsFalse(String.CompareOrdinal(text, k_TestText) == 0);
            Assert.IsFalse(String.CompareOrdinal(text, k_TestText2) == 0);

            m_TestComponent.Text = k_TestText;

            yield return null;

            text = VisionOSNativeTextTracker.GetStringForInstance(iid);
            Assert.IsFalse(String.IsNullOrEmpty(text));
            Assert.AreEqual(0, String.CompareOrdinal(text, m_TestComponent.Text));
            Assert.AreEqual(0, String.CompareOrdinal(text, k_TestText));

            m_TestComponent.Text = k_TestText2;

            yield return null;

            text = VisionOSNativeTextTracker.GetStringForInstance(iid);
            Assert.AreEqual(0, String.CompareOrdinal(text, m_TestComponent.Text));
            Assert.AreEqual(0, String.CompareOrdinal(text, k_TestText2));

            DestroyAllTestObjects();

            yield return null;

            text = VisionOSNativeTextTracker.GetStringForInstance(iid);
            Assert.IsTrue(String.IsNullOrEmpty(text));
        }

        // Boxing FTW. Because generice don't have any real ability
        // to generate type generic code, we have to box the values
        // so that we can reuse functions generically.
        static IEnumerator<(object, object)> TestColors()
        {
            yield return (Color.red, Color.red);
            yield return (Color.blue, Color.blue);
        }

        static IEnumerator<(object, object)> TestFontSize()
        {
            yield return (18, 18);
            yield return (37, 37);
        }

        static IEnumerator<(object, object)> TestCornerRadius()
        {
            yield return (1, 1);
            yield return (30, 30);
        }

        static IEnumerator<(object, object)> TestTextMargin()
        {
            yield return (new Vector4(10, 0, 10, 0), new Vector4(10, 0, 10, 0));
            yield return (new Vector4(100, 100, 10, 10), new Vector4(100, 100, 10, 10));
            yield return (new Vector4(-1, -1, -1, -1), new Vector4(0, 0, 0, 0));
        }

        static IEnumerator<(object, object)> TestCanvasSize()
        {
            yield return (new Vector2(10, 10), new Vector2(10, 10));
            yield return (new Vector2(100, 100), new Vector2(100, 100));
            yield return (new Vector2(-1, -1), new Vector2(1, 1));
        }

        static IEnumerator<(object, object)> TestTmpFont()
        {
            var fontOne = Resources.Load("Fonts & Materials/LiberationSans SDF");
            var fontOneAssetId = PolySpatialCore.LocalAssetManager.Register(fontOne);
            yield return (fontOne, fontOneAssetId);

            var fontTwo = Resources.Load("Symbol SDF");
            var fontTwoAssetId = PolySpatialCore.LocalAssetManager.Register(fontTwo);
            yield return (fontTwo, fontTwoAssetId);
        }

        public struct TestData
        {
            public string componentPropertyName;
            public string dataPropertyName;
            public Type propertyType;
            public Type expectedType;
            public Func<IEnumerator<(object, object)>> testValues;

            public override string ToString()
            {
                return $"{componentPropertyName}";
            }
        }

        static TestData[] s_TestValues = new TestData[]
        {
            new TestData() { componentPropertyName = "TextColor", dataPropertyName = "textColor", expectedType = typeof(Color), testValues = TestColors },
            new TestData() { componentPropertyName = "TextSize", dataPropertyName = "textSize", expectedType = typeof(int), testValues = TestFontSize },
            new TestData() { componentPropertyName = "TextEdgeInsets", dataPropertyName = "textEdgeInsets", expectedType = typeof(Vector4), testValues = TestTextMargin },
            new TestData() { componentPropertyName = "CanvasBackgroundColor", dataPropertyName = "canvasBackgroundColor", expectedType = typeof(Color), testValues = TestColors },
            new TestData() { componentPropertyName = "CanvasSize", dataPropertyName = "canvasSize", expectedType = typeof(Vector2), testValues = TestCanvasSize },
            new TestData() { componentPropertyName = "CanvasCornerRadius", dataPropertyName = "canvasCornerRadius", expectedType = typeof(int), testValues = TestCornerRadius },
            new TestData() { componentPropertyName = "TmProFontAsset", dataPropertyName = "tmProFontAssetId", propertyType = typeof(TMP_FontAsset), expectedType = typeof(PolySpatialAssetID), testValues = TestTmpFont },
        };

        [UnityTest]
        public IEnumerator Test_UnityPolySpatialText_ChangeProperty_IsReflected([ValueSource(nameof(s_TestValues))]TestData prop)
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
            var iid = data.InstanceId;

            var property = m_TestComponent.GetType().GetProperty(prop.componentPropertyName);
            var dataProperty = data.customData.GetType().GetField(prop.dataPropertyName);

            Assert.IsNotNull(property);
            Assert.AreEqual(prop.propertyType != null ? prop.propertyType : prop.expectedType, property.PropertyType);
            Assert.AreEqual(prop.expectedType, dataProperty.FieldType);

            var val = property.GetValue(m_TestComponent);
            var testValues = prop.testValues();

            while (testValues.MoveNext())
            {
                (object, object) tuple = testValues.Current;
                property.SetValue(m_TestComponent, tuple.Item1);
                yield return null;

                data = PolySpatialComponentUtils.GetTrackingData(m_TestComponent);
                if (prop.propertyType != null)
                {
                    Assert.AreEqual(tuple.Item1, property.GetValue(m_TestComponent));
                    Assert.AreEqual(tuple.Item2, dataProperty.GetValue(data.customData));
                }
                else
                {
                    Assert.AreEqual(tuple.Item2, property.GetValue(m_TestComponent));
                    Assert.AreEqual(dataProperty.GetValue(data.customData), property.GetValue(m_TestComponent));
                }
            }

        }
#endif
    }
}
