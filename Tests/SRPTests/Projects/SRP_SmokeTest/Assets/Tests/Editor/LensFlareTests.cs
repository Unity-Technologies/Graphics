using System.IO;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools.Graphics;

namespace UnityEngine.Rendering.Tests
{
    class LensFlareTests
    {
        // Resolution of the render target and expected images
        static readonly int kRes = 512;
        GameObject go;
        GameObject camGo;
        ImageComparisonSettings comparisonSettings;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("Light", typeof(Light));
            camGo = new GameObject();
            comparisonSettings = new()
            {
                TargetWidth = kRes,
                TargetHeight = kRes,
                ActiveImageTests = ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
                ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaGamma,
                IncorrectPixelsThreshold = 1.0f / (kRes * kRes),
                PerPixelGammaThreshold = 1f / 255
            };
        }

        [TearDown]
        public void TearDown()
        {
            LensFlareCommonSRP.Dispose();
            GameObject.DestroyImmediate(go);
            GameObject.DestroyImmediate(camGo);
        }

        float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        {
            Assume.That(light != null);
            switch (light.type)
            {
                case LightType.Directional:
                    return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
                case LightType.Point:
                    return LensFlareCommonSRP.ShapeAttenuationPointLight();
                case LightType.Spot:
                    return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
                default:
                    return 1.0f;
            }
        }

        enum Mode
        {
            RenderToColor,
            RenderToOcclusion,
            RenderToColorWithInlineOcclusion,
        }

        public enum SRP
        {
            HDRP,
            URP
        }
        
        Texture2D RenderLensFlareAndDownloadTex(SRPLensFlareType flareType, Mode mode, SRP srp)
        {
            // Initialize the static class LensFlareCommonSRP for the test
            LensFlareCommonSRP.mergeNeeded = 0;
            LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample = 1;
            LensFlareCommonSRP.Initialize();

            var comp = go.AddComponent<LensFlareComponentSRP>();
            // Setup mock lens flare data
            {
                var lensFlareData = ScriptableObject.CreateInstance<LensFlareDataSRP>();
                lensFlareData.elements = new LensFlareDataElementSRP[1];
                lensFlareData.elements[0] = new LensFlareDataElementSRP();
                lensFlareData.elements[0].visible = true;
                lensFlareData.elements[0].localIntensity = 1.0f;
                lensFlareData.elements[0].count = 1;
                lensFlareData.elements[0].flareType = flareType;
                comp.lensFlareData = lensFlareData;
                comp.useOcclusion = (mode == Mode.RenderToOcclusion || mode == Mode.RenderToColorWithInlineOcclusion);
            }
            comp.intensity = 1.0f;
            comp.transform.position = new Vector3(0, 0, 1);
            LensFlareCommonSRP.Instance.AddData(comp);

            // Initialize a command buffer
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "LensFlareTest";

            // Create a camera
            Camera camera = camGo.AddComponent<Camera>();

            // Set constants that URP usually takes care of
            {
                cmd.SetGlobalVector(Shader.PropertyToID("_ScaledScreenParams"),
                    new Vector4(kRes, kRes,
                        1.0f + 1.0f / kRes, 1.0f + 1.0f / kRes));
                cmd.SetGlobalVector(Shader.PropertyToID("_ScreenSize"),
                    new Vector4(kRes, kRes, 1.0f / kRes, 1.0f / kRes));
            }

            // Create a material for lens flare
            Shader shader = Shader.Find(
                (srp == SRP.URP) ?
                "Hidden/Universal Render Pipeline/LensFlareDataDriven" :
                "Hidden/HDRP/LensFlareDataDriven"
            );
            Assume.That(shader != null);
            Material lensFlareShader = new Material(shader);

            // Set up viewport and dimensions
            Rect viewport = new Rect(0, 0, kRes, kRes);

            // Set up projections params
            bool usePanini = false;
            float paniniDistance = 0;
            float paniniCropToFit = 0;

            // Set up camera params
            bool isCameraRelative = true;
            Matrix4x4 viewProjMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;

            // Other params
            bool taaEnabled = false;
            bool hasCloudLayer = false;

            // Create a dummy XRPass (we'll use null which should handle the non-XR path)
            XRPass xrPass = new XRPass();
            int xrIndex = 0;

            // Call the function to be tested
            if (mode == Mode.RenderToColor)
            {
                // Create a render texture for output
                RenderTexture rt = RenderTexture.GetTemporary(kRes, kRes, 24, RenderTextureFormat.ARGB32);
                RenderTargetIdentifier rtID = new RenderTargetIdentifier(rt);

                // Clear the render texture first
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.black);

                LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                    lensFlareShader, camera, viewport, xrPass, xrIndex,
                    kRes, kRes,
                    usePanini, paniniDistance, paniniCropToFit,
                    isCameraRelative,
                    camera.transform.position,
                    viewProjMatrix,
                    cmd,
                    taaEnabled, hasCloudLayer, null, null,
                    rtID,
                    GetLensFlareLightAttenuation,
                    false);

                // Submit command buffer to GPU
                // ExternalGPUProfiler.BeginGPUCapture();
                Graphics.ExecuteCommandBuffer(cmd);
                // ExternalGPUProfiler.EndGPUCapture();

                // Download the render target to CPU
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                return tex;
            }
            else if (mode == Mode.RenderToOcclusion)
            {
                // Generate a depth buffer with the far plane on top and a near
                // plane on the bottom. The bottom part of the flare is occluded,
                // and the top part is visible. Thus, the whole flare gets a
                // partial opacity.
                var depthTexture = new Texture2D(kRes, kRes, TextureFormat.RFloat, false);
                {
                    // This is the Y coordinate at which the depth changes. It's
                    // not perfectly in the center to avoid symmetry, so that
                    // we avoid a value of 0.5 occlusion, and thus improve the
                    // test case.
                    int depthStepCoordY = (kRes / 2) + 10;
                    var pixels = new Color[kRes * kRes];
                    for (var y = 0; y < kRes; y++)
                    {
                        float depthValue = (y < depthStepCoordY) ? 0 : 1;

                        // Set the entire row to this depth value
                        for (var x = 0; x < kRes; x++)
                        {
                            int index = y * kRes + x;
                            pixels[index] = new Color(depthValue, 0, 0, 0); // R channel contains depth
                        }
                    }

                    // Apply the pixels to the texture
                    depthTexture.SetPixels(pixels);
                    depthTexture.Apply();
                }

                // Bind the depth texture to the global property
                cmd.SetGlobalTexture("_CameraDepthTexture", depthTexture);

                // Generate a remap texture, which is a 128x1 texture containing
                // a gradient going from 0 to 1.
                var remapTexture = comp.occlusionRemapCurve.GetTexture();

                // Bind the remap texture to the global property
                cmd.SetGlobalTexture("_FlareOcclusionRemapTex", remapTexture);

                LensFlareCommonSRP.ComputeOcclusion(
                    lensFlareShader, camera, xrPass, xrIndex,
                    kRes, kRes,
                    usePanini, paniniDistance, paniniCropToFit,
                    isCameraRelative,
                    camera.transform.position,
                    viewProjMatrix,
                    cmd,
                    taaEnabled, hasCloudLayer, null, null);

                // ExternalGPUProfiler.BeginGPUCapture();
                Graphics.ExecuteCommandBuffer(cmd);
                // ExternalGPUProfiler.EndGPUCapture();

                // Download the render target to CPU
                RenderTexture.active = LensFlareCommonSRP.occlusionRT.rt;
                Assume.That(LensFlareCommonSRP.occlusionRT.rt.width == 128);
                Assume.That(LensFlareCommonSRP.occlusionRT.rt.height == 1);
                Texture2D tex = new Texture2D(128, 1, TextureFormat.RFloat, false);
                tex.ReadPixels(new Rect(0f, 0f, 128, 1), 0, 0);
                tex.Apply();

                // Clean up the temporary CPU textures
                Object.DestroyImmediate(depthTexture);

                return tex;
            }
            else
            {
                Assume.That(mode == Mode.RenderToColorWithInlineOcclusion);

                // Generate a depth buffer with the far plane on top and a near
                // plane on the bottom. The bottom part of the flare is occluded,
                // and the top part is visible. Thus, the whole flare gets a
                // partial opacity.
                var depthTexture = new Texture2D(kRes, kRes, TextureFormat.RFloat, false);
                {
                    // This is the Y coordinate at which the depth changes. It's
                    // not perfectly in the center to avoid symmetry, so that
                    // we avoid a value of 0.5 occlusion, and thus improve the
                    // test case.
                    int depthStepCoordY = (kRes / 2) + 10;
                    var pixels = new Color[kRes * kRes];
                    for (var y = 0; y < kRes; y++)
                    {
                        float depthValue = (y < depthStepCoordY) ? 0 : 1;

                        // Set the entire row to this depth value
                        for (var x = 0; x < kRes; x++)
                        {
                            int index = y * kRes + x;
                            pixels[index] = new Color(depthValue, 0, 0, 0); // R channel contains depth
                        }
                    }

                    // Apply the pixels to the texture
                    depthTexture.SetPixels(pixels);
                    depthTexture.Apply();
                }

                // Bind the depth texture to the global property
                cmd.SetGlobalTexture("_CameraDepthTexture", depthTexture);

                // Generate a remap texture, which is a 128x1 texture containing
                // a gradient going from 0 to 1.
                var remapTexture = comp.occlusionRemapCurve.GetTexture();

                // Bind the remap texture to the global property
                cmd.SetGlobalTexture("_FlareOcclusionRemapTex", remapTexture);

                // Create a render texture for output
                RenderTexture rt = RenderTexture.GetTemporary(kRes, kRes, 24, RenderTextureFormat.ARGB32);
                RenderTargetIdentifier rtID = new RenderTargetIdentifier(rt);

                // Clear the render texture first
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.black);

                LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                    lensFlareShader, camera, viewport, xrPass, xrIndex,
                    kRes, kRes,
                    usePanini, paniniDistance, paniniCropToFit,
                    isCameraRelative,
                    camera.transform.position,
                    viewProjMatrix,
                    cmd,
                    taaEnabled, hasCloudLayer, null, null,
                    rtID,
                    GetLensFlareLightAttenuation,
                    false);

                // Submit command buffer to GPU
                // ExternalGPUProfiler.BeginGPUCapture();
                Graphics.ExecuteCommandBuffer(cmd);
                // ExternalGPUProfiler.EndGPUCapture();

                // Download the render target to CPU
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                return tex;
            }
        }

        [Test, GraphicsTest]
        public void FlareRendersToColorURP(
            GraphicsTestCase testCase,
            [Values(SRPLensFlareType.Circle, SRPLensFlareType.Polygon, SRPLensFlareType.Ring)] SRPLensFlareType flareType
        )
        {
            Texture2D actualTex = RenderLensFlareAndDownloadTex(flareType, Mode.RenderToColor, SRP.URP);
            Texture2D expectedTex = testCase.ReferenceImage.Image;
            ImageAssert.AreEqual(expectedTex, actualTex, comparisonSettings, testCase.ReferenceImage.LoadMessage);
        }
        
        [Test, GraphicsTest]
        [IgnoreGraphicsTest("", "Occlusion render texture is not supported on GL or WebGPU", graphicsDeviceTypes: new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.WebGPU })]
        public void FlareRendersToColorHDRP(
            GraphicsTestCase testCase,
            [Values(SRPLensFlareType.Circle, SRPLensFlareType.Polygon, SRPLensFlareType.Ring)] SRPLensFlareType flareType
        )
        {
            Texture2D actualTex = RenderLensFlareAndDownloadTex(flareType, Mode.RenderToColor, SRP.HDRP);
            Texture2D expectedTex = testCase.ReferenceImage.Image;
            ImageAssert.AreEqual(expectedTex, actualTex, comparisonSettings, testCase.ReferenceImage.LoadMessage);
        }

        [Test, GraphicsTest]
        [IgnoreGraphicsTest("", "Occlusion render texture is not supported on GL or WebGPU", graphicsDeviceTypes: new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.WebGPU })]
        public void FlareRendersToOcclusionTexture(
            GraphicsTestCase testCase,
            [Values(SRP.URP, SRP.HDRP)] SRP srp
        )
        {
            Assume.That(LensFlareCommonSRP.IsOcclusionRTCompatible());

            Texture2D actualTex = RenderLensFlareAndDownloadTex(SRPLensFlareType.Polygon, Mode.RenderToOcclusion, srp);
            Texture2D expectedTex = testCase.ReferenceImage.Image;
            ImageComparisonSettings comp = new()
            {
                TargetWidth = kRes,
                TargetHeight = kRes,
                ActiveImageTests = ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
                ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaGamma,
                IncorrectPixelsThreshold = 1.0f / (kRes * kRes),
                // This test in URP mode has a high divergence from the reference image, but only on Yamato, where it is
                // running on an integrated GPU. For now, we're putting a high tolerance for this variant of the test.
                // The ratio of incorrect pixels is still very low (only 1 deviant pixel allowed), and so it is still
                // a good (albeit imperfect) test.
                PerPixelGammaThreshold = (srp == SRP.URP) ? 50f / 255f : 1f/255f,
            };
            ImageAssert.AreEqual(expectedTex, actualTex, comp, testCase.ReferenceImage.LoadMessage);
        }

        [Test, GraphicsTest]
        [IgnoreGraphicsTest("", "Inline occlusion is only used on GL or WebGPU", graphicsDeviceTypes: new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.WebGPU }, isInclusive: true)]
        public void FlareRendersToColorWithInlineOcclusion(
            GraphicsTestCase testCase
        )
        {
            Assume.That(!LensFlareCommonSRP.IsOcclusionRTCompatible());

            Texture2D actualTex = RenderLensFlareAndDownloadTex(SRPLensFlareType.Polygon, Mode.RenderToColorWithInlineOcclusion, SRP.URP);
            Texture2D expectedTex = testCase.ReferenceImage.Image;
            ImageAssert.AreEqual(expectedTex, actualTex, comparisonSettings, testCase.ReferenceImage.LoadMessage);
        }
    }
}
