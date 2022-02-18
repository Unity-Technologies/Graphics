using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;

public class RenderPipelineManagerCallbackTests
{
    const int k_RenderCount = 10;

    int begin = 0;
    int end = 0;
    Camera currentCamera;

    public IEnumerator SetupTest()
    {
        begin = 0;
        end = 0;

        GameObject go = new GameObject();
        currentCamera = go.AddComponent<Camera>();

        // Skip a few frame for the rp to stability
        for (int i = 0; i < 5; i++)
            yield return new WaitForEndOfFrame();

        RenderPipelineManager.beginCameraRendering += CountBeginCameraRender;
        RenderPipelineManager.endCameraRendering += CountEndCameraRender;
    }

    void CountBeginCameraRender(ScriptableRenderContext context, Camera camera) => begin++;

    void CountEndCameraRender(ScriptableRenderContext context, Camera camera) => end++;

    public void CheckResult(int expectedValue)
    {
        RenderPipelineManager.beginCameraRendering -= CountBeginCameraRender;
        RenderPipelineManager.endCameraRendering -= CountEndCameraRender;

        Assert.AreEqual(expectedValue, begin);
        Assert.AreEqual(begin, end);
    }

    [UnityTest]
    public IEnumerator BeginAndEndCameraRenderingCallbackMatch_Camera()
    {
        if (XRGraphicsAutomatedTests.enabled)
            yield break;
        yield return SetupTest();
        for (int i = 0; i < k_RenderCount; i++)
            currentCamera.Render();
        CheckResult(k_RenderCount);
    }

    [UnityTest]
    public IEnumerator BeginAndEndCameraRenderingCallbackMatch_RenderToTexture()
    {
        yield return SetupTest();
        currentCamera.targetTexture = new RenderTexture(1, 1, 32, RenderTextureFormat.ARGB32);
        currentCamera.targetTexture.Create();
        for (int i = 0; i < k_RenderCount; i++)
            currentCamera.Render();
        CheckResult(k_RenderCount);
    }

    [UnityTest]
    public IEnumerator BeginAndEndCameraRenderingCallbackMatch_CustomRender()
    {
        if (XRGraphicsAutomatedTests.enabled)
            yield break;
        yield return SetupTest();
        var additionalData = currentCamera.gameObject.AddComponent<HDAdditionalCameraData>();
        additionalData.customRender += (_, _) => { };
        for (int i = 0; i < k_RenderCount; i++)
            currentCamera.Render();
        CheckResult(k_RenderCount);
    }

    [UnityTest]
    public IEnumerator BeginAndEndCameraRenderingCallbackMatch_FullscreenPassthrough()
    {
        yield return SetupTest();
        var additionalData = currentCamera.gameObject.AddComponent<HDAdditionalCameraData>();
        additionalData.fullscreenPassthrough = true;
        additionalData.customRender += (_, _) => { };
        for (int i = 0; i < k_RenderCount; i++)
            currentCamera.Render();
        // Fullscreen passthrough don't trigger begin/end camera rendering
        CheckResult(0);
    }
}