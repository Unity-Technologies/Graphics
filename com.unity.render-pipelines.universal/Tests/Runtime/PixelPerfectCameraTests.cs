using NUnit.Framework;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    internal class PixelPerfectCameraTests
    {
        internal class PixelPerfectCameraTestComponent : IPixelPerfectCamera
        {
            public int assetsPPU { get; set; }
            public int refResolutionX { get; set; }
            public int refResolutionY { get; set; }
            public bool upscaleRT { get; set; }
            public bool pixelSnapping { get; set; }
            public bool cropFrameX { get; set; }
            public bool cropFrameY { get; set; }
            public bool stretchFill { get; set; }
        }

        internal class CalculateCameraPropertiesResult
        {
            public int zoom;
            public bool useOffscreenRT;
            public int offscreenRTWidth;
            public int offscreenRTHeight;
            public Rect pixelRect;
            public float orthoSize;
            public float unitsPerPixel;
        }

        private static object[] GetCalculateCameraPropertiesTestCases()
        {
            object[] testCaseArray = new object[9];

            for (int i = 0; i < testCaseArray.Length; ++i)
            {
                PixelPerfectCameraTestComponent testComponent = new PixelPerfectCameraTestComponent();
                int screenWidth = 0;
                int screenHeight = 0;
                CalculateCameraPropertiesResult expected = new CalculateCameraPropertiesResult();

                switch (i)
                {
                    case 0:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = false;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = true;
                        testComponent.cropFrameY = true;
                        testComponent.stretchFill = true;
                        screenWidth = 800;
                        screenHeight = 500;

                        expected.zoom = 1;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 400;
                        expected.offscreenRTHeight = 300;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 400, 300);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 1:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = true;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = true;
                        testComponent.cropFrameY = true;
                        testComponent.stretchFill = true;
                        screenWidth = 1100;
                        screenHeight = 900;

                        expected.zoom = 2;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 400;
                        expected.offscreenRTHeight = 300;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 400, 300);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 2:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = true;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = false;
                        testComponent.cropFrameY = true;
                        testComponent.stretchFill = false;
                        screenWidth = 400;
                        screenHeight = 250;

                        expected.zoom = 1;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 400;
                        expected.offscreenRTHeight = 300;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 400, 300);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 3:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = true;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = true;
                        testComponent.cropFrameY = false;
                        testComponent.stretchFill = false;
                        screenWidth = 1600;
                        screenHeight = 1200;

                        expected.zoom = 4;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 400;
                        expected.offscreenRTHeight = 300;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 400, 300);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 4:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = true;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = false;
                        testComponent.cropFrameY = false;
                        testComponent.stretchFill = false;
                        screenWidth = 1600;
                        screenHeight = 1100;

                        expected.zoom = 3;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 532;
                        expected.offscreenRTHeight = 366;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 532, 366);
                        expected.orthoSize = 1.83f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 5:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = false;
                        testComponent.pixelSnapping = false;
                        testComponent.cropFrameX = false;
                        testComponent.cropFrameY = false;
                        testComponent.stretchFill = true;
                        screenWidth = 800;
                        screenHeight = 600;

                        expected.zoom = 2;
                        expected.useOffscreenRT = false;
                        expected.offscreenRTWidth = 0;
                        expected.offscreenRTHeight = 0;
                        expected.pixelRect = Rect.zero;
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.005f;
                        break;

                    case 6:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = false;
                        testComponent.pixelSnapping = false;
                        testComponent.cropFrameX = true;
                        testComponent.cropFrameY = true;
                        testComponent.stretchFill = false;
                        screenWidth = 800;
                        screenHeight = 700;

                        expected.zoom = 2;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 800;
                        expected.offscreenRTHeight = 600;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 800, 600);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.005f;
                        break;

                    case 7:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = false;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = false;
                        testComponent.cropFrameY = true;
                        testComponent.stretchFill = false;
                        screenWidth = 900;
                        screenHeight = 600;

                        expected.zoom = 2;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 900;
                        expected.offscreenRTHeight = 600;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 900, 600);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;

                    case 8:
                        testComponent.assetsPPU = 100;
                        testComponent.refResolutionX = 400;
                        testComponent.refResolutionY = 300;
                        testComponent.upscaleRT = false;
                        testComponent.pixelSnapping = true;
                        testComponent.cropFrameX = true;
                        testComponent.cropFrameY = false;
                        testComponent.stretchFill = false;
                        screenWidth = 900;
                        screenHeight = 600;

                        expected.zoom = 2;
                        expected.useOffscreenRT = true;
                        expected.offscreenRTWidth = 800;
                        expected.offscreenRTHeight = 600;
                        expected.pixelRect = new Rect(0.0f, 0.0f, 800, 600);
                        expected.orthoSize = 1.5f;
                        expected.unitsPerPixel = 0.01f;
                        break;
                }

                testCaseArray[i] = new object[] { testComponent, screenWidth, screenHeight, expected };
            }

            return testCaseArray;
        }

        [Test, TestCaseSource("GetCalculateCameraPropertiesTestCases")]
        public void CalculateCameraPropertiesProvidesCorrectResultsWithVariousInputs(PixelPerfectCameraTestComponent testComponent, int screenWidth, int screenHeight, CalculateCameraPropertiesResult expected)
        {
            PixelPerfectCameraInternal internals = new PixelPerfectCameraInternal(testComponent);
            internals.CalculateCameraProperties(screenWidth, screenHeight);

            Assert.AreEqual(expected.zoom, internals.zoom);
            Assert.AreEqual(expected.useOffscreenRT, internals.useOffscreenRT);
            Assert.AreEqual(expected.offscreenRTWidth, internals.offscreenRTWidth);
            Assert.AreEqual(expected.offscreenRTHeight, internals.offscreenRTHeight);
            Assert.AreEqual(expected.pixelRect, internals.pixelRect);
            Assert.AreEqual(expected.orthoSize, internals.orthoSize);
            Assert.AreEqual(expected.unitsPerPixel, internals.unitsPerPixel);
        }

        [Test]
        public void CalculateFinalBlitPixelRectStretchToFitHeightWorks()
        {
            PixelPerfectCameraTestComponent testComponent = new PixelPerfectCameraTestComponent();
            testComponent.refResolutionX = 200;
            testComponent.refResolutionY = 100;
            PixelPerfectCameraInternal internals = new PixelPerfectCameraInternal(testComponent);

            internals.useStretchFill = true;
            Rect pixelRect = internals.CalculateFinalBlitPixelRect(400, 100);

            Rect expected = new Rect(100.0f, 0.0f, 200.0f, 100.0f);
            Assert.AreEqual(expected, pixelRect);
        }

        [Test]
        public void CalculateFinalBlitPixelRectStretchToFitWidthWorks()
        {
            PixelPerfectCameraTestComponent testComponent = new PixelPerfectCameraTestComponent();
            testComponent.refResolutionX = 200;
            testComponent.refResolutionY = 100;
            PixelPerfectCameraInternal internals = new PixelPerfectCameraInternal(testComponent);

            internals.useStretchFill = true;
            Rect pixelRect = internals.CalculateFinalBlitPixelRect(200, 200);

            Rect expected = new Rect(0.0f, 50.0f, 200.0f, 100.0f);
            Assert.AreEqual(expected, pixelRect);
        }

        [Test]
        public void CalculateFinalBlitPixelRectCenteredWorksWithUpscaleRT()
        {
            PixelPerfectCameraTestComponent testComponent = new PixelPerfectCameraTestComponent();
            testComponent.upscaleRT = true;
            testComponent.refResolutionX = 400;
            testComponent.refResolutionY = 300;
            PixelPerfectCameraInternal internals = new PixelPerfectCameraInternal(testComponent);

            internals.useStretchFill = false;
            internals.zoom = 2;
            internals.offscreenRTWidth = 400;
            internals.offscreenRTHeight = 300;

            Rect pixelRect = internals.CalculateFinalBlitPixelRect(1600, 1200);

            Rect expected = new Rect(400.0f, 300.0f, 800.0f, 600.0f);
            Assert.AreEqual(expected, pixelRect);
        }

        [Test]
        public void CalculateFinalBlitPixelRectCenteredWorksWithoutUpscaleRT()
        {
            PixelPerfectCameraTestComponent testComponent = new PixelPerfectCameraTestComponent();
            testComponent.upscaleRT = false;
            testComponent.refResolutionX = 400;
            testComponent.refResolutionY = 300;
            PixelPerfectCameraInternal internals = new PixelPerfectCameraInternal(testComponent);

            internals.useStretchFill = false;
            internals.zoom = 2;
            internals.offscreenRTWidth = 400;
            internals.offscreenRTHeight = 300;

            Rect pixelRect = internals.CalculateFinalBlitPixelRect(1600, 1200);

            Rect expected = new Rect(600.0f, 450.0f, 400.0f, 300.0f);
            Assert.AreEqual(expected, pixelRect);
        }
    }
}
