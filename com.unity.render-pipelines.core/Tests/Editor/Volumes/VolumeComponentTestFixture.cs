using NUnit.Framework;

#if FSCHECK
using FsCheck;
using UnityEngine.TestTools.FsCheckExtensions;
#endif

namespace UnityEngine.Rendering.Tests
{
    [TestFixture]
    public class VolumeComponentTestFixture
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
#if FSCHECK
            ArbX.Register();
#endif
        }
    }
}
