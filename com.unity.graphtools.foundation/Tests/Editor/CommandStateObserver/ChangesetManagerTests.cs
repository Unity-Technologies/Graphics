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

        const int firstVersion = 10;
        const int currentVersion = 128;
        ChangesetManager<TestChangeset> SetupChangesets()
        {
            var cm = new ChangesetManager<TestChangeset>();
            for (uint i = firstVersion; i < currentVersion; i++)
            {
                cm.CurrentChangeset.Sentinel = (int)i;
                cm.PushChangeset(i);
            }

            cm.CurrentChangeset.Sentinel = currentVersion;
            return cm;
        }

        [Test]
        public void Purge0ChangesetRemoveNothing()
        {
            var cm = SetupChangesets();

            // Remove nothing, current changeset not affected
            cm.PurgeOldChangesets(0, currentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, currentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(firstVersion, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(currentVersion - firstVersion + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(currentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeSomeChangesetRemoveExpectedChangesets()
        {
            const int theVersion = 64;

            var cm = SetupChangesets();

            // Remove some, current changeset not affected
            cm.PurgeOldChangesets(theVersion, currentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, currentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(theVersion + 1, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(currentVersion - (theVersion + 1) + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(currentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeAlmostAllChangesetRemoveExpectedChangesets()
        {
            var cm = SetupChangesets();
            // Remove some, current changeset not affected
            cm.PurgeOldChangesets(currentVersion - 2, currentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, currentVersion);

            Assert.IsNotNull(aggregatedChangeset);
            Assert.AreEqual(currentVersion - 1, LastAggregatedChangesetList.First().Sentinel);
            Assert.AreEqual(1 + 1, LastAggregatedChangesetList.Count);
            Assert.AreEqual(currentVersion, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void PurgeAllChangesetRemoveExpectedChangesets()
        {
            var cm = SetupChangesets();

            // Remove all, current changeset cleared
            cm.PurgeOldChangesets(currentVersion, currentVersion);
            var aggregatedChangeset = cm.GetAggregatedChangeset(0, currentVersion);

            Assert.AreEqual(cm.CurrentChangeset, aggregatedChangeset);
            Assert.AreEqual(TestChangeset.clearSentinelValue, cm.CurrentChangeset.Sentinel);
        }

        [Test]
        public void AggregatedChangesetCacheIsReturnedIfVersionsAreTheSame()
        {
            var cm = SetupChangesets();
            var aggregatedChangset1 = cm.GetAggregatedChangeset(0, currentVersion);
            var aggregatedChangset2 = cm.GetAggregatedChangeset(0, currentVersion);
            Assert.IsTrue(ReferenceEquals(aggregatedChangset1, aggregatedChangset2));
        }

        [Test]
        public void AggregatedChangesetCacheIsNotReturnedIfVersionsAreNotTheSame()
        {
            var cm = SetupChangesets();
            var aggregatedChangset1 = cm.GetAggregatedChangeset(0, currentVersion);
            var aggregatedChangset2 = cm.GetAggregatedChangeset(1, currentVersion);
            Assert.IsFalse(ReferenceEquals(aggregatedChangset1, aggregatedChangset2));
        }
    }
}
