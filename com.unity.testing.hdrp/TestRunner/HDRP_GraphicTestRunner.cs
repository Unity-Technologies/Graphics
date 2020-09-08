using System.Collections;
using System.Collections.Generic;
using NUnit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;

public class HDRP_GraphicTestRunner
{
    Texture2D backBufferCaptureTexture;
    bool doCapture = false;

    GraphicsTestCase m_testCase;

    Camera camera;

    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    [Timeout(300 * 1000)] // Set timeout to 5 minutes to handle complex scenes with many shaders (default timeout is 3 minutes)
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        m_testCase = testCase;
        // Debug.Log($"Load Scene : {testCase.ScenePath}");
        SceneManager.LoadScene(testCase.ScenePath);

        yield return null;

        camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        // Check for the backbuffer toogle and force it to use the HDRP test runner specific one, to avoid issues when capturing the images.
        // This is a "lazy" fix to avoid rewriting part of the code
        if (settings.ImageComparisonSettings.UseBackBuffer)
        {
            settings.ImageComparisonSettings.UseBackBuffer = false;
            settings.captureFromBackBuffer = true;
        }

        // Setup the temporary texture to copy from the backbuffer and compare
        // Also set the game view render size
        if (settings.captureFromBackBuffer)
            SetupBackBufferCapture(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i = 0; i < 5; ++i)
            yield return null;

        // Grab the HDCamera
        HDCamera hdCamera = HDCamera.GetOrCreate(camera);


        Time.captureFramerate = settings.captureFramerate;

        if (XRGraphicsAutomatedTests.enabled)
        {
            if (settings.xrCompatible)
            {
                XRGraphicsAutomatedTests.running = true;

                // Increase tolerance to account for slight changes due to float precision
                settings.ImageComparisonSettings.AverageCorrectnessThreshold *= settings.xrThresholdMultiplier;
                settings.ImageComparisonSettings.PerPixelCorrectnessThreshold *= settings.xrThresholdMultiplier;

                // Increase number of volumetric slices to compensate for initial half-resolution due to XR single-pass optimization
                foreach (var volume in GameObject.FindObjectsOfType<Volume>())
                {
                    if (volume.profile.TryGet<Fog>(out Fog fog))
                        fog.volumeSliceCount.value *= 2;
                }
            }
            else
            {
                Assert.Ignore("Test scene is not compatible with XR and will be skipped.");
            }
        }

        if (HDRenderPipeline.enableRenderGraphTests)
        {
            if (!settings.renderGraphCompatible)
            {
                Assert.Ignore("Test scene is not compatible with Render Graph and will be skipped.");
            }
        }

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        if (settings.waitForFrameCountMultiple)
        {
            // Get HDRP instance
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            // Standard Test
            if (settings.captureFromBackBuffer) // Using Backbuffer
            {
                // When we capture from the back buffer, there is no requirement of compensation frames
                while (((hdCamera.cameraFrameCount) % (uint)settings.frameCountMultiple) != 0) yield return null;
            }
            else
            {
                // Given that we will render two frames, we need to compensate for them in the waiting
                // After this line, the next frame will be frame 0.
                while (((hdCamera.cameraFrameCount + 2) % (uint)settings.frameCountMultiple) != 0) yield return null;
            }
        }

        // Force clear all the history buffers
        hdCamera.RequestClearHistoryBuffers();

        for (int i = 0 ; i < settings.waitFrames ; ++i)
            yield return null;

        var settingsSG = (GameObject.FindObjectOfType<HDRP_TestSettings>() as HDRP_ShaderGraph_TestSettings);
        if (settingsSG == null || !settingsSG.compareSGtoBI)
        {
            // Standard Test
            if (settings.captureFromBackBuffer) // Using Backbuffer
            {
                doCapture = true;

                while (doCapture) yield return null;

                ImageAssert.AreEqual(m_testCase.ReferenceImage, backBufferCaptureTexture, settings.ImageComparisonSettings);

                // Cleanup the capture data
                CleanBackBufferCapture();
            }
            else // Or rendering to a render texture
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings?.ImageComparisonSettings);
            }

            if (settings.checkMemoryAllocation)
            {
                // Does it allocate memory when it renders what's on camera?
                bool allocatesMemory = false;
                try
                {
                    // GC alloc from Camera.CustomRender (case 1206364)
                    int gcAllocThreshold = 2;

                    ImageAssert.AllocatesMemory(camera, settings?.ImageComparisonSettings, gcAllocThreshold);
                }
                catch (AssertionException)
                {
                    allocatesMemory = true;
                }
                if (allocatesMemory)
                    Assert.Fail("Allocated memory when rendering what is on camera");
            }
        }
        else
        {
            if (settingsSG.sgObjs == null)
            {
                Assert.Fail("Missing Shader Graph objects in test scene.");
            }
            if (settingsSG.biObjs == null)
            {
                Assert.Fail("Missing comparison objects in test scene.");
            }

            settingsSG.sgObjs.SetActive(true);
            settingsSG.biObjs.SetActive(false);
            yield return null; // Wait a frame
            yield return null;
            bool sgFail = false;
            bool biFail = false;

            // First test: Shader Graph
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
            }
            catch (AssertionException)
            {
                sgFail = true;
            }

            settingsSG.sgObjs.SetActive(false);
            settingsSG.biObjs.SetActive(true);
            settingsSG.biObjs.transform.position = settingsSG.sgObjs.transform.position; // Move to the same location.
            yield return null; // Wait a frame
            yield return null;

            // Second test: HDRP/Lit Materials
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
            }
            catch (AssertionException)
            {
                biFail = true;
            }

            // Informs which ImageAssert failed, if any.
            if (sgFail && biFail) Assert.Fail("Both Shader Graph and Non-Shader Graph Objects failed to match the reference image");
            else if (sgFail) Assert.Fail("Shader Graph Objects failed.");
            else if (biFail) Assert.Fail("Non-Shader Graph Objects failed to match Shader Graph objects.");
        }
    }

    // Register the capture from backbuffer logic in the endCameraRendering hook point.
    [OneTimeSetUp]
    public void OneTimeSetUpFunc()
    {
        RenderPipelineManager.endCameraRendering += PostRenderCallback;
    }

    // Remove the hook and delete the texture.
    [OneTimeTearDown]
    public void OneTimeTearDownFunc()
    {
        if (backBufferCaptureTexture != null) Object.DestroyImmediate(backBufferCaptureTexture);
        RenderPipelineManager.endCameraRendering -= PostRenderCallback;
    }

    void PostRenderCallback( ScriptableRenderContext context,  Camera cam )
    {
        if ( !doCapture)
            return;

        if ( camera == null || cam == null || camera != cam) return;

        backBufferCaptureTexture.ReadPixels(
            new Rect(0, 0, backBufferCaptureTexture.width, backBufferCaptureTexture.height),
            0, 0,
            false
            );

        backBufferCaptureTexture.Apply();

        // Debug.Log($"imageComparisonSettings before ImageAssert: width {imageComparisonSettings.TargetWidth}, height {imageComparisonSettings.TargetHeight}, pixel corr. threshold {imageComparisonSettings.PerPixelCorrectnessThreshold}, avg. corr. threshold {imageComparisonSettings.AverageCorrectnessThreshold}");

        doCapture = false;
    }

    void SetupBackBufferCapture( int width, int height )
    {
        SetViewSize(width, height);

        if (backBufferCaptureTexture != null) Object.DestroyImmediate(backBufferCaptureTexture);

        backBufferCaptureTexture = new Texture2D(
            width, height,
            TextureFormat.RGB24,
            false,
            true
            );
    }

    void CleanBackBufferCapture(  )
    {
        camera = null;
    }

    void SetViewSize( int width, int height)
    {
#if UNITY_EDITOR
        GameViewUtils.SetGameViewSize(width, height);
#else
        Screen.SetResolution(width, height, Screen.fullScreenMode);
#endif
    }

#if UNITY_EDITOR


    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

    [TearDown]
    public void ResetSystemState()
    {
        XRGraphicsAutomatedTests.running = false;
    }
#endif

}
