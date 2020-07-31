using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine.TestTools.Graphics.Tests
{
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
            expected.SetPixels(new [] { c });
            expected.Apply(false);

            var actual = new Texture2D(1, 2);
            actual.SetPixels(new [] { c, c });
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
    }
}
