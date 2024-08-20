using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class RenderRequest_InLoop : MonoBehaviour
{
    public Camera renderRequestCamera;
    private UniversalRenderPipeline.SingleCameraRequest m_SingleCameraRequest = new();

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

    void SubmitSingleRenderRequest(RenderTexture rt, Camera cam)
    {
        if (RenderPipeline.SupportsRenderRequest(cam, m_SingleCameraRequest))
        {
            m_SingleCameraRequest.destination = rt;
            RenderPipeline.SubmitRenderRequest(cam, m_SingleCameraRequest);
        }
    }

    private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        // Properly nest the context render of this camera with the submit of the other camera
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitSingleRenderRequest(onBeginContextRendering, renderRequestCamera);
        }
    }

    private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        // Properly nest the context render of this camera with the submit of the other camera
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitSingleRenderRequest(onEndContextRendering, renderRequestCamera);
        }
    }

    private void OnBeginCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        // Only render if the camera that was begin render called is the main camera.
        if (cam == GetComponent<Camera>())
        {
            SubmitSingleRenderRequest(onBeginCameraRendering, renderRequestCamera);
        }
    }

    private void OnEndCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        // Only render if the camera that was begin render called is the main camera.
        if (cam == GetComponent<Camera>())
        {
            SubmitSingleRenderRequest(onEndCameraRendering, renderRequestCamera);
        }
    }
}
