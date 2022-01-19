using NUnit.Framework;
using System.Threading;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Utils.Tests
{
    public class TimedScopeTests
    {
        [Test]
        public unsafe void SimpleTimeCheck()
        {
            double stripTimeMs = 0;
            using (TimedScope.FromPtr(&stripTimeMs))
            {
                Thread.Sleep(1000);
            }
            Assert.AreEqual(stripTimeMs, 1000, 20);
        }
    }
}
