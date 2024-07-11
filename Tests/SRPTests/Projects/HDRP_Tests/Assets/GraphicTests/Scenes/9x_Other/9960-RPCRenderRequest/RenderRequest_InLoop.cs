using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class RenderRequest_InLoop : MonoBehaviour
{
    public Camera renderRequestCamera;
    private RenderPipeline.StandardRequest m_StandardRequest = new();

    public RenderTexture onBeginCameraRendering;
    public RenderTexture onBeginContextRendering;
    public RenderTexture onEndCameraRendering;
    public RenderTexture onEndContextRendering;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRender;
        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRender;
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }

    public void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRender;
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRender;
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }

    void SubmitStandardRenderRequest(RenderTexture rt, Camera cam)
    {
        if (RenderPipeline.SupportsRenderRequest(cam, m_StandardRequest))
        {
            m_StandardRequest.destination = rt;
            RenderPipeline.SubmitRenderRequest(cam, m_StandardRequest);
        }
    }

    private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        // Properly nest the context render of this camera with the submit of the other camera
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitStandardRenderRequest(onBeginContextRendering, renderRequestCamera);
        }
    }

    private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        // Properly nest the context render of this camera with the submit of the other camera
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitStandardRenderRequest(onEndContextRendering, renderRequestCamera);
        }
    }

    private void OnBeginCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        // Only render if the camera that was begin render called is the main camera.
        if (cam == GetComponent<Camera>())
        {
            SubmitStandardRenderRequest(onBeginCameraRendering, renderRequestCamera);
        }
    }

    private void OnEndCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        // Only render if the camera that was begin render called is the main camera.
        if (cam == GetComponent<Camera>())
        {
            SubmitStandardRenderRequest(onEndCameraRendering, renderRequestCamera);
        }
    }
}
