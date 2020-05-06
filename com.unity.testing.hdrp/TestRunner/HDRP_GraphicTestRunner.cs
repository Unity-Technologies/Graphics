using System.Collections;
using System.Collections.Generic;
using NUnit;
using NUnit.Framework;
using UnityEngine;
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

        // Setup the temporary texture to copy from the backbuffer and compare
        // Also set the game view render size
        if (settings.captureFromBackBuffer)
            SetupBackBufferCapture(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i = 0; i < 5; ++i)
            yield return null;

        Time.captureFramerate = settings.captureFramerate;

        if (XRSystem.testModeEnabled)
        {
            if (settings.xrCompatible)
            {
                XRSystem.automatedTestRunning = true;

                // Increase tolerance to account for slight changes due to float precision
                settings.ImageComparisonSettings.AverageCorrectnessThreshold *= settings.xrThresholdMultiplier;
                settings.ImageComparisonSettings.PerPixelCorrectnessThreshold *= settings.xrThresholdMultiplier;
            }
            else
            {
                // Skip incompatible XR tests
                yield break;
            }
        }

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        for (int i = 0; i < settings.waitFrames; ++i)
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

#if UNITY_2019_3
                    // In case playmode tests for XR are enabled in 2019.3 we allow one GC alloc from XRSystem:120
                    if (XRSystem.testModeEnabled)
                        gcAllocThreshold += 1;
#endif

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

    [OneTimeSetUp]
    public void OneTimeSetUpFunc()
    {
        RenderPipelineManager.endCameraRendering += PostRenderCallback;
    }

    [OneTimeTearDown]
    public void OneTimeTearDownFunc()
    {
        if (backBufferCaptureTexture != null) Object.DestroyImmediate(backBufferCaptureTexture);
        RenderPipelineManager.endCameraRendering -= PostRenderCallback;
    }

    void PostRenderCallback( ScriptableRenderContext context, Camera cam )
    {
        if ( !doCapture)
            return;

        if ( camera == null || cam == null || camera != cam) return;

        // Debug.Log("Capture Backbuffer");

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
        XRSystem.automatedTestRunning = false;
    }
#endif

}
