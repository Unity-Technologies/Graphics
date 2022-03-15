using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UnityEngine.TestTools.Graphics.Tests
{
#if TEST_FRAMEWORK_1_2_0_OR_NEWER
    [RequiresPlayMode]
#endif
    public class ImageAssertTests
    {
        [Test]
        public void AreEqual_WithNullCamera_ThrowsArgumentNullException()
        {
            Assert.That(() => ImageAssert.AreEqual(new Texture2D(1, 1), (Camera)null), Throws.ArgumentNullException);
        }

        [Test]
        public void AreEqual_WithNullCameras_ThrowsArgumentNullException()
        {
            Assert.That(() => ImageAssert.AreEqual(new Texture2D(1, 1), (IEnumerable<Camera>)null), Throws.ArgumentNullException);
        }

        [Test]
        public void AreEqual_WithNullActualImage_ThrowsArgumentNullException()
        {
            Assert.That(() => ImageAssert.AreEqual(new Texture2D(1, 1), (Texture2D)null), Throws.ArgumentNullException);
        }

        [Test]
        public void AreEqual_WithIdenticalImage_Succeeds()
        {
            var testImage = new Texture2D(64, 64);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = i % 2 == 1 ? Color.black : Color.white;
            testImage.SetPixels32(pixels);
            testImage.Apply(false);

            Assert.That(() => ImageAssert.AreEqual(testImage, testImage), Throws.Nothing);
        }

        [Test]
        public void AreEqual_WithTotallyDifferentImages_ThrowsAssertionException()
        {
            Assert.That(() => ImageAssert.AreEqual(Texture2D.whiteTexture, Texture2D.blackTexture), Throws.InstanceOf<AssertionException>());
        }

        [Test]
        public void AreEqual_WithSlightlyDifferentImages_SucceedsWithAppropriateTolerance()
        {
            var expected = Texture2D.blackTexture;
            var actual = new Texture2D(expected.width, expected.height);
            var pixels = new Color32[actual.width * actual.height];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = new Color32(0x01, 0x01, 0x01, 0x01);
            actual.SetPixels32(pixels);
            actual.Apply(false);

            Assert.That(() => ImageAssert.AreEqual(expected, actual), Throws.InstanceOf<AssertionException>());
            Assert.That(() => ImageAssert.AreEqual(expected, actual, new ImageComparisonSettings { PerPixelCorrectnessThreshold = 0.005f }), Throws.Nothing);
        }

        [Test]
        public void AreEqual_WidthDifferentSizeImages_ThrowsAssertionException()
        {
            var c = Color.black;

            var expected = new Texture2D(1, 1);
            expected.SetPixels(new[] { c });
            expected.Apply(false);

            var actual = new Texture2D(1, 2);
            actual.SetPixels(new[] { c, c });
            actual.Apply(false);

            Assert.That(() => ImageAssert.AreEqual(expected, actual), Throws.InstanceOf<AssertionException>());
        }

        [Test]
        public void AreEqual_DestinationTextureResolution()
        {
            Texture2D source = new Texture2D(1920, 1080);

            Texture2D destination = new Texture2D(640, 360);

            Texture2D newDestination = ImageAssert.ResizeInto(source, destination);

            Assert.AreEqual(destination.height, newDestination.height);
            Assert.AreEqual(destination.width, newDestination.width);
        }

        [Test]
        public void AreEqual_TexturesAfterResize()
        {
            var sourceImage = new Texture2D(1920, 1080);
            var pixels = new Color32[1920 * 1080];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = i % 2 == 1 ? Color.magenta : Color.black;
            sourceImage.SetPixels32(pixels);
            sourceImage.Apply(false);

            var expected = new Texture2D(640, 360);
            var pixels2 = new Color32[640 * 360];
            for (int i = 0; i < pixels2.Length; ++i)
                pixels2[i] = i % 2 == 1 ? Color.magenta : Color.black;
            expected.SetPixels32(pixels2);
            expected.Apply(false);

            Texture2D actual;
            actual = ImageAssert.ResizeInto(sourceImage, expected);

            Assert.That(() => ImageAssert.AreEqual(expected, actual), Throws.Nothing);
        }

        // TestCaseSource may not be supported on some devices.
#if UNITY_EDITOR
        public class PerPixelTestSource
        {
            const int k_Size = 64;
            const float k_AlphaThreshold = 10f / 255;
            const float k_GammaThreshold = 10f / 255;

            static TestCaseData Next(float incorrectPixelsThreshold, Color32 alteredColor, int alteredCount, bool throws, string name,
                ImageComparisonSettings.PixelTests pixelTests = ImageComparisonSettings.PixelTests.DeltaGamma|ImageComparisonSettings.PixelTests.DeltaAlpha,
                ImageComparisonSettings.ImageTests imageTests = ImageComparisonSettings.ImageTests.IncorrectPixelsCount)
            {
                return new TestCaseData(
                    new ImageComparisonSettings
                    {
                        TargetWidth = k_Size, TargetHeight = k_Size,
                        PerPixelAlphaThreshold = k_AlphaThreshold,
                        PerPixelGammaThreshold = k_GammaThreshold,
                        IncorrectPixelsThreshold = incorrectPixelsThreshold,
                        ActiveImageTests = imageTests,
                        ActivePixelTests = pixelTests,
                    },
                    (Color32)Color.clear, alteredColor, alteredCount, throws
                ).SetName($"PerPixel_{name}_{(throws ? "Throws" : "Succeeds")}");
            }

            public static IEnumerable TestCases
            {
                get
                {
                    int count = k_Size * k_Size;

                    Color32 valid = new Color32(9, 9, 9, 9);
                    Color32 badColor = new Color32(11, 11, 11, 9);
                    Color32 badAlpha = new Color32(9, 9, 9, 11);
                    Color32 badBoth = new Color32(11, 11, 11, 11);

                    var testColor = ImageComparisonSettings.PixelTests.DeltaGamma;
                    var testAlpha = ImageComparisonSettings.PixelTests.DeltaAlpha;
                    var testBoth = testColor | testAlpha;

                    // The following tests focus on how bad pixels are counted: how we count pixels, when they should
                    // be counted or not, and when they should trigger failures.
                    {
                        // AllGood
                        yield return Next(0, Color.clear, 0, false, "Identical");

                        // Make sure that the test is actually disabled when we disable it one way or another.
                        // The following tests fill a texture with bad pixels and should succeed nonetheless.
                        yield return Next(0, badBoth, count, false, "DisabledCount",
                            ImageComparisonSettings.PixelTests.None, ImageComparisonSettings.ImageTests.IncorrectPixelsCount);
                        yield return Next(0, badBoth, count, false, "DisabledGammaAlpha",
                            testColor | testAlpha, ImageComparisonSettings.ImageTests.None);

                        // Make sure that one test being enabled doesn't enable the other test.
                        yield return Next(0, badColor, count, false, "BadColor_TestAlpha", testAlpha);
                        yield return Next(0, badAlpha, count, false, "BadAlpha_TestColor", testColor);

                        // Test the pixel count thresholds.
                        yield return Next(32f / count, badColor, 31, false, "31BadColor_Max32Bad", testColor);
                        yield return Next(32f / count, badColor, 32, false, "32BadColor_Max32Bad", testColor);
                        yield return Next(32f / count, badColor, 33, true, "33BadColor_Max32Bad", testColor);

                        yield return Next(32f / count, badAlpha, 31, false, "31BadAlpha_Max32Bad", testAlpha);
                        yield return Next(32f / count, badAlpha, 32, false, "32BadAlpha_Max32Bad", testAlpha);
                        yield return Next(32f / count, badAlpha, 33, true, "33BadAlpha_Max32Bad", testAlpha);

                        // Make sure that each pixel is marked invalid only once even though multiple pixel tests fail.
                        // This test is on the edge of failure. If the incorrect pixel count doubles, we'll exceed the threshold.
                        yield return Next(32f / count, badBoth, 32, false, "MultipleFailCountOnce", testBoth);
                    }

                    // The following tests focus on the actual per-pixel tests and make sure that pixels that are bad
                    // are effectively reported as bad.
                    {
                        yield return Next(0, valid, 1, false, "GoodColor_TestColor", testColor);
                        yield return Next(0, valid, 1, false, "GoodAlpha_TestAlpha", testAlpha);

                        // Any component above the threshold must yield to a failure
                        yield return Next(0, new Color32(11, 0, 0, 255), 1, true, "BadR");
                        yield return Next(0, new Color32(0, 11, 0, 255), 1, true, "BadG");
                        yield return Next(0, new Color32(0, 0, 11, 255), 1, true, "BadB");
                    }
                }
            }
        }

        [Test, TestCaseSource(typeof(PerPixelTestSource), nameof(PerPixelTestSource.TestCases))]
        public void PerPixelTest(ImageComparisonSettings settings, Color32 initColor, Color32 alterColor, int alterCount, bool mustThrow)
        {
            int w = settings.TargetWidth;
            int h = settings.TargetHeight;
            int count = w * h;

            var exp = new Texture2D(w, h);
            var act = new Texture2D(w, h);
            var pixels = new Color32[count];

            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = initColor;
            exp.SetPixels32(pixels);
            exp.Apply();

            // Spread the bad pixels.
            Assume.That(alterCount >= 0 && alterCount <= count);
            int alterPeriod = Mathf.RoundToInt(Mathf.Clamp(alterCount * 0.95f / count, 1f, alterCount));
            int alterNext = 0;
            while (alterCount > 0)
            {
                pixels[alterNext] = alterColor;
                --alterCount;
                alterNext += alterPeriod;
            }

            act.SetPixels32(pixels);
            act.Apply();

            try
            {
                if(mustThrow)
                    Assert.That(() => ImageAssert.AreEqual(exp, act, settings), Throws.InstanceOf<AssertionException>());
                else
                    Assert.That(() => ImageAssert.AreEqual(exp, act, settings), Throws.Nothing);
            }
            finally
            {
                Object.DestroyImmediate(exp);
                Object.DestroyImmediate(act);
            }
        }
#endif // UNITY_EDITOR
    }
}
