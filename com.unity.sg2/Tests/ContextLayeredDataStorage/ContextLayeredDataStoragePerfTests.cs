using NUnit.Framework;
using Unity.PerformanceTesting;

namespace UnityEditor.ContextLayeredDataStorage
{
    [TestFixture]
    public class CLDSPerfTests
    {
        TestStorage m_DataStore;
        [SetUp]
        public void PerTestSetUp()
        {
            m_DataStore = new TestStorage();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_DataStore = null;
        }

        [Test, Performance]
        public void CreateDataStorePerfTest()
        {
            Measure.Method(
                () =>
                {
                    TestStorage storage = new TestStorage();
                }).
                WarmupCount(5).
                MeasurementCount(20).
                IterationsPerMeasurement(1000).
                GC().
                Run();
        }

        [Test, Performance]
        public void AddUniquePathsToDataStorePerfTest()
        {
            int i = 0;
            Measure.Method(
                () =>
                {
                    m_DataStore.AddData($"foo {i}", 10);
                    i++;
                }).
                WarmupCount(5).
                MeasurementCount(20).
                IterationsPerMeasurement(1000).
                GC().
                Run();

            // Clear the data store each time
            m_DataStore = new TestStorage();
        }

        [Test, Performance]
        public void AddIdenticalPathsToDataStorePerfTest()
        {
            int i = 0;
            Measure.Method(
                    () =>
                    {
                        m_DataStore.AddData("foo", 10);
                        i++;
                    }).
                WarmupCount(5).
                MeasurementCount(20).
                IterationsPerMeasurement(1000).
                GC().
                Run();
        }

        // Can do lots of search variations with example nested layers of paths, finding top-level stuff, mid level stuff ...
        [Test, Performance]
        public void SearchDataStorePerfTest()
        {
            for (var i = 0; i < 1000; i++)
            {
                m_DataStore.AddData($"foo {i}");
            }

            Measure.Method(
                    () =>
                    {
                        m_DataStore.Search("foo 0");
                    }).
                WarmupCount(5).
                MeasurementCount(20).
                IterationsPerMeasurement(1000).
                GC().
                Run();
        }
    }
}
