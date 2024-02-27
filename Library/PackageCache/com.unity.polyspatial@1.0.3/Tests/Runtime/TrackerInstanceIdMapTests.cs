using NUnit.Framework;
using Unity.PolySpatial.Internals;

namespace Tests.Runtime.Functional
{
    [TestFixture]
    public class TrackerInstanceIdMapTests
    {
        [Test]
        public void Test_Indexer_Adds_To_Count()
        {
            TrackerInstanceIdMap<DefaultTrackingData> map = default;
            map.Initialize(1);
            try
            {
                Assert.AreEqual(0, map.Count);

                map[0] = new DefaultTrackingData();

                Assert.AreEqual(1, map.Count);
            }
            finally
            {
                map.Dispose();
            }
        }
    }
}
