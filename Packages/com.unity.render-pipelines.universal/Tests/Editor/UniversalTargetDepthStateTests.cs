using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class UniversalTargetDepthStateTests
    {
        static UniversalTarget NewTarget()
        {
            return new UniversalTarget();
        }

        [Test]
        public void OverrideDepthState_AutoAndLEqual_IsFalse()
        {
            var target = NewTarget();
            target.zWriteControl = ZWriteControl.Auto;
            target.zTestMode = ZTestMode.LEqual;

            Assert.IsFalse(target.overrideDepthState);
        }

        [Test]
        public void OverrideDepthState_NonAutoZWrite_IsTrue()
        {
            var target = NewTarget();
            target.zWriteControl = ZWriteControl.ForceEnabled;
            target.zTestMode = ZTestMode.LEqual;

            Assert.IsTrue(target.overrideDepthState);
        }

        [Test]
        public void OverrideDepthState_NonLEqualZTest_IsTrue()
        {
            var target = NewTarget();
            target.zWriteControl = ZWriteControl.Auto;
            target.zTestMode = ZTestMode.Greater;

            Assert.IsTrue(target.overrideDepthState);
        }

        [Test]
        public void OverrideDepthState_SetTrueOnOpaque_ResolvesAutoToForceEnabled()
        {
            var target = NewTarget();
            target.surfaceType = SurfaceType.Opaque;
            target.zWriteControl = ZWriteControl.Auto;
            target.zTestMode = ZTestMode.LEqual;

            target.overrideDepthState = true;

            Assert.AreEqual(ZWriteControl.ForceEnabled, target.zWriteControl);
        }

        [Test]
        public void OverrideDepthState_SetTrueOnTransparent_ResolvesAutoToForceDisabled()
        {
            var target = NewTarget();
            target.surfaceType = SurfaceType.Transparent;
            target.zWriteControl = ZWriteControl.Auto;
            target.zTestMode = ZTestMode.LEqual;

            target.overrideDepthState = true;

            Assert.AreEqual(ZWriteControl.ForceDisabled, target.zWriteControl);
        }

        [Test]
        public void OverrideDepthState_SetFalse_ResetsToAutoAndLEqual()
        {
            var target = NewTarget();
            target.zWriteControl = ZWriteControl.ForceEnabled;
            target.zTestMode = ZTestMode.Greater;

            target.overrideDepthState = false;

            Assert.AreEqual(ZWriteControl.Auto, target.zWriteControl);
            Assert.AreEqual(ZTestMode.LEqual, target.zTestMode);
        }
    }
}
