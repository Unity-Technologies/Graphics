using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[ExecuteInEditMode]
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
