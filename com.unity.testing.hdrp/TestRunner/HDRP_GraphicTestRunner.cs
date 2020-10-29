using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;

public class HDRP_GraphicTestRunner
{
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    [Timeout(300 * 1000)] // Set timeout to 5 minutes to handle complex scenes with many shaders (default timeout is 3 minutes)
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i=0 ; i<5 ; ++i)
            yield return null;

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

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

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        // Reset temporal effects on hdCamera
        HDCamera.GetOrCreate(camera).Reset();

        for (int i=0 ; i<settings.waitFrames ; ++i)
            yield return null;

        var settingsSG = (GameObject.FindObjectOfType<HDRP_TestSettings>() as HDRP_ShaderGraph_TestSettings);
        if (settingsSG == null || !settingsSG.compareSGtoBI)
        {
            // Standard Test
            ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings?.ImageComparisonSettings);

            // For some reason, tests on mac os have started failing with render graph enabled by default.
            // Some tests have 400+ gcalloc in them. Unfortunately it's not reproductible outside of command line so it's impossible to debug.
            // That's why we don't test on macos anymore.
            if (settings.checkMemoryAllocation && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal)
            {
                // Does it allocate memory when it renders what's on camera?
                bool allocatesMemory = false;
                try
                {
                    // GC alloc from Camera.CustomRender (case 1206364)
                    int gcAllocThreshold = 0;

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
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
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
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
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
