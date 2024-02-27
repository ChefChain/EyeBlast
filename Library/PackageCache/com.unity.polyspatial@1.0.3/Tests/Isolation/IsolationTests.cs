
using System.Diagnostics;
using NUnit.Framework;
using Unity.PolySpatial;
using Tests.Runtime.PolySpatialTest.Base;

namespace Tests.Isolation
{
    /// <summary>
    /// These tests are expected to run in new, clean projects that declare PolySpatial package dependencies but have not yet been configured with any additional
    /// define symbol or flags. Some isolation tests are agnostic of any configuration changes and can be included in any test run. However, If adding any test
    /// that depends on having a clean project and default configuration (ex. PolySpatial Runtime disabled by default), gate them behind INCLUDE_ISOLATION_TESTS
    /// so they are not included in other test runs that may modify project configuration.
    ///
    /// Note that whether the runtime is enabled or disabled is dependent on the platform and project configuration, so can't be tested here. For example,
    /// on visionOS, the runtime is enabled if the current VisionOS XR Provider setting is set to Mixed Reality mode. But that won't be the case
    /// for a brand new clean project.
    /// </summary>
    [TestFixture]
    public class IsolationTests
    {
        [Test]
        [Conditional("INCLUDE_ISOLATION_TESTS")]
        public void Test_01_PolySpatial_Settings_Are_Present()
        {
            Assert.IsNotNull(PolySpatialSettings.instance, "PolySpatial Settings are null");
        }

        [Test]
        [Conditional("INCLUDE_ISOLATION_TESTS")]
        public void Test_02_PolySpatial_Settings_Have_Default_Values()
        {
            var enableClipping = PolySpatialSettings.EnableClipping;
            Assert.IsFalse(enableClipping, "PolySpatial Settings - clipping enabled by default");

            var enableStatistics = PolySpatialSettings.instance.EnableStatistics;
            Assert.IsFalse(enableStatistics, "PolySpatial Settings - statistics enabled by default");

            var disabledTrackers = PolySpatialSettings.instance.DisabledTrackers;
            Assert.IsFalse(disabledTrackers != null && disabledTrackers.Length != 0, "PolySpatial Settings - disabled trackers are not empty by default");

            var runtimeFlags = PolySpatialSettings.RuntimeFlags;
            Assert.AreEqual(0, runtimeFlags, $"PolySpatial Settings - Runtime flags are not empty. ulong value: {runtimeFlags}");
        }
    }
}

