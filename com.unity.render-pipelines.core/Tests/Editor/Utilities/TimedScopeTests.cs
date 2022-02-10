using NUnit.Framework;
using System.Threading;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Utils.Tests
{
    public class TimedScopeTests
    {
        private const int k_MillisecondsTimeout = 1000;
        private const int k_TimeComparisonDelta = 20;

        [Test]
        public unsafe void SimpleTimeCheckFromPtr()
        {
            double stripTimeMs = 0;
            using (TimedScope.FromPtr(&stripTimeMs))
            {
                Thread.Sleep(k_MillisecondsTimeout);
            }
            Assert.AreEqual(stripTimeMs, k_MillisecondsTimeout, k_TimeComparisonDelta);
        }

        [Test]
        public void SimpleTimeCheckFromRef()
        {
            double stripTimeMs = 0;
            using (TimedScope.FromRef(ref stripTimeMs))
            {
                Thread.Sleep(k_MillisecondsTimeout);
            }
            Assert.AreEqual(stripTimeMs, k_MillisecondsTimeout, k_TimeComparisonDelta);
        }
    }
}
