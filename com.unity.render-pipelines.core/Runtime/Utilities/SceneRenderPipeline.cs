using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SceneRenderPipeline : MonoBehaviour
{
    public RenderPipelineAsset renderPipelineAsset;

    void OnEnable()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }

    void OnValidate()
    {
        GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }
}
