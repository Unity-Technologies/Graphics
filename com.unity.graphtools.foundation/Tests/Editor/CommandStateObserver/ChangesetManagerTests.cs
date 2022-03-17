using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class ChangesetManagerTests
    {
        static IReadOnlyList<TestChangeset> LastAggregatedChangesetList { get; set; }

        class TestChangeset : IChangeset
        {
            public const int defaultSentinelValue = -42;
            public const int clearSentinelValue = -4242;

            public int Sentinel { get; set; } = defaultSentinelValue;

            /// <inheritdoc />
            public void Clear()
            {
                Sentinel = clearSentinelValue;
            }

            /// <inheritdoc />
            public void AggregateFrom(IEnumerable<IChangeset> changesets)
            {
                LastAggregatedChangesetList = changesets.Cast<TestChangeset>().ToList();
            }
        }

        [Test]
        public void PushChangesetResetCurrentChangeset()
        {
            var cm = new ChangesetManager<TestChangeset>();
            cm.CurrentChangeset.Sentinel = TestChangeset.defaultSentinelValue + 100;
            cm.PushChangeset(22);
            Assert.AreEqual(TestChangeset.defaultSentinelValue, cm.CurrentChangeset.Sentinel);
        }

        const int k_FirstVersion = 10;
        const int k_CurrentVersion = 128;
        ChangesetManager<TestChangeset> SetupChangesets()
        {
            var cm = new ChangesetManager<TestChangeset>();
            for (uint i = k_FirstVersion; i < k_CurrentVersion; i++)
            {
                cm.CurrentChangeset.Sentinel = (int)i;
                cm.PushChangeset(i);
            }

            cm.CurrentChangeset.Sentinel = k_CurrentVersion;
            return cm;
        }

        [Test]
        public void Purge0ChangesetRemoveNothing()
        {
            var cm = SetupChangesets();

            // Remove nothing, current changeset not affected
            cm.PurgeOldChangesets(0, k_CurrentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, k_CurrentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(k_FirstVersion, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(k_CurrentVersion - k_FirstVersion + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(k_CurrentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeSomeChangesetRemoveExpectedChangesets()
        {
            const int theVersion = 64;

            var cm = SetupChangesets();

            // Remove some, current changeset not affected
            cm.PurgeOldChangesets(theVersion, k_CurrentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, k_CurrentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(theVersion + 1, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(k_CurrentVersion - (theVersion + 1) + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(k_CurrentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeAlmostAllChangesetRemoveExpectedChangesets()
        {
            var cm = SetupChangesets();
            // Remove some, current changeset not affected
            cm.PurgeOldChangesets(k_CurrentVersion - 2, k_CurrentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, k_CurrentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(k_CurrentVersion - 1, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(1 + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(k_CurrentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeAllChangesetRemoveExpectedChangesets()
        {
            var cm = SetupChangesets();

            // Remove all, current changeset cleared
            cm.PurgeOldChangesets(k_CurrentVersion, k_CurrentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, k_CurrentVersion);

            Assert.AreEqual(cm.CurrentChangeset, aggregatedChangeset);
            Assert.AreEqual(TestChangeset.clearSentinelValue, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void AggregatedChangesetCacheIsReturnedIfVersionsAreTheSame()
        {
            var cm = SetupChangesets();
            var aggregatedChangset1 = cm.GetAggregatedChangeset(0, k_CurrentVersion);
            var aggregatedChangset2 = cm.GetAggregatedChangeset(0, k_CurrentVersion);
            Assert.IsTrue(ReferenceEquals(aggregatedChangset1, aggregatedChangset2));
        }

        [Test]
        public void AggregatedChangesetCacheIsNotReturnedIfVersionsAreNotTheSame()
        {
            var cm = SetupChangesets();
            var aggregatedChangset1 = cm.GetAggregatedChangeset(0, k_CurrentVersion);
            var aggregatedChangset2 = cm.GetAggregatedChangeset(1, k_CurrentVersion);
            Assert.IsFalse(ReferenceEquals(aggregatedChangset1, aggregatedChangset2));
        }
    }
}
