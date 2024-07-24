using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class SetPathTracingSeed : MonoBehaviour
{
    PathTracing pt;
    public Camera pathTracingCamera;

    void OnEnable()
    {
        GetComponent<Volume>().profile.TryGet(out pt);
        pt.seedMode.value = SeedMode.Custom;
        pt.seedMode.overrideState = true;
        pt.customSeed.overrideState = true;
        pt.customSeed.value = 0;
        RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnBeginCameraRendering;
    }

    void RenderPipelineManagerOnBeginCameraRendering(ScriptableRenderContext _, Camera camera)
    {
        if (pathTracingCamera == camera)
            pt.customSeed.value++;
    }
}
