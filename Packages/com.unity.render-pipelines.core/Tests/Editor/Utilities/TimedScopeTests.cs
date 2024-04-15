using System.Diagnostics;
using NUnit.Framework;
using System.Threading;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Utils.Tests
{
    class TimedScopeTests
    {
        private const int k_MillisecondsTimeout = 1000;
        private const int k_TimeComparisonDelta = 20;


        [Test]
        [Ignore("Test is not stable for CI.")]
        public unsafe void SimpleTimeCheckFromPtr()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            double timedScopeTime = 0;
            using (TimedScope.FromPtr(&timedScopeTime))
            {
                Thread.Sleep(k_MillisecondsTimeout);
            }

            stopwatch.Stop();
            double stopWatchTime = stopwatch.Elapsed.TotalMilliseconds;
            Assert.AreEqual(stopWatchTime, timedScopeTime, k_TimeComparisonDelta);
        }

        [Test]
        [Ignore("Test is not stable for CI.")]
        public void SimpleTimeCheckFromRef()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            double timedScopeTime = 0;
            using (TimedScope.FromRef(ref timedScopeTime))
            {
                Thread.Sleep(k_MillisecondsTimeout);
            }

            stopwatch.Stop();
            double stopWatchTime = stopwatch.Elapsed.TotalMilliseconds;
            Assert.AreEqual(stopWatchTime, timedScopeTime, k_TimeComparisonDelta);
        }
    }
}
