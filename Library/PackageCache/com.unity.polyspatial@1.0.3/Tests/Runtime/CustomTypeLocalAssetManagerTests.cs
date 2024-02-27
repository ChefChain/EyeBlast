using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using Tests.Runtime.PolySpatialTest.Base;

// If Flare becomes a tracked type, this test will need to be updated with another random type
using TypeToUseForTest = UnityEngine.Flare;

namespace Tests.Runtime.Functional
{
    public class CustomTypeLocalAssetManagerTests
    {
        [UnityTest, PolySpatialRuntimeSetup]
        public IEnumerator CorrectlyRegisteredTypeWorks()
        {
            var assetManager = PolySpatialCore.LocalAssetManager;

            Assert.IsFalse(LocalAssetManager.TrackedAssetTypes.Contains(typeof(TypeToUseForTest)), $"{nameof(LocalAssetManager)} should not track {typeof(TypeToUseForTest).Name} type by default");

            var theAsset = new TypeToUseForTest();

            Assert.That(() => assetManager.Register(theAsset), Throws.Exception, "Registering an Asset of an untracked type should throw");

            assetManager.AddTrackedAssetType(typeof(TypeToUseForTest), LocalAssetManager.TrackingMode.Replicated);

            assetManager.Register(theAsset);

            var theAssetFiltered = false;
            assetManager.AssetChangeProcessingFilter = (obj, _) =>
            {
                // Pass all non-TypeToUseForTests
                if (obj.GetType() != typeof(TypeToUseForTest))
                    return true;

                if (obj == theAsset)
                    theAssetFiltered = true;

                // Stop all TypeToUseForTests
                return false;
            };

            yield return null;

            Assert.IsTrue(theAssetFiltered, "The TypeToUseForTest Asset didn't appear in the filter callback");
        }
    }
}
