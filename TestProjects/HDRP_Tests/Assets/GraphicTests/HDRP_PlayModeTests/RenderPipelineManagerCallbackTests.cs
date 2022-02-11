using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;

public class RenderPipelineManagerCallbackTests
{
    const int k_RenderCount = 10;

    int begin = 0;
    int end = 0;

    public Camera SetupTest()
    {
        begin = 0;
        end = 0;

        GameObject go = new GameObject();
        var camera = go.AddComponent<Camera>();

        RenderPipelineManager.beginCameraRendering += CountBeginCameraRender;
        RenderPipelineManager.endCameraRendering += CountEndCameraRender;

        return camera;
    }

    void CountBeginCameraRender(ScriptableRenderContext context, Camera camera) => begin++;

    void CountEndCameraRender(ScriptableRenderContext context, Camera camera) => end++;

    public void CheckResult(int expectedValue)
    {
        RenderPipelineManager.beginCameraRendering -= CountBeginCameraRender;
        RenderPipelineManager.endCameraRendering -= CountEndCameraRender;

        Assert.IsTrue(begin == end && begin == expectedValue);
    }

    [Test]
    public void BeginAndEndCameraRenderingCallbackMatch_Camera()
    {
        var camera = SetupTest();
        for (int i = 0; i < k_RenderCount; i++)
            camera.Render();
        CheckResult(k_RenderCount);
    }

    [Test]
    public void BeginAndEndCameraRenderingCallbackMatch_RenderToTexture()
    {
        var camera = SetupTest();
        camera.targetTexture = new RenderTexture(1, 1, 32, RenderTextureFormat.ARGB32);
        camera.targetTexture.Create();
        for (int i = 0; i < k_RenderCount; i++)
            camera.Render();
        CheckResult(k_RenderCount);
    }

    [Test]
    public void BeginAndEndCameraRenderingCallbackMatch_CustomRender()
    {
        var camera = SetupTest();
        var additionalData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
        additionalData.customRender += (_, _) => { };
        for (int i = 0; i < k_RenderCount; i++)
            camera.Render();
        CheckResult(k_RenderCount);
    }

    [Test]
    public void BeginAndEndCameraRenderingCallbackMatch_FullscreenPassthrough()
    {
        var camera = SetupTest();
        var additionalData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
        additionalData.fullscreenPassthrough = true;
        additionalData.customRender += (_, _) => {};
        for (int i = 0; i < k_RenderCount; i++)
            camera.Render();
        // Fullscreen passthrough don't trigger begin/end camera rendering
        CheckResult(0);
    }
}
