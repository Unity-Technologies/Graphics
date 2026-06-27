using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public class HDRP_GraphicTestRunner
{
    private static bool GPUResidentDrawerRequested()
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

    static Camera FindMainCamera()
    {
        {
            var cameras = GameObject.FindGameObjectsWithTag("MainCamera");
            Assert.LessOrEqual(cameras.Length, 1, "Multiple cameras found with tag 'MainCamera'.");
            if (cameras.Length > 0) return cameras[0].GetComponent<Camera>();
        }

        {
            var cameras = Object.FindObjectsByType<Camera>();
            Assert.LessOrEqual(cameras.Length, 1, "No camera found with tag 'MainCamera', and multiple cameras found without it.");
            if (cameras.Length > 0) return cameras[0].GetComponent<Camera>();
        }

        Assert.Fail("Missing camera for graphic tests.");
        return null;
    }

    static HDRP_TestSettings FindHDRPTestSettings()
    {
        var settings = Object.FindObjectsByType<HDRP_TestSettings>();
        Assert.AreEqual(1, settings.Length, "Multiple HDRP_TestSettings found.");
        return settings[0];
    }

    static void VERYSLOW_SortByHierarchyPath<T>(T[] objects) where T : Component
    {
        // This weird little function is used to put the objects in the order they show up
        // in the scene hierarchy. Which is something that cannot be inferred from their IDs.
        // Unfortunately a lot of tests rely on this. This approach is really slow but the
        // test scenes are quite small.

        string GetHierarchySiblingPath(Transform t)
        {
            if (t.parent == null)
                return t.GetSiblingIndex().ToString("D8");
            return GetHierarchySiblingPath(t.parent) + "/" + t.GetSiblingIndex().ToString("D8");
        }

        var objectAndPath = new (T obj, string path)[objects.Length];
        for (int i = 0; i < objects.Length; ++i)
        {
            objectAndPath[i] = (objects[i], GetHierarchySiblingPath(objects[i].transform));
        }

        Array.Sort(objectAndPath, (a, b) => a.path.CompareTo(b.path));

        for (int i = 0; i < objects.Length; ++i)
        {
            objects[i] = objectAndPath[i].obj;
        }
    }

    public static IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        HDRP_TestSettings settings = null;
        Camera[] cameras = null;
        HDCamera[] hdCameras = null;
        Camera camera = null;

        Watermark.showDeveloperWatermark = false;

#if UNITY_EDITOR
        // Load the test settings
        var oldValueShaderUtil = UnityEditor.ShaderUtil.allowAsyncCompilation;
        var oldValueEditorSettings = UnityEditor.EditorSettings.asyncShaderCompilation;

        UnityEditor.ShaderUtil.allowAsyncCompilation = true;
        UnityEditor.EditorSettings.asyncShaderCompilation = true;

        GraphicsTestLogger.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImage.LoadMessage}.");
        SceneManager.LoadScene(testCase.ScenePath);

        // Wait for scene loading to retrieve settings/camera.
        for (int i = 0; i < 5; ++i)
            yield return new WaitForEndOfFrame();

        settings = FindHDRPTestSettings();
        camera = FindMainCamera();

        if (!settings.gpuDrivenCompatible && GPUResidentDrawerRequested())
            Assert.Ignore("Test scene is not compatible with GPU Driven and and will be skipped.");

        // Purpose is to setup proper test aspect ratio for the camera to "see" all objects and trigger related shader compilation tasks.
        int warmupTime = 1;
        Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(warmupTime, settings.ImageComparisonSettings);

        if (!XRGraphicsAutomatedTests.enabled)
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
        settings = FindHDRPTestSettings();
        camera = FindMainCamera();

        // Grab the HDCamera
        HDCamera hdCamera = HDCamera.GetOrCreate(camera);

        //We need to get all the cameras to set accumulation in all of them for tests that uses multiple cameras
        cameras = Object.FindObjectsByType<Camera>();
        VERYSLOW_SortByHierarchyPath(cameras);

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
            GraphicsTestLogger.Log(LogType.Log, "XR Automated Tests enabled.");

            Unity.Testing.XR.Runtime.ConfigureMockHMD.ConfigureXRDisplay(settings.ImageComparisonSettings);

            #if !UNITY_EDITOR
            // Some tests need time after eye resolution change before setting player resolution
            yield return Unity.Testing.XR.Runtime.ConfigureMockHMD.WaitForXRFrames(1);
            #endif

            waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetPlayerResolutionForXR(settings.ImageComparisonSettings, waitFrames);

            // Increase tolerance to account for slight changes due to float precision
            settings.ImageComparisonSettings.AverageCorrectnessThreshold *= settings.xrThresholdMultiplier;
            settings.ImageComparisonSettings.PerPixelCorrectnessThreshold *= settings.xrThresholdMultiplier;

            // Increase number of volumetric slices to compensate for initial half-resolution due to XR single-pass optimization
            foreach (var volume in Object.FindObjectsByType<Volume>())
            {
                if (volume.profile.TryGet(out Fog fog))
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

        var settingsSG = settings as HDRP_ShaderGraph_TestSettings;
        if (settingsSG == null || !settingsSG.compareSGtoBI)
        {
            // Standard Test
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, settings?.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);

            // For some reason, tests on mac os have started failing with render graph enabled by default.
            // Some tests have 400+ gcalloc in them. Unfortunately it's not reproductible outside of command line so it's impossible to debug.
            // That's why we don't test on macos anymore.
            if (settings.checkMemoryAllocation)
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
                ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, (settings != null) ? settings.ImageComparisonSettings : null, testCase.ReferenceImage.LoadMessage);
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
                ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, (settings != null) ? settings.ImageComparisonSettings : null, testCase.ReferenceImage.LoadMessage);
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

    public static void SetRayTracingAccumulationOnCameras(HDCamera[] hdCameras, bool b)
    {
        foreach(HDCamera hdCamera in hdCameras)
        {
            hdCamera.SetRayTracingAccumulation(b);
        }
    }
}
