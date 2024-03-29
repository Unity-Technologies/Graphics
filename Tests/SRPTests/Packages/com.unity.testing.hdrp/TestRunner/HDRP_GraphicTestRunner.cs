using System;
using System.Collections;
using System.Collections.Generic;
using NUnit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;
using System.Linq;
using UnityEngine.VFX;

public class HDRP_GraphicTestRunner
{
    private bool GPUResidentDrawerRequested()
    {
        bool forcedOn = false;
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.Equals("-force-gpuresidentdrawer", StringComparison.InvariantCultureIgnoreCase))
            {
                forcedOn = true;
                break;
            }
        }

        var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
        if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
            return forcedOn || mbAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled;

        return false;
    }

    [UnityTest]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        HDRP_TestSettings settings = null;
        Camera[] cameras = null;
        HDCamera[] hdCameras = null;
        Camera camera = null;

#if UNITY_EDITOR
        // Load the test settings
        var oldValueShaderUtil = UnityEditor.ShaderUtil.allowAsyncCompilation;
        var oldValueEditorSettings = UnityEditor.EditorSettings.asyncShaderCompilation;

        UnityEditor.ShaderUtil.allowAsyncCompilation = true;
        UnityEditor.EditorSettings.asyncShaderCompilation = true;

		Debug.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImagePathLog}.");
        SceneManager.LoadScene(testCase.ScenePath);

        // Wait for scene loading to retrieve settings/camera.
        for (int i = 0; i < 5; ++i)
            yield return new WaitForEndOfFrame();

        settings = GameObject.FindFirstObjectByType<HDRP_TestSettings>();
        camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        if (!settings.gpuDrivenCompatible && GPUResidentDrawerRequested())
            Assert.Ignore("Test scene is not compatible with GPU Driven and and will be skipped.");

        // Purpose is to setup proper test aspect ratio for the camera to "see" all objects and trigger related shader compilation tasks.
        int warmupTime = 1;
        if (XRGraphicsAutomatedTests.enabled)
            warmupTime = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.xrCompatible, warmupTime, settings.ImageComparisonSettings);
        else
            camera.targetTexture = RenderTexture.GetTemporary(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight);

        // Trigger any test specific script. This is because it may change objects state and trigger shader compilation.
        settings.doBeforeTest?.Invoke();

        // Wait one rendered frame to trigger shader compilation.
        for (int i = 0; i < warmupTime; ++i)
            yield return new WaitForEndOfFrame();

        // Wait for all compilation to end.
        while (UnityEditor.ShaderUtil.anythingCompiling)
        {
            yield return new WaitForEndOfFrame();
        }

        camera.targetTexture = null;
#endif

        // Reload the scene to reset time in order to be deterministic.
        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for a few frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        // Used to be 5 but we changed the process a bit with shader compilation in editor so we need 4 to retain old behavior.
        int frameSkip = 5;
#if UNITY_EDITOR
        frameSkip = 4;
#endif

        for (int i = 0; i < frameSkip; ++i)
            yield return new WaitForEndOfFrame();

        // Need to retrieve objects again after scene reload.
        settings = GameObject.FindFirstObjectByType<HDRP_TestSettings>();
        camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        // Grab the HDCamera
        HDCamera hdCamera = HDCamera.GetOrCreate(camera);

        //We need to get all the cameras to set accumulation in all of them for tests that uses multiple cameras
        cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.InstanceID);

        //Grab the HDCameras
        hdCameras = new HDCamera[cameras.Length];
        for(int i = 0; i < cameras.Length; ++i)
        {
            hdCameras[i] = HDCamera.GetOrCreate(cameras[i]);
        }


        bool useBackBuffer = settings.ImageComparisonSettings.UseBackBuffer;

// #if UNITY_EDITOR
        if (useBackBuffer)
        {
            //When using back buffer, we have to set accumulation to true to make sure effects accumulates.
            SetRayTracingAccumulationOnCameras(hdCameras, true);
        }
        else
        {
            //To no break our current tests, we have to disable accumulation if we capture using render texture.
            SetRayTracingAccumulationOnCameras(hdCameras, false);
        }
// #endif


        if (!settings.gpuDrivenCompatible && GPUResidentDrawerRequested())
            Assert.Ignore("Test scene is not compatible with GPU Driven and and will be skipped.");

        Time.captureFramerate = settings.captureFramerate;

        int waitFrames = settings.waitFrames;

        if (XRGraphicsAutomatedTests.enabled)
        {
            waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.xrCompatible, waitFrames, settings.ImageComparisonSettings);

            // Increase tolerance to account for slight changes due to float precision
            settings.ImageComparisonSettings.AverageCorrectnessThreshold *= settings.xrThresholdMultiplier;
            settings.ImageComparisonSettings.PerPixelCorrectnessThreshold *= settings.xrThresholdMultiplier;

            // Increase number of volumetric slices to compensate for initial half-resolution due to XR single-pass optimization
            foreach (var volume in GameObject.FindObjectsByType<Volume>(FindObjectsSortMode.InstanceID))
            {
                if (volume.profile.TryGet<Fog>(out Fog fog))
                    fog.volumeSliceCount.value *= 2;
            }
        }

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return new WaitForEndOfFrame();
        }

        // Reset temporal effects on hdCameras
        foreach(HDCamera hdCam in hdCameras)
            hdCam.Reset();

        if (settings.waitForFrameCountMultiple)
        {
            // When we capture from the back buffer, there is no requirement of compensation frames
            // Else, given that we will render two frames, we need to compensate for them in the waiting
            var frameCountOffset = useBackBuffer ? 0 : 2;
            while (((hdCamera.cameraFrameCount + frameCountOffset) % (uint)settings.frameCountMultiple) != 0)
                yield return new WaitForEndOfFrame();
        }

        // Force clear all the history buffers
        if (useBackBuffer)
        {
            foreach(HDCamera hdCam in hdCameras)
            {
                hdCamera.RequestClearHistoryBuffers();
            }
        }

        // Reset temporal effects on hdCameras
        foreach(HDCamera hdCam in hdCameras)
            hdCam.Reset();

        // Ensure frame consistency for VFXs
        if (settings.containsVFX)
        {
            const int maxFrameWaiting = 8;
            int maxFrame = maxFrameWaiting;
            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            while (vfxComponents.All(o => o.culled) && maxFrame-- > 0)
                yield return new WaitForEndOfFrame();
            Assert.Greater(maxFrame, 0);

            foreach (var component in vfxComponents)
                component.Reinit();
        }

        for (int i = 0; i < waitFrames; ++i)
            yield return new WaitForEndOfFrame();

        var settingsSG = (GameObject.FindFirstObjectByType<HDRP_TestSettings>() as HDRP_ShaderGraph_TestSettings);
        if (settingsSG == null || !settingsSG.compareSGtoBI)
        {
            // Standard Test
            ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings?.ImageComparisonSettings, testCase.ReferenceImagePathLog);

            // For some reason, tests on mac os have started failing with render graph enabled by default.
            // Some tests have 400+ gcalloc in them. Unfortunately it's not reproductible outside of command line so it's impossible to debug.
            // That's why we don't test on macos anymore.
            if (settings.checkMemoryAllocation && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal)
            {
                yield return ImageAssert.CheckGCAllocWithCallstack(camera, settings?.ImageComparisonSettings);
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
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            bool sgFail = false;
            bool biFail = false;

            // First test: Shader Graph
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null, testCase.ReferenceImagePathLog);
            }
            catch (AssertionException)
            {
                sgFail = true;
            }

            settingsSG.sgObjs.SetActive(false);
            settingsSG.biObjs.SetActive(true);
            settingsSG.biObjs.transform.position = settingsSG.sgObjs.transform.position; // Move to the same location.
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Second test: HDRP/Lit Materials
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null, testCase.ReferenceImagePathLog);
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


        //// In Standalone, we need to reset the RTHandleSystem because on some devices with low memory we can easily run out
        //// For example when going from MSAA4x to regular 1080p we'd have both sets of full sized buffer in memory.
        //if (!Application.isEditor)
        //{
        //    HDRenderPipeline.currentPipeline?.ResetRTHandleReferenceSize(1, 1);

        //    // Wait for RT dealloc to happen.
        //    for (int i = 0; i < frameSkip; ++i)
        //        yield return new WaitForEndOfFrame();
        //}

#if UNITY_EDITOR
        UnityEditor.ShaderUtil.allowAsyncCompilation = oldValueShaderUtil;
        UnityEditor.EditorSettings.asyncShaderCompilation = oldValueEditorSettings;
#endif

        //When using back buffer, we have to set accumulation back to true at the end of the test
        if (useBackBuffer)
        {
            SetRayTracingAccumulationOnCameras(hdCameras, true);
        }

    }

    public void SetRayTracingAccumulationOnCameras(HDCamera[] hdCameras, bool b)
    {
        foreach(HDCamera hdCamera in hdCameras)
        {
            hdCamera.SetRayTracingAccumulation(b);
        }
    }

#if UNITY_EDITOR

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

    [TearDown]
    public void TearDownXR()
    {
        XRGraphicsAutomatedTests.running = false;
    }

#endif
}
