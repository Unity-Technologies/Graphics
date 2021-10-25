using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentDatabaseTests
    {
        [Test]
        public void Testing()
        {
            Func<int[], bool> revRevIsOrig = xs => xs.Reverse().SequenceEqual(xs);
            Prop.ForAll(revRevIsOrig).QuickCheckThrowOnFailure();
        }
    }
}
