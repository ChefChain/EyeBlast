using System;
using NUnit.Framework;
using UnityEngine;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;

namespace Tests.Runtime.Functional
{
    [TestFixture]
    public class PolySpatialTypeTests
    {
        [Test]
        public void Test_PolySpatialAssetID_Deserializes_To_Same_PolySpatialAssetID()
        {
            PolySpatialAssetID testId = new PolySpatialAssetID(Guid.NewGuid());

            string assetString = JsonUtility.ToJson(testId);
            PolySpatialAssetID newId = JsonUtility.FromJson<PolySpatialAssetID>(assetString);

            Assert.IsTrue(newId.Equals(testId));
        }

        [Test]
        public void Test_PolySpatialAssetID_ToString()
        {
            var guid = Guid.NewGuid();
            var guidString = guid.ToString();
            var testId = (new PolySpatialAssetID(guidString)).ToGuid();
            var idString = testId.ToString();

            Assert.AreEqual(guid, testId);
            Assert.AreEqual(guidString, idString);
        }
    }
}
