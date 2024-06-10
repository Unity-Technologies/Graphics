using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

class SingleCameraRequestCallbacksTests
{
    private Camera m_Camera;
    private Camera m_SingleRenderRequestCamera;

    private RenderTexture m_RT;

    [SetUp]
    public void Setup()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            Assert.Ignore("URP Only test");
        
        var go = new GameObject($"{nameof(SingleCameraRequestCallbacksTests)}_Main");
        m_Camera = go.AddComponent<Camera>();

        go = new GameObject($"{nameof(SingleCameraRequestCallbacksTests)}_SingleRenderRequest");
        m_SingleRenderRequestCamera = go.AddComponent<Camera>();

        // Avoid that the camera renders outside the submit render request
        m_Camera.enabled = false; 
        m_SingleRenderRequestCamera.enabled = false; 

        m_RT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        m_RT.Create();
    }

    [TearDown]
    public void TearDown()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
        {
            Assert.IsTrue(m_CallbackTriggered);

            Object.DestroyImmediate(m_Camera.gameObject);
            Object.DestroyImmediate(m_SingleRenderRequestCamera.gameObject);

            m_RT.Release();

            m_CallbackTriggered = false;
        }
    }

    void SendRequest()
    {
        UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();

        if (RenderPipeline.SupportsRenderRequest(m_SingleRenderRequestCamera, request))
        {
            request.destination = m_RT;
            RenderPipeline.SubmitRenderRequest(m_SingleRenderRequestCamera, request);
        }
    }

    private bool m_CallbackTriggered = false;

    private void ActionRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == m_SingleRenderRequestCamera)
            m_CallbackTriggered = true;
    }

    private void ActionContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (cameras.Contains(m_SingleRenderRequestCamera))
            m_CallbackTriggered = true;
    }

    [UnityTest]
    public IEnumerator BeginCameraRenderingIsTriggered()
    {
        RenderPipelineManager.beginCameraRendering += ActionRendering;

        SendRequest();

        yield return new WaitForEndOfFrame();

        RenderPipelineManager.beginCameraRendering -= ActionRendering;
    }

    [UnityTest]
    public IEnumerator EndCameraRenderingIsTriggered()
    {
        RenderPipelineManager.endCameraRendering += ActionRendering;

        SendRequest();

        yield return new WaitForEndOfFrame();

        RenderPipelineManager.endCameraRendering -= ActionRendering;
    }

    [UnityTest]
    public IEnumerator BeginContextRenderingIsTriggered()
    {
        RenderPipelineManager.beginContextRendering += ActionContext;
        
        SendRequest();

        yield return new WaitForEndOfFrame();

        RenderPipelineManager.beginContextRendering -= ActionContext;
    }

    [UnityTest]
    public IEnumerator EndContextRenderingIsTriggered()
    {
        RenderPipelineManager.endContextRendering += ActionContext;

        SendRequest();

        yield return new WaitForEndOfFrame();

        RenderPipelineManager.endContextRendering -= ActionContext;
    }
}
