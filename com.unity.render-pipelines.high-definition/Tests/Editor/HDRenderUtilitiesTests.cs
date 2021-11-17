using NUnit.Framework;
using System;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class HDRenderUtilitiesTests
    {
        [Test]
        public void RenderThrowWhenTargetIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => HDRenderUtilities.Render(
                default(CameraSettings),
                default(CameraPositionSettings),
                null
            ));
        }

        [Test]
        public void RenderThrowWhenFrameSettingsIsNull()
        {
            // should throw error that Texture2D is no RenderTexture
            Assert.Throws<ArgumentException>(() => HDRenderUtilities.Render(
                default(CameraSettings),
                default(CameraPositionSettings),
                Texture2D.whiteTexture
            ));
        }

        [Test]
        public void RenderThrowWhenTargetIsNotARenderTextureForTex2DRendering()
        {
            Assert.Throws<ArgumentException>(() => HDRenderUtilities.Render(
                new CameraSettings
                {
                    renderingPathCustomFrameSettings = default
                },
                default(CameraPositionSettings),
                Texture2D.whiteTexture
            ));
        }

        static private void TestAnimationCurveInterp(AnimationCurve lhsCurve, AnimationCurve rhsCurve, float t, float startTime, float endTime, int numSteps, float eps, bool debugPrint)
        {
            AnimationCurve midCurve = new AnimationCurve(lhsCurve.keys);
            KeyframeUtility.InterpAnimationCurve(ref midCurve, rhsCurve, t);

            for (int i = 0; i <= numSteps; i++)
            {
                float timeT = ((float)i) / ((float)numSteps);
                float currTime = Mathf.Lerp(startTime, endTime, timeT);

                float lhsVal = lhsCurve.Evaluate(currTime);
                float rhsVal = rhsCurve.Evaluate(currTime);

                float expectedVal = Mathf.Lerp(lhsVal, rhsVal, t);

                float actualVal = midCurve.Evaluate(currTime);

                float offset = actualVal - expectedVal;
                if (debugPrint)
                {
                    Debug.Log(i.ToString() + ": " + offset.ToString());
                }

                Assert.IsTrue(Mathf.Abs(offset) < eps);
            }
        }

        [Test]
        public void RenderInterpolateAnimationCurve()
        {
            AnimationCurve testCurve0 = new AnimationCurve();
            testCurve0.AddKey(new Keyframe(0.0f, 3.0f, 2.0f, 2.0f));
            testCurve0.AddKey(new Keyframe(4.0f, 2.0f, -1.0f, -1.0f));
            testCurve0.AddKey(new Keyframe(7.0f, 2.6f, -1.0f, -1.0f));

            AnimationCurve testCurve1 = new AnimationCurve();
            testCurve1.AddKey(new Keyframe(-1.0f, 3.0f, 2.0f, 2.0f));
            testCurve1.AddKey(new Keyframe(4.0f, 2.0f, 3.0f, 3.0f));
            testCurve1.AddKey(new Keyframe(5.0f, 2.6f, 0.0f, 0.0f));
            testCurve1.AddKey(new Keyframe(9.0f, 2.6f, -5.0f, -5.0f));

            // Test the same positions as curve 0 but different values and tangents
            AnimationCurve testCurve2 = new AnimationCurve();
            testCurve2.AddKey(new Keyframe(0.0f, 1.0f, -1.0f, 3.0f));
            testCurve2.AddKey(new Keyframe(4.0f, 6.0f, -9.0f, -2.0f));
            testCurve2.AddKey(new Keyframe(7.0f, 5.2f, -3.0f, -4.0f));

            // Test the case where two curves have no overlap
            AnimationCurve testCurve3 = new AnimationCurve();
            testCurve3.AddKey(new Keyframe(11.0f, 1.0f, -1.0f, 3.0f));
            testCurve3.AddKey(new Keyframe(14.0f, 6.0f, -9.0f, -2.0f));
            testCurve3.AddKey(new Keyframe(17.0f, 5.2f, -3.0f, -4.0f));

            TestAnimationCurveInterp(testCurve0, testCurve1, 0.5f, -2.0f, 10.0f, 100, 1e-5f,false);
            TestAnimationCurveInterp(testCurve1, testCurve2, 0.5f, -2.0f, 10.0f, 100, 1e-5f,false);
            TestAnimationCurveInterp(testCurve0, testCurve2, 0.5f, -2.0f, 10.0f, 100, 1e-5f, false);
            TestAnimationCurveInterp(testCurve0, testCurve3, 0.5f, -2.0f, 25.0f, 100, 1e-5f, false);
        }

    }
}
