using System;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

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

        // TODO: search variations with example nested layers of paths, finding top-level stuff, mid level stuff ...
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

        [Test, Performance]
        public void NearWorstCaseSearchPerfTest()
        {
            var sampleGroup = new SampleGroup("Scope");
            for (int i = 0; i < 100; ++i)
            {
                const int x = 100;
                for (int j = 0; j < x; ++j)
                {
                    m_DataStore.AddData($"foo {i*x + j}");
                }

                // Always searches near the end of the 100 elements we just added
                var randInt = ((i + 1) * x) - 2;
                var rootNode = m_DataStore.LayerList.First().root;
                using (Measure.Scope(sampleGroup))
                {
                    m_DataStore.SearchRelative(rootNode, $"foo {randInt}");
                    m_DataStore.SearchRelative(rootNode, $"foo {randInt - 5}");
                    m_DataStore.SearchRelative(rootNode, $"foo {randInt - 10}");
                    m_DataStore.SearchRelative(rootNode, $"foo {randInt - 15}");
                    m_DataStore.SearchRelative(rootNode, $"foo {randInt - 20}");
                }
            }
        }

        // TODO: Investigate how to test the case where we are:
        // 1) adding 100 elements to store
        // 2) Searching 12/100/X times <-- Measure this
        // 3) GoTo 1
        // How data store handles searches for a scaling data set

        [Test, Performance]
        public void AddLayerToStorePerfTest()
        {
            Measure.Method(
                    () =>
                    {
                        m_DataStore.AddNewTopLayer("foo");
                    }).
                WarmupCount(5).
                MeasurementCount(20).
                IterationsPerMeasurement(1000).
                GC().
                Run();
        }
    }
}
