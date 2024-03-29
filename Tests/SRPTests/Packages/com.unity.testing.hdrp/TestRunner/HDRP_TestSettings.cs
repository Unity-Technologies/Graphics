using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class HDRP_TestSettings : GraphicsTestSettings
{
    public UnityEngine.Events.UnityEvent doBeforeTest;

    [Tooltip("Sets the framerate to use when executing the tests. Setting 0 does not set any framerate.")]
    public int captureFramerate = 0;

    [Tooltip("Sets the number of frames the framework needs to wait before executing the test.")]
    public int waitFrames = 0;

    [Tooltip("When enabled, the framework waits for a specific frame multiple count before executing the test.")]
    public bool waitForFrameCountMultiple = false;
    [Tooltip("Sets the multiple frame count.")]
    public int frameCountMultiple = 8;

    [Tooltip("When enabled, the tests handles XR compatibility.")]
    public bool xrCompatible = true;

    [Tooltip("When enabled, the tests handles GPU Driven compatibility.")]
    public bool gpuDrivenCompatible = true;

    [UnityEngine.Range(1.0f, 10.0f)]
    [Tooltip("Set the multiplier to increase the tolerance in AverageCorrectnessThreshold and PerPixelCorrectnessThreshold to account for slight changes due to float precision.")]
    public float xrThresholdMultiplier = 1.0f;

    [Tooltip("When enabled, the tests fails if GC.Alloc are executed after a few frames during the tests.")]
    public bool checkMemoryAllocation = true;

    [Tooltip("Specifies the render pipeline asset used when executing the test.")]
    public RenderPipelineAsset renderPipelineAsset;

    [Tooltip("RP Asset change is only effective after a frame is rendered.")]
    public bool forceCameraRenderDuringSetup = false;

    [Tooltip("When enabled, the tests handle frame consistency for VFXs.")]
    public bool containsVFX = false;

    void Awake()
    {
        if (renderPipelineAsset == null)
        {
            Debug.LogWarning("No RenderPipelineAsset has been assigned in the test settings. This may result in a wrong test.");
            return;
        }

        var currentRP = GraphicsSettings.defaultRenderPipeline;

        if (currentRP != renderPipelineAsset)
        {
            quitDebug.AppendLine($"{SceneManager.GetActiveScene().name} RP asset change: {((currentRP == null) ? "null" : currentRP.name)} => {renderPipelineAsset.name}");

            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;

            // Render pipeline is only reconstructed when a frame is renderer
            // If scene requires lightmap baking, we have to force it
            // Currently Camera.Render() fails on mac so we have to filter out the tests that rely on forceCameraRenderDuringSetup (like 2120 for APV).
            // But since setup is run regardless of the filter we add this explicit check on platform
            if (forceCameraRenderDuringSetup && !Application.isPlaying && Application.platform != RuntimePlatform.OSXEditor)
                Camera.main.Render();
        }
    }

    static StringBuilder quitDebug = new StringBuilder();

    void OnApplicationQuit()
    {
        if (quitDebug.Length == 0) return;

        Debug.Log($"Scenes that needed to change the RP asset:{Environment.NewLine}{quitDebug.ToString()}");

        quitDebug.Clear();
    }
}
