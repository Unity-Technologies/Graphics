using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class AnimationCurveParameterTests
    {
        [Test]
        public void Clone_PreservesKeysAndWrapModes()
        {
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.PingPong;
            var source = new AnimationCurveParameter(curve, true);

            var clone = (AnimationCurveParameter)source.Clone();

            Assert.AreEqual(source.overrideState, clone.overrideState);
            Assert.AreEqual(WrapMode.Loop, clone.value.preWrapMode);
            Assert.AreEqual(WrapMode.PingPong, clone.value.postWrapMode);
            Assert.AreEqual(source.value.keys, clone.value.keys);
            Assert.AreEqual(source.value.Evaluate(-0.25f), clone.value.Evaluate(-0.25f), 1e-5f);
            Assert.AreEqual(source.value.Evaluate(1.25f), clone.value.Evaluate(1.25f), 1e-5f);
        }

        [Test]
        public void Clone_DoesNotShareCurveInstance()
        {
            var source = new AnimationCurveParameter(AnimationCurve.Linear(0f, 0f, 1f, 1f));

            var clone = (AnimationCurveParameter)source.Clone();
            clone.value.postWrapMode = WrapMode.Loop;
            clone.value.AddKey(0.5f, 2f);

            Assert.AreNotSame(source.value, clone.value);
            Assert.AreEqual(WrapMode.ClampForever, source.value.postWrapMode);
            Assert.AreEqual(2, source.value.length);
        }
    }
}
