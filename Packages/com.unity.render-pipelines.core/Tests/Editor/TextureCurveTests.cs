using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class TextureCurveTests
    {
        // Keys at 0.25 and 0.75; time 0.9 is only reachable through the looping curve.
        static TextureCurve CreateLoopingCurve(float valueAtQuarter, float valueAtThreeQuarters)
        {
            var keys = new[] { new Keyframe(0.25f, valueAtQuarter), new Keyframe(0.75f, valueAtThreeQuarters) };
            return new TextureCurve(keys, 0.5f, true, new Vector2(0f, 1f));
        }

        [Test]
        public void Evaluate_LoopingCurveCreatedByJsonUtility_ReturnsLoopedValue()
        {
            using var original = CreateLoopingCurve(1f, 0f);
            using var copy = JsonUtility.FromJson<TextureCurve>(JsonUtility.ToJson(original));

            Assert.AreEqual(0.216f, copy.Evaluate(0.9f), 1e-5f);
        }

        [Test]
        public void LoopingCurveOverwrittenByJsonUtility_ReturnsOverwrittenValues()
        {
            using var target = CreateLoopingCurve(1f, 0f);
            using var source = CreateLoopingCurve(0f, 1f);
            target.GetTexture();

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), target);

            Assert.AreEqual(source.Evaluate(0.9f), target.Evaluate(0.9f), 1e-5f,
                "Evaluate returned the curve from before the overwrite.");
            CollectionAssert.AreEqual(source.GetTexture().GetPixels(), target.GetTexture().GetPixels(),
                "GetTexture returned the texture from before the overwrite.");
        }
    }
}
